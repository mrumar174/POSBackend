using Microsoft.EntityFrameworkCore;
using POS.DTOs.Tenancy;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Entities.Tenancy;
using POS.Entities.Identity;
using POS.Entities.IServices.Tenancy;
using POS.Entities.Tenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.Services.Temancy
{
    public class TenantService : ITenantService
    {
        private const int TrialDays = 3;
        private const string SuperAdminRoleName = "Admin";
        private static readonly string[] PlatformOnlyPermissionPrefixes = { "Tenants.", "Permissions." };
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public TenantService(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // Manual projection (not AutoMapper) so the two duplicate enums
        // (POS.Entities.Common vs POS.DTOs.Common) get cast explicitly.
        // Excludes soft-deleted tenants.
        private IQueryable<TenantDto> TenantsWithCounts() =>
            _db.Tenants.IgnoreQueryFilters().Where(t => t.IsActive).Select(t => new TenantDto
            {
                Id = t.Id,
                Code = t.Code,
                BusinessName = t.BusinessName,
                OwnerName = t.OwnerName,
                ContactNo = t.ContactNo,
                Email = t.Email,
                Address = t.Address,
                City = t.City,
                Slug = t.Slug,
                SubscriptionStatus = (DTOs.Common.SubscriptionStatus)(int)t.SubscriptionStatus,
                SubscriptionPlanCode = t.SubscriptionPlanCode,
                SubscriptionStartDate = t.SubscriptionStartDate,
                SubscriptionEndDate = t.SubscriptionEndDate,
                MaxShops = t.MaxShops,
                MaxUsers = t.MaxUsers,
                DataIsolationMode = (DTOs.Common.DataIsolationMode)(int)t.DataIsolationMode,
                ShopCount = t.Shops.Count(s => s.IsActive),
                UserCount = t.Users.Count(u => u.IsActive)
            });

        public async Task<List<TenantDto>> GetAllAsync()
            => await TenantsWithCounts().ToListAsync();

        public async Task<TenantDto?> GetByIdAsync(int id)
            => await TenantsWithCounts().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
        {
            // Subscription: a trial is fixed at TrialDays; a paid tenant needs a future end date
            SubscriptionStatus status;
            string planCode;
            DateTime subscriptionEnd;
            if (dto.IsTrial)
            {
                status = SubscriptionStatus.Trial;
                planCode = "TRIAL";
                subscriptionEnd = DateTime.Today.AddDays(TrialDays - 1);
            }
            else
            {
                if (dto.SubscriptionEndDate is null || dto.SubscriptionEndDate.Value.Date < DateTime.Today)
                    throw new InvalidOperationException("Subscription end date is required and cannot be in the past.");
                status = SubscriptionStatus.Active;
                planCode = string.IsNullOrWhiteSpace(dto.SubscriptionPlanCode) ? "CUSTOM" : dto.SubscriptionPlanCode;
                subscriptionEnd = dto.SubscriptionEndDate.Value.Date;
            }

            var slug = await GenerateUniqueSlugAsync(dto.BusinessName);
            var code = await GenerateNextCodeAsync();

            // Everything below succeeds or fails together
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var tenant = new Tenant
            {
                Code = code,
                BusinessName = dto.BusinessName,
                OwnerName = dto.OwnerName,
                ContactNo = dto.ContactNo,
                Email = dto.Email,
                Address = dto.Address,
                City = dto.City,
                Slug = slug,
                SubscriptionStatus = status,
                SubscriptionPlanCode = planCode,
                SubscriptionStartDate = DateTime.Now,
                SubscriptionEndDate = subscriptionEnd,
                MaxShops = dto.MaxShops,
                MaxUsers = dto.MaxUsers
            };

            var shop = new Shop
            {
                Code = "SHOP-00001",
                Name = string.IsNullOrWhiteSpace(dto.MainShopName) ? "Main Branch" : dto.MainShopName,
                Address = dto.Address,
                City = dto.City,
                ContactNo = dto.ContactNo,
                IsMainBranch = true,
                InvoicePrefix = code
            };
            tenant.Shops.Add(shop);

            User? adminUser = null;
            if (!string.IsNullOrWhiteSpace(dto.AdminUserName) && !string.IsNullOrWhiteSpace(dto.AdminPassword))
            {
                adminUser = new User
                {
                    UserName = dto.AdminUserName,
                    FullName = string.IsNullOrWhiteSpace(dto.AdminFullName) ? dto.AdminUserName : dto.AdminFullName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.AdminPassword)
                };
                adminUser.UserShops.Add(new UserShop { Shop = shop, IsDefault = true });
                tenant.Users.Add(adminUser);
            }

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            var role = new Role { TenantId = tenant.Id, Name = SuperAdminRoleName };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            var permissions = await _db.Permissions
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            foreach (var p in permissions.Where(p =>
                         !PlatformOnlyPermissionPrefixes.Any(prefix => (p.Name ?? "").StartsWith(prefix))))
            {
                _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
            }

            if (adminUser != null)
                _db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = role.Id });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdAsync(tenant.Id)
                ?? throw new InvalidOperationException("Tenant was created but could not be reloaded.");
        }

        public async Task<TenantDto> UpdateAsync(UpdateTenantDto dto)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == dto.Id && t.IsActive)
                ?? throw new KeyNotFoundException($"Tenant {dto.Id} not found.");

            var activeShopCount = await _db.Set<Shop>()
                .CountAsync(s => s.TenantId == dto.Id && s.IsActive);
            if (dto.MaxShops < activeShopCount)
                throw new InvalidOperationException(
                    $"Cannot set MaxShops to {dto.MaxShops}; tenant already has {activeShopCount} active shop(s).");

            tenant.BusinessName = dto.BusinessName;
            tenant.OwnerName = dto.OwnerName;
            tenant.ContactNo = dto.ContactNo;
            tenant.Email = dto.Email;
            tenant.Address = dto.Address;
            tenant.City = dto.City;
            tenant.MaxShops = dto.MaxShops;
            tenant.MaxUsers = dto.MaxUsers;

            await _db.SaveChangesAsync();

            return await GetByIdAsync(tenant.Id)
                ?? throw new InvalidOperationException("Tenant was updated but could not be reloaded.");
        }

        public async Task<TenantDto> UpdateSubscriptionAsync(UpdateSubscriptionDto dto)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == dto.TenantId && t.IsActive)
                ?? throw new KeyNotFoundException($"Tenant {dto.TenantId} not found.");

            var status = (SubscriptionStatus)(int)dto.SubscriptionStatus;

            if (status == SubscriptionStatus.Trial)
            {
                // A trial can never run longer than TrialDays from the tenant's start date
                var lastTrialDay = tenant.SubscriptionStartDate.Date.AddDays(TrialDays - 1);
                if (dto.SubscriptionEndDate is null || dto.SubscriptionEndDate.Value.Date > lastTrialDay)
                    throw new InvalidOperationException(
                        $"A trial is limited to {TrialDays} days from the start date (until {lastTrialDay:dd MMM yyyy}).");
            }
            else if (status == SubscriptionStatus.Active
                     && dto.SubscriptionEndDate.HasValue
                     && dto.SubscriptionEndDate.Value.Date < DateTime.Today)
            {
                throw new InvalidOperationException("For an Active subscription the end date cannot be in the past.");
            }

            tenant.SubscriptionStatus = status;
            tenant.SubscriptionPlanCode = dto.SubscriptionPlanCode ?? tenant.SubscriptionPlanCode;
            tenant.SubscriptionEndDate = dto.SubscriptionEndDate?.Date;

            await _db.SaveChangesAsync();

            return await GetByIdAsync(tenant.Id)
                ?? throw new InvalidOperationException("Subscription was updated but tenant could not be reloaded.");
        }

        public async Task DeleteAsync(int id)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id && t.IsActive)
                ?? throw new KeyNotFoundException($"Tenant {id} not found.");

            // Soft delete only — AuditableEntity has no hard-delete concept,
            // and hard-deleting a tenant would cascade into every shop/user/sale.
            tenant.IsActive = false;
            tenant.DeletedBy = _currentUser.UserId;
            tenant.DeletedOn = DateTime.Now;

            await _db.SaveChangesAsync();
        }

        private async Task<string> GenerateUniqueSlugAsync(string businessName)
        {
            var baseSlug = new string(businessName.ToLower()
                    .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                    .ToArray())
                .Trim('-');
            while (baseSlug.Contains("--")) baseSlug = baseSlug.Replace("--", "-");

            var slug = baseSlug;
            var suffix = 1;
            // IgnoreQueryFilters: soft-deleted tenants still own their slug in the unique index
            while (await _db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == slug))
                slug = $"{baseSlug}-{suffix++}";

            return slug;
        }

        private async Task<string> GenerateNextCodeAsync()
        {
            // Highest existing code including soft-deleted tenants (codes are fixed-width, so sorting as text works)
            var lastCode = await _db.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.Code.StartsWith("TEN-"))
                .OrderByDescending(t => t.Code)
                .Select(t => t.Code)
                .FirstOrDefaultAsync();

            var lastNumber = 0;
            if (lastCode != null)
                int.TryParse(lastCode.Substring(4), out lastNumber);

            return $"TEN-{(lastNumber + 1):D5}";
        }
    }
}

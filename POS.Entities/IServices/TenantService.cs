using Microsoft.EntityFrameworkCore;
using POS.DTOs.Tenancy;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Tenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices
{
    public class TenantService : ITenantService
    {
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
            _db.Tenants.Where(t => t.IsActive).Select(t => new TenantDto
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
                SubscriptionStatus = (POS.DTOs.Common.SubscriptionStatus)(int)t.SubscriptionStatus,
                SubscriptionPlanCode = t.SubscriptionPlanCode,
                SubscriptionStartDate = t.SubscriptionStartDate,
                SubscriptionEndDate = t.SubscriptionEndDate,
                MaxShops = t.MaxShops,
                MaxUsers = t.MaxUsers,
                DataIsolationMode = (POS.DTOs.Common.DataIsolationMode)(int)t.DataIsolationMode,
                ShopCount = t.Shops.Count(s => s.IsActive),
                UserCount = t.Users.Count(u => u.IsActive)
            });

        public async Task<List<TenantDto>> GetAllAsync()
            => await TenantsWithCounts().ToListAsync();

        public async Task<TenantDto?> GetByIdAsync(int id)
            => await TenantsWithCounts().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
        {
            var slug = await GenerateUniqueSlugAsync(dto.BusinessName);
            var code = await GenerateNextCodeAsync();

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
                SubscriptionPlanCode = dto.SubscriptionPlanCode,
                MaxShops = dto.MaxShops,
                MaxUsers = dto.MaxUsers,
                SubscriptionStartDate = DateTime.Now
            };

            // Every tenant gets exactly one shop row at signup — see Shop.cs comment.
            tenant.Shops.Add(new Shop
            {
                Code = "SHOP-00001",
                Name = string.IsNullOrWhiteSpace(dto.MainShopName) ? "Main Branch" : dto.MainShopName,
                Address = dto.Address,
                City = dto.City,
                ContactNo = dto.ContactNo,
                IsMainBranch = true,
                InvoicePrefix = code
            });

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

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

            tenant.SubscriptionStatus = (POS.Entities.Common.SubscriptionStatus)(int)dto.SubscriptionStatus;
            tenant.SubscriptionPlanCode = dto.SubscriptionPlanCode ?? tenant.SubscriptionPlanCode;
            tenant.SubscriptionEndDate = dto.SubscriptionEndDate;

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
            while (await _db.Tenants.AnyAsync(t => t.Slug == slug))
                slug = $"{baseSlug}-{suffix++}";

            return slug;
        }

        private async Task<string> GenerateNextCodeAsync()
        {
            var count = await _db.Tenants.CountAsync();
            return $"TEN-{(count + 1):D5}";
        }
    }
}

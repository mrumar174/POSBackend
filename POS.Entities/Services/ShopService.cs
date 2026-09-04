using AutoMapper;
using Microsoft.EntityFrameworkCore;
using POS.DTOs.Tenancy;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.Tenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.Services
{
    public class ShopService : IShopService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public ShopService(ApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        // Every query: scoped to this tenant AND excludes soft-deleted rows.
        private IQueryable<Shop> TenantShops() =>
            _db.Shops.Where(s => s.TenantId == _currentUser.TenantId && s.IsActive);

        public async Task<List<ShopDto>> GetAllAsync()
        {
            var shops = await TenantShops().ToListAsync();
            return _mapper.Map<List<ShopDto>>(shops);
        }

        public async Task<ShopDto?> GetByIdAsync(int id)
        {
            var shop = await TenantShops().FirstOrDefaultAsync(s => s.Id == id);
            return shop is null ? null : _mapper.Map<ShopDto>(shop);
        }

        public async Task<ShopDto> CreateAsync(CreateShopDto dto)
        {
            var tenant = await _db.Tenants.FindAsync(_currentUser.TenantId)
                ?? throw new InvalidOperationException("Tenant context is invalid.");

            var currentShopCount = await TenantShops().CountAsync();
            if (currentShopCount >= tenant.MaxShops)
                throw new InvalidOperationException(
                    $"Shop limit reached ({tenant.MaxShops}). Upgrade the subscription plan to add more shops.");

            var nameTaken = await TenantShops().AnyAsync(s => s.Name.ToLower() == dto.Name.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"Shop '{dto.Name}' already exists for this tenant.");

            var shop = new Shop
            {
                TenantId = _currentUser.TenantId,
                Code = $"SHOP-{(currentShopCount + 1):D5}",
                Name = dto.Name,
                Address = dto.Address,
                City = dto.City,
                ContactNo = dto.ContactNo,
                InvoicePrefix = dto.InvoicePrefix,
                IsMainBranch = currentShopCount == 0
            };

            _db.Shops.Add(shop);
            await _db.SaveChangesAsync();

            return _mapper.Map<ShopDto>(shop);
        }

        public async Task<ShopDto> UpdateAsync(UpdateShopDto dto)
        {
            var shop = await TenantShops().FirstOrDefaultAsync(s => s.Id == dto.Id)
                ?? throw new KeyNotFoundException($"Shop {dto.Id} not found.");

            var nameTaken = await TenantShops().AnyAsync(s =>
                s.Id != dto.Id && s.Name.ToLower() == dto.Name.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"Shop '{dto.Name}' already exists for this tenant.");

            shop.Name = dto.Name;
            shop.Address = dto.Address;
            shop.City = dto.City;
            shop.ContactNo = dto.ContactNo;
            shop.InvoicePrefix = dto.InvoicePrefix;

            await _db.SaveChangesAsync();
            return _mapper.Map<ShopDto>(shop);
        }

        public async Task DeleteAsync(int id)
        {
            var shop = await TenantShops().FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Shop {id} not found.");

            if (shop.IsMainBranch)
                throw new InvalidOperationException("The main branch shop cannot be deleted.");

            var hasActiveUsers = await _db.Set<UserShop>()
                .AnyAsync(us => us.ShopId == id);
            if (hasActiveUsers)
                throw new InvalidOperationException("Cannot delete a shop that still has users assigned to it.");

            // Soft delete — AuditableEntity has no hard-delete concept.
            shop.IsActive = false;
            shop.DeletedBy = _currentUser.UserId;
            shop.DeletedOn = DateTime.Now;

            await _db.SaveChangesAsync();
        }

        public async Task AssignUserAsync(AssignUserToShopDto dto)
        {
            var shopExists = await TenantShops().AnyAsync(s => s.Id == dto.ShopId);
            if (!shopExists)
                throw new KeyNotFoundException($"Shop {dto.ShopId} not found.");

            var alreadyAssigned = await _db.Set<UserShop>()
                .AnyAsync(us => us.UserId == dto.UserId && us.ShopId == dto.ShopId);
            if (alreadyAssigned)
                throw new InvalidOperationException("User is already assigned to this shop.");

            if (dto.IsDefault)
            {
                var existingDefaults = await _db.Set<UserShop>()
                    .Where(us => us.UserId == dto.UserId && us.IsDefault)
                    .ToListAsync();
                foreach (var d in existingDefaults) d.IsDefault = false;
            }

            _db.Set<UserShop>().Add(new UserShop
            {
                UserId = dto.UserId,
                ShopId = dto.ShopId,
                IsDefault = dto.IsDefault
            });

            await _db.SaveChangesAsync();
        }

        public async Task UnassignUserAsync(int userId, int shopId)
        {
            var link = await _db.Set<UserShop>()
                .FirstOrDefaultAsync(us => us.UserId == userId && us.ShopId == shopId)
                ?? throw new KeyNotFoundException("User is not assigned to this shop.");

            _db.Set<UserShop>().Remove(link); // UserShop has no soft-delete fields — hard delete is correct here
            await _db.SaveChangesAsync();
        }

        public async Task SetDefaultShopAsync(int userId, int shopId)
        {
            var links = await _db.Set<UserShop>().Where(us => us.UserId == userId).ToListAsync();
            var target = links.FirstOrDefault(us => us.ShopId == shopId)
                ?? throw new KeyNotFoundException("User is not assigned to this shop.");

            foreach (var l in links) l.IsDefault = false;
            target.IsDefault = true;

            await _db.SaveChangesAsync();
        }
    }
}

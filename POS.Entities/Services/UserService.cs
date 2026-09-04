using AutoMapper;
using Microsoft.EntityFrameworkCore;
using POS.DTOs.Identity;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Identity;
using POS.Entities.Tenancy;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POS.Entities.IServices;

namespace POS.Entities.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public UserService(ApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        private IQueryable<User> TenantUsers() =>
            _db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserShops)
                .Where(u => u.TenantId == _currentUser.TenantId);

        public async Task<List<UserDto>> GetAllAsync()
        {
            var users = await TenantUsers().ToListAsync();
            return _mapper.Map<List<UserDto>>(users);
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await TenantUsers().FirstOrDefaultAsync(u => u.Id == id);
            return user is null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var usernameTaken = await _db.Users.AnyAsync(u =>
                u.TenantId == _currentUser.TenantId &&
                u.UserName.ToLower() == dto.UserName.ToLower());

            if (usernameTaken)
                throw new InvalidOperationException($"Username '{dto.UserName}' is already taken.");

            var user = _mapper.Map<User>(dto);
            user.TenantId = _currentUser.TenantId;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            user.UserRoles = dto.RoleIds.Distinct()
                .Select(roleId => new UserRole { RoleId = roleId })
                .ToList();

            user.UserShops = dto.ShopIds.Distinct()
                .Select(shopId => new UserShop { ShopId = shopId })
                .ToList();

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(user.Id)
                ?? throw new InvalidOperationException("User was created but could not be reloaded.");
        }

        public async Task<UserDto> UpdateAsync(UpdateUserDto dto)
        {
            var user = await TenantUsers().FirstOrDefaultAsync(u => u.Id == dto.Id)
                ?? throw new KeyNotFoundException($"User {dto.Id} not found.");

            var usernameTaken = await _db.Users.AnyAsync(u =>
                u.TenantId == _currentUser.TenantId &&
                u.Id != dto.Id &&
                u.UserName.ToLower() == dto.UserName.ToLower());

            if (usernameTaken)
                throw new InvalidOperationException($"Username '{dto.UserName}' is already taken.");

            user.UserName = dto.UserName;
            user.FullName = dto.FullName;
            user.ContactNo = dto.ContactNo;
            user.Address = dto.Address;

            // Re-sync role assignments
            var newRoleIds = dto.RoleIds.Distinct().ToHashSet();
            //user.UserRoles.RemoveAll(ur => !newRoleIds.Contains(ur.RoleId));
            foreach (var role in user.UserRoles
                .Where(ur => !newRoleIds.Contains(ur.RoleId))
                .ToList())
            {
                user.UserRoles.Remove(role);
            }
            var existingRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
            foreach (var roleId in newRoleIds.Except(existingRoleIds))
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

            // Re-sync shop assignments
            var newShopIds = dto.ShopIds.Distinct().ToHashSet();
            //user.UserShops.RemoveAll(us => !newShopIds.Contains(us.ShopId));
            foreach (var shop in user.UserShops
                .Where(us => !newShopIds.Contains(us.ShopId))
                .ToList())
            {
                user.UserShops.Remove(shop);
            }
            var existingShopIds = user.UserShops.Select(us => us.ShopId).ToHashSet();
            foreach (var shopId in newShopIds.Except(existingShopIds))
                user.UserShops.Add(new UserShop { UserId = user.Id, ShopId = shopId });

            await _db.SaveChangesAsync();

            return await GetByIdAsync(user.Id)
                ?? throw new InvalidOperationException("User was updated but could not be reloaded.");
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == _currentUser.TenantId)
                ?? throw new KeyNotFoundException($"User {id} not found.");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.TenantId == _currentUser.TenantId)
                ?? throw new KeyNotFoundException($"User {dto.UserId} not found.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _db.SaveChangesAsync();
        }
    }
}

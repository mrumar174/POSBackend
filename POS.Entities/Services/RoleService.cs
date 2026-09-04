using AutoMapper;
using Microsoft.EntityFrameworkCore;
using POS.DTOs.Identity;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Identity;
using POS.Entities.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.Services
{
    public class RoleService : IRoleService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public RoleService(ApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        private IQueryable<Role> TenantRoles() =>
            _db.Roles
                .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .Where(r => r.TenantId == _currentUser.TenantId);

        public async Task<List<RoleDto>> GetAllAsync()
        {
            var roles = await TenantRoles().ToListAsync();
            return _mapper.Map<List<RoleDto>>(roles);
        }

        public async Task<RoleDto?> GetByIdAsync(int id)
        {
            var role = await TenantRoles().FirstOrDefaultAsync(r => r.Id == id);
            return role is null ? null : _mapper.Map<RoleDto>(role);
        }

        public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
        {
            var nameTaken = await _db.Roles.AnyAsync(r =>
                r.TenantId == _currentUser.TenantId &&
                r.Name.ToLower() == dto.Name.ToLower());

            if (nameTaken)
                throw new InvalidOperationException($"Role '{dto.Name}' already exists.");

            var role = _mapper.Map<Role>(dto);
            role.TenantId = _currentUser.TenantId;

            role.RolePermissions = dto.PermissionIds.Distinct()
                .Select(permissionId => new RolePermission { PermissionId = permissionId })
                .ToList();

            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(role.Id)
                ?? throw new InvalidOperationException("Role was created but could not be reloaded.");
        }

        public async Task<RoleDto> UpdateAsync(UpdateRoleDto dto)
        {
            var role = await TenantRoles().FirstOrDefaultAsync(r => r.Id == dto.Id)
                ?? throw new KeyNotFoundException($"Role {dto.Id} not found.");

            var nameTaken = await _db.Roles.AnyAsync(r =>
                r.TenantId == _currentUser.TenantId &&
                r.Id != dto.Id &&
                r.Name.ToLower() == dto.Name.ToLower());

            if (nameTaken)
                throw new InvalidOperationException($"Role '{dto.Name}' already exists.");

            role.Name = dto.Name;
            role.Description = dto.Description;

            var newPermissionIds = dto.PermissionIds.Distinct().ToHashSet();
            role.RolePermissions = role.RolePermissions
                .Where(rp => newPermissionIds.Contains(rp.PermissionId))
                .ToList();
            var existingPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
            foreach (var permissionId in newPermissionIds.Except(existingPermissionIds))
                role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });

            await _db.SaveChangesAsync();

            return await GetByIdAsync(role.Id)
                ?? throw new InvalidOperationException("Role was updated but could not be reloaded.");
        }

        public async Task DeleteAsync(int id)
        {
            var role = await _db.Roles
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _currentUser.TenantId)
                ?? throw new KeyNotFoundException($"Role {id} not found.");

            var inUse = await _db.Set<UserRole>().AnyAsync(ur => ur.RoleId == id);
            if (inUse)
                throw new InvalidOperationException("Cannot delete a role that is still assigned to users.");

            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
        }
    }
}

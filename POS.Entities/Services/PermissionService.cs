using AutoMapper;
using Microsoft.EntityFrameworkCore;
using POS.DTOs.Identity;
using POS.Entities.Data;
using POS.Entities.Identity;
using POS.Entities.IServices;

namespace POS.Entities.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public PermissionService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<PermissionDto>> GetAllAsync()
        {
            var permissions = await _db.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Module).ThenBy(p => p.Name)
                .ToListAsync();
            return _mapper.Map<List<PermissionDto>>(permissions);
        }

        public async Task<List<PermissionDto>> GetByModuleAsync(string module)
        {
            var permissions = await _db.Permissions
                .Where(p => p.IsActive && p.Module.ToLower() == module.ToLower())
                .OrderBy(p => p.Name)
                .ToListAsync();
            return _mapper.Map<List<PermissionDto>>(permissions);
        }

        public async Task<PermissionDto?> GetByIdAsync(int id)
        {
            var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            return permission is null ? null : _mapper.Map<PermissionDto>(permission);
        }

        public async Task<PermissionDto> CreateAsync(CreatePermissionDto dto)
        {
            var exists = await _db.Permissions.AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower());
            if (exists)
                throw new InvalidOperationException($"Permission '{dto.Name}' already exists.");

            var permission = new Permission
            {
                Name = dto.Name,
                Module = dto.Module,
                Description = dto.Description
            };

            _db.Permissions.Add(permission);
            await _db.SaveChangesAsync();

            return _mapper.Map<PermissionDto>(permission);
        }

        public async Task<List<PermissionDto>> CreateManyAsync(List<CreatePermissionDto> dtos)
        {
            var results = new List<PermissionDto>();
            foreach (var dto in dtos)
            {
                // Skip duplicates silently so re-running a seed list is safe.
                var exists = await _db.Permissions.AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower());
                if (exists) continue;

                var permission = new Permission
                {
                    Name = dto.Name,
                    Module = dto.Module,
                    Description = dto.Description
                };
                _db.Permissions.Add(permission);
                results.Add(_mapper.Map<PermissionDto>(permission));
            }

            await _db.SaveChangesAsync();
            return _mapper.Map<List<PermissionDto>>(results); // re-map to pick up generated Ids after save
        }
    }
}
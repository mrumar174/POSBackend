using AutoMapper;
using Microsoft.EntityFrameworkCore;
using POS.DTOs.Catalog;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.IServices.Catalog;
using UnitEntity = POS.Entities.Catalog.Unit;

namespace POS.Entities.Services
{
    public class UnitService : IUnitService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UnitService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<UnitDto>> GetAllAsync()
        {
            var units = await _db.Units.OrderBy(u => u.Name).ToListAsync();
            return _mapper.Map<List<UnitDto>>(units);
        }

        public async Task<UnitDto?> GetByIdAsync(int id)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id);
            return unit is null ? null : _mapper.Map<UnitDto>(unit);
        }

        public async Task<UnitDto> CreateAsync(CreateUnitDto dto)
        {
            var nameTaken = await _db.Units.AnyAsync(u =>
                u.Name.ToLower() == dto.Name.ToLower() || u.ShortName.ToLower() == dto.ShortName.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"Unit '{dto.Name}' or short name '{dto.ShortName}' already exists.");

            var unit = new UnitEntity { Name = dto.Name, ShortName = dto.ShortName };
            _db.Units.Add(unit);
            await _db.SaveChangesAsync();

            return _mapper.Map<UnitDto>(unit);
        }

        public async Task<UnitDto> UpdateAsync(UpdateUnitDto dto)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == dto.Id)
                ?? throw new KeyNotFoundException($"Unit {dto.Id} not found.");

            var nameTaken = await _db.Units.AnyAsync(u => u.Id != dto.Id &&
                (u.Name.ToLower() == dto.Name.ToLower() || u.ShortName.ToLower() == dto.ShortName.ToLower()));
            if (nameTaken)
                throw new InvalidOperationException($"Unit '{dto.Name}' or short name '{dto.ShortName}' already exists.");

            unit.Name = dto.Name;
            unit.ShortName = dto.ShortName;
            await _db.SaveChangesAsync();

            return _mapper.Map<UnitDto>(unit);
        }

        public async Task DeleteAsync(int id)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new KeyNotFoundException($"Unit {id} not found.");

            var inUse = await _db.Products.AnyAsync(p => p.UnitId == id);
            if (inUse)
                throw new InvalidOperationException("Cannot delete a unit that is still assigned to products.");

            _db.Units.Remove(unit);
            await _db.SaveChangesAsync();
        }
    }
}
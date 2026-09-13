using AutoMapper;
using Microsoft.EntityFrameworkCore;
using POS.DTOs.Catalog;
using POS.Entities.Catalog;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.IServices.Catalog;

namespace POS.Entities.Services
{
    public class BrandService : IBrandService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public BrandService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<BrandDto>> GetAllAsync()
        {
            var brands = await _db.Brands.OrderBy(b => b.Name).ToListAsync();
            return _mapper.Map<List<BrandDto>>(brands);
        }

        public async Task<BrandDto?> GetByIdAsync(int id)
        {
            var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id);
            return brand is null ? null : _mapper.Map<BrandDto>(brand);
        }

        public async Task<BrandDto> CreateAsync(CreateBrandDto dto)
        {
            var nameTaken = await _db.Brands.AnyAsync(b => b.Name.ToLower() == dto.Name.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"Brand '{dto.Name}' already exists.");

            var brand = new Brand { Name = dto.Name, Description = dto.Description };
            _db.Brands.Add(brand);
            await _db.SaveChangesAsync();

            return _mapper.Map<BrandDto>(brand);
        }

        public async Task<BrandDto> UpdateAsync(UpdateBrandDto dto)
        {
            var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Id == dto.Id)
                ?? throw new KeyNotFoundException($"Brand {dto.Id} not found.");

            var nameTaken = await _db.Brands.AnyAsync(b => b.Id != dto.Id && b.Name.ToLower() == dto.Name.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"Brand '{dto.Name}' already exists.");

            brand.Name = dto.Name;
            brand.Description = dto.Description;
            await _db.SaveChangesAsync();

            return _mapper.Map<BrandDto>(brand);
        }

        public async Task DeleteAsync(int id)
        {
            var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new KeyNotFoundException($"Brand {id} not found.");

            var inUse = await _db.Products.AnyAsync(p => p.BrandId == id);
            if (inUse)
                throw new InvalidOperationException("Cannot delete a brand that is still assigned to products.");

            _db.Brands.Remove(brand); // soft-delete via SaveChanges override
            await _db.SaveChangesAsync();
        }
    }
}
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using POS.DTOs.Catalog;
using POS.Entities.Catalog;
using POS.Entities.Data;
using POS.Entities.IServices;

namespace POS.Entities.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CategoryService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        // No .Where(TenantId == ...) anywhere in this class — the global query
        // filter on ApplicationDbContext already restricts every query to the
        // caller's tenant. That's the whole point of the multi-shop setup.
        public async Task<List<CategoryDto>> GetAllAsync()
        {
            var categories = await _db.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return _mapper.Map<List<CategoryDto>>(categories);
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            return category is null ? null : _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            var nameTaken = await _db.Categories.AnyAsync(c => c.Name == dto.Name);
            if (nameTaken)
                throw new InvalidOperationException($"A category named '{dto.Name}' already exists.");

            var category = _mapper.Map<Category>(dto);
            // category.TenantId is left at its default (0) here on purpose —
            // ApplicationDbContext.SaveChanges fills it in from ICurrentUserService.

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.Id)
                ?? throw new KeyNotFoundException("Category not found.");

            var nameTaken = await _db.Categories.AnyAsync(c => c.Name == dto.Name && c.Id != dto.Id);
            if (nameTaken)
                throw new InvalidOperationException($"A category named '{dto.Name}' already exists.");

            _mapper.Map(dto, category);
            await _db.SaveChangesAsync();

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException("Category not found.");

            var inUse = await _db.Products.AnyAsync(p => p.CategoryId == id);
            if (inUse)
                throw new InvalidOperationException("Cannot delete a category that still has products assigned to it.");

            _db.Categories.Remove(category); // intercepted into a soft delete (IsActive = false) by SaveChanges
            await _db.SaveChangesAsync();
        }
    }
}

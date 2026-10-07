using Microsoft.EntityFrameworkCore;
using POS.DTOs.Finance;
using POS.Entities.Data;
using POS.Entities.Finance;
using POS.Entities.IServices.Finance;

namespace POS.Entities.Services.Finance
{
    public class ExpenseCategoryService : IExpenseCategoryService
    {
        private readonly ApplicationDbContext _db;

        public ExpenseCategoryService(ApplicationDbContext db) => _db = db;

        private static ExpenseCategoryDto ToDto(ExpenseCategory c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description
        };

        public async Task<List<ExpenseCategoryDto>> GetAllAsync()
            => await _db.ExpenseCategories.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new ExpenseCategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
                .ToListAsync();

        public async Task<ExpenseCategoryDto?> GetByIdAsync(int id)
            => await _db.ExpenseCategories.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new ExpenseCategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
                .FirstOrDefaultAsync();

        public async Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryDto dto)
        {
            var name = NormalizeName(dto.Name);
            await EnsureUniqueNameAsync(name, excludeId: null);

            var entity = new ExpenseCategory
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
            };

            _db.ExpenseCategories.Add(entity);
            await _db.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ExpenseCategoryDto> UpdateAsync(int id, UpdateExpenseCategoryDto dto)
        {
            var entity = await _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Expense category {id} not found.");

            var name = NormalizeName(dto.Name);
            await EnsureUniqueNameAsync(name, excludeId: id);

            entity.Name = name;
            entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            await _db.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Expense category {id} not found.");

            if (await _db.Expenses.IgnoreQueryFilters().AnyAsync(e => e.ExpenseCategoryId == id))
                throw new InvalidOperationException(
                    $"Cannot delete '{entity.Name}' because expenses are recorded against it.");

            _db.ExpenseCategories.Remove(entity);
            await _db.SaveChangesAsync();
        }

        private static string NormalizeName(string? name)
        {
            var trimmed = name?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                throw new InvalidOperationException("Name is required.");
            if (trimmed.Length > 100)
                throw new InvalidOperationException("Name cannot exceed 100 characters.");
            return trimmed;
        }

        private async Task EnsureUniqueNameAsync(string name, int? excludeId)
        {
            var lower = name.ToLower();
            var exists = await _db.ExpenseCategories
                .AnyAsync(c => c.Name.ToLower() == lower && (excludeId == null || c.Id != excludeId));
            if (exists)
                throw new InvalidOperationException($"An expense category named '{name}' already exists.");
        }
    }
}
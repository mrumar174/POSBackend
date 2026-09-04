
using POS.DTOs.Catalog;

namespace POS.Entities.IServices
{
    /// <summary>
    /// Reference pattern for every other simple master-data service
    /// (Brand, Unit, PaymentMethod, ExpenseCategory, Supplier, ...) — same
    /// five methods, same shape. Copy this interface + its implementation
    /// and rename for each one.
    /// </summary>
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
        Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto);
        Task DeleteAsync(int id);
    }
}

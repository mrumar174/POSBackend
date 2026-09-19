using POS.DTOs.Purchasing;

namespace POS.Entities.IServices
{
    public interface IPurchaseService
    {
        Task<List<PurchaseDto>> GetAllAsync();
        Task<PurchaseDto?> GetByIdAsync(int id);
        Task<PurchaseDto> CreateAsync(CreatePurchaseDto dto);
        Task<PurchaseDto> UpdateAsync(int id, CreatePurchaseDto dto);
        Task DeleteAsync(int id);
    }
}
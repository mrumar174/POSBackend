using POS.DTOs.Common;
using POS.DTOs.Purchasing;

namespace POS.Entities.IServices
{
    public interface IPurchaseReturnService
    {
        Task<PagedResultDto<PurchaseReturnDto>> SearchAsync(PurchaseReturnQueryDto query);
        Task<PurchaseReturnDto?> GetByIdAsync(int id);
        Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnDto dto);
        Task<PurchaseReturnDto> UpdateAsync(int id, CreatePurchaseReturnDto dto);
        Task DeleteAsync(int id);
    }
}
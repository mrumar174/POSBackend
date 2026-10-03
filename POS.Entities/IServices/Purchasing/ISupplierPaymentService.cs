using POS.DTOs.Common;
using POS.DTOs.Purchasing;

namespace POS.Entities.IServices
{
    public interface ISupplierPaymentService
    {
        Task<PagedResultDto<SupplierPaymentDto>> SearchAsync(SupplierPaymentQueryDto query);
        Task<SupplierPaymentDto?> GetByIdAsync(int id);
        Task<SupplierPaymentDto> CreateAsync(CreateSupplierPaymentDto dto);
        Task<SupplierPaymentDto> UpdateAsync(int id, CreateSupplierPaymentDto dto);
        Task DeleteAsync(int id);

        // Purchases with DueAmount > 0 — feeds the payment form's dropdown.
        // Excludes the purchase currently being edited's own due (handled
        // by including its own row regardless of due, via excludePaymentId).
        Task<List<UnpaidPurchaseDto>> GetUnpaidPurchasesAsync(int? supplierId, int? excludePaymentId);
    }
}
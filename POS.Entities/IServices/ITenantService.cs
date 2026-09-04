using POS.DTOs.Tenancy;

namespace POS.Entities.IServices
{
    public interface ITenantService
    {
        Task<List<TenantDto>> GetAllAsync();
        Task<TenantDto?> GetByIdAsync(int id);
        Task<TenantDto> CreateAsync(CreateTenantDto dto);
        Task<TenantDto> UpdateAsync(UpdateTenantDto dto);
        Task<TenantDto> UpdateSubscriptionAsync(UpdateSubscriptionDto dto);
        Task DeleteAsync(int id);
    }
}
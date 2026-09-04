using POS.DTOs.Tenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.Entities.Tenancy
{
    public interface ITenantService
    {
        Task<List<TenantDto>> GetAllAsync();
        Task<TenantDto?> GetByIdAsync(int id);
        Task<TenantDto> CreateAsync(CreateTenantDto dto);
        Task<TenantDto> UpdateAsync(UpdateTenantDto dto);
        Task<TenantDto> UpdateSubscriptionAsync(UpdateSubscriptionDto dto);
        Task DeleteAsync(int id); // soft delete — sets IsActive = false
    }
}

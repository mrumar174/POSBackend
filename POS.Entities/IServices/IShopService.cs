using POS.DTOs.Tenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices
{
    public interface IShopService
    {
        Task<List<ShopDto>> GetAllAsync();
        Task<ShopDto?> GetByIdAsync(int id);
        Task<ShopDto> CreateAsync(CreateShopDto dto);
        Task<ShopDto> UpdateAsync(UpdateShopDto dto);
        Task DeleteAsync(int id); // soft delete — sets IsActive = false

        // User <-> Shop assignment
        Task AssignUserAsync(AssignUserToShopDto dto);
        Task UnassignUserAsync(int userId, int shopId);
        Task SetDefaultShopAsync(int userId, int shopId);
    }
}

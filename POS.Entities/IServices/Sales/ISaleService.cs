using POS.DTOs.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Sales
{
    public interface ISaleService
    {
        Task<List<SaleDto>> GetAllAsync();
        Task<SaleDto?> GetByIdAsync(int id);
        Task<SaleDto> CreateAsync(CreateSaleDto dto);
        Task<SaleDto> UpdateAsync(int id, CreateSaleDto dto);
        Task DeleteAsync(int id);
    }
}

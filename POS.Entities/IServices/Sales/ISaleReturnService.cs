using POS.DTOs.Common;
using POS.DTOs.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Sales
{
    public interface ISaleReturnService
    {
        Task<PagedResultDto<SaleReturnDto>> SearchAsync(SaleReturnQueryDto query);
        Task<SaleReturnDto?> GetByIdAsync(int id);
        Task<SaleReturnDto> CreateAsync(CreateSaleReturnDto dto);
        Task<SaleReturnDto> UpdateAsync(int id, CreateSaleReturnDto dto);
        Task DeleteAsync(int id);
    }
}

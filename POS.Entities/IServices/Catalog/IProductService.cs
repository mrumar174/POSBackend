using POS.DTOs.Catalog;
using POS.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Catalog
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllAsync();
        Task<ProductDto?> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(CreateProductDto dto);
        Task<ProductDto> UpdateAsync(UpdateProductDto dto);
        Task DeleteAsync(int id);
        Task<PagedResultDto<ProductDto>> SearchAsync(ProductQueryDto query);
    }
}

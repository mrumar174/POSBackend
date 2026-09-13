using POS.DTOs.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Catalog
{
    public interface IUnitService
    {
        Task<List<UnitDto>> GetAllAsync();
        Task<UnitDto?> GetByIdAsync(int id);
        Task<UnitDto> CreateAsync(CreateUnitDto dto);
        Task<UnitDto> UpdateAsync(UpdateUnitDto dto);
        Task DeleteAsync(int id);
    }
}

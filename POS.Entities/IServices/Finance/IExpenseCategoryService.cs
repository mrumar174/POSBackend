using POS.DTOs.Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Finance
{
    public interface IExpenseCategoryService
    {
        Task<List<ExpenseCategoryDto>> GetAllAsync();
        Task<ExpenseCategoryDto?> GetByIdAsync(int id);
        Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryDto dto);
        Task<ExpenseCategoryDto> UpdateAsync(int id, UpdateExpenseCategoryDto dto);
        Task DeleteAsync(int id);
    }
}

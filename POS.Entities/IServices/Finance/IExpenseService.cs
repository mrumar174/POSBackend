using POS.DTOs.Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Finance
{
    public interface IExpenseService
    {
        Task<ExpensePageDto> GetPagedAsync(ExpenseQueryDto query);
        Task<ExpenseDto?> GetByIdAsync(int id);
        Task<ExpenseDto> CreateAsync(CreateExpenseDto dto);
        Task<ExpenseDto> UpdateAsync(int id, CreateExpenseDto dto);
        Task DeleteAsync(int id);
    }
}

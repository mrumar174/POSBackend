using POS.DTOs.Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Finance
{
    public interface IDailyCashClosingService
    {
        Task<DailyCashClosingPreviewDto> CalculateAsync(DateTime date);
        Task<DailyCashClosingDto> CreateAsync(CreateDailyCashClosingDto dto);
        Task<List<DailyCashClosingDto>> GetAllAsync(DateTime? fromDate, DateTime? toDate);
        Task<DailyCashClosingDto?> GetByIdAsync(int id);
        Task DeleteAsync(int id); // "reopen": latest closing only
    }
}

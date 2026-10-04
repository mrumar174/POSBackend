using POS.DTOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Inventory
{
    public interface IStockService
    {
        // View / Summary
        Task<List<StockSummaryDto>> GetSummaryAsync(int? categoryId, int? brandId, string? search, bool lowStockOnly);
        Task<decimal> GetProductStockAsync(int productId);

        // Adjustments
        Task<List<StockAdjustmentDto>> GetAllAdjustmentsAsync();
        Task<StockAdjustmentDto?> GetAdjustmentByIdAsync(int id);
        Task<StockAdjustmentDto> CreateAdjustmentAsync(CreateStockAdjustmentDto dto);
        Task<StockAdjustmentDto> UpdateAdjustmentAsync(int id, CreateStockAdjustmentDto dto);
        Task DeleteAdjustmentAsync(int id);
    }
}

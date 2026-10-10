using POS.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Reports
{
    public interface IReportService
    {
        // Available now
        Task<PurchaseSummaryReportDto> GetPurchaseSummaryAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<SupplierPurchaseReportRowDto>> GetPurchasesBySupplierAsync(DateTime? fromDate, DateTime? toDate, string sortBy);
        Task<StockValuationSummaryDto> GetStockValuationAsync(int? categoryId);
        Task<List<StockMovementRowDto>> GetStockMovementAsync(int productId, DateTime? fromDate, DateTime? toDate);
        Task<List<LowStockReportRowDto>> GetLowStockAsync(int? categoryId);
        Task<List<SupplierDueReportRowDto>> GetSupplierDuesAsync(int? minDaysOverdue);
        Task<TaxReportDto> GetTaxReportAsync(DateTime? fromDate, DateTime? toDate);

        // Requires Sales module — remove if Sale/SaleDetail don't exist
        Task<SalesSummaryReportDto> GetSalesSummaryAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<ProductSalesReportRowDto>> GetSalesByProductAsync(DateTime? fromDate, DateTime? toDate, int? categoryId, string sortBy, int top);
        Task<List<CategorySalesReportRowDto>> GetSalesByCategoryAsync(DateTime? fromDate, DateTime? toDate);
        Task<ProfitSummaryReportDto> GetProfitReportAsync(DateTime? fromDate, DateTime? toDate, int? categoryId);
        Task<List<CashierPerformanceRowDto>> GetCashierPerformanceAsync(DateTime? fromDate, DateTime? toDate);

        // Requires Expenses module — remove if Expense/ExpenseCategory don't exist
        Task<ExpenseSummaryReportDto> GetExpenseReportAsync(DateTime? fromDate, DateTime? toDate);
    }
}

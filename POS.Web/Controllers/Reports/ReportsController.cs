using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Reports;
using POS.Entities.IServices;
using POS.Entities.IServices.Reports;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // reports are view-only; no RequirePermission beyond being logged in
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // ---- Available now ----

        [HttpGet("purchase-summary")]
        [ProducesResponseType(typeof(PurchaseSummaryReportDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<PurchaseSummaryReportDto>> GetPurchaseSummary(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => Ok(await _reportService.GetPurchaseSummaryAsync(fromDate, toDate));

        [HttpGet("purchases-by-supplier")]
        [ProducesResponseType(typeof(List<SupplierPurchaseReportRowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SupplierPurchaseReportRowDto>>> GetPurchasesBySupplier(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? sortBy)
            => Ok(await _reportService.GetPurchasesBySupplierAsync(fromDate, toDate, sortBy ?? "totalPurchased"));

        [HttpGet("stock-valuation")]
        [ProducesResponseType(typeof(StockValuationSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<StockValuationSummaryDto>> GetStockValuation([FromQuery] int? categoryId)
            => Ok(await _reportService.GetStockValuationAsync(categoryId));

        [HttpGet("stock-movement")]
        [ProducesResponseType(typeof(List<StockMovementRowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<StockMovementRowDto>>> GetStockMovement(
            [FromQuery] int productId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => Ok(await _reportService.GetStockMovementAsync(productId, fromDate, toDate));

        [HttpGet("low-stock")]
        [ProducesResponseType(typeof(List<LowStockReportRowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<LowStockReportRowDto>>> GetLowStock([FromQuery] int? categoryId)
            => Ok(await _reportService.GetLowStockAsync(categoryId));

        [HttpGet("supplier-dues")]
        [ProducesResponseType(typeof(List<SupplierDueReportRowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SupplierDueReportRowDto>>> GetSupplierDues([FromQuery] int? minDaysOverdue)
            => Ok(await _reportService.GetSupplierDuesAsync(minDaysOverdue));

        [HttpGet("tax")]
        [ProducesResponseType(typeof(TaxReportDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaxReportDto>> GetTaxReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => Ok(await _reportService.GetTaxReportAsync(fromDate, toDate));

        // ---- Requires Sales module — remove if Sale/SaleDetail don't exist ----

        [HttpGet("sales-summary")]
        [ProducesResponseType(typeof(SalesSummaryReportDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<SalesSummaryReportDto>> GetSalesSummary(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => Ok(await _reportService.GetSalesSummaryAsync(fromDate, toDate));

        [HttpGet("sales-by-product")]
        [ProducesResponseType(typeof(List<ProductSalesReportRowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProductSalesReportRowDto>>> GetSalesByProduct(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
            [FromQuery] int? categoryId, [FromQuery] string? sortBy, [FromQuery] int top = 20)
            => Ok(await _reportService.GetSalesByProductAsync(fromDate, toDate, categoryId, sortBy ?? "revenue", top));

        [HttpGet("sales-by-category")]
        [ProducesResponseType(typeof(List<CategorySalesReportRowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CategorySalesReportRowDto>>> GetSalesByCategory(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => Ok(await _reportService.GetSalesByCategoryAsync(fromDate, toDate));

        [HttpGet("profit")]
        [ProducesResponseType(typeof(ProfitSummaryReportDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProfitSummaryReportDto>> GetProfitReport(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int? categoryId)
            => Ok(await _reportService.GetProfitReportAsync(fromDate, toDate, categoryId));

        [HttpGet("cashier-performance")]
        [ProducesResponseType(typeof(List<CashierPerformanceRowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CashierPerformanceRowDto>>> GetCashierPerformance(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => Ok(await _reportService.GetCashierPerformanceAsync(fromDate, toDate));

        // ---- Requires Expenses module — remove if Expense/ExpenseCategory don't exist ----

        [HttpGet("expenses")]
        [ProducesResponseType(typeof(ExpenseSummaryReportDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ExpenseSummaryReportDto>> GetExpenseReport(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => Ok(await _reportService.GetExpenseReportAsync(fromDate, toDate));
    }
}
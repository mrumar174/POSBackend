using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.DTOs.Reports
{
    public class PurchaseSummaryReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalPurchaseInvoices { get; set; }
        public decimal TotalGrossPurchases { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalNetPurchases { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstandingDue { get; set; }
        public List<DailyPurchasePointDto> DailyBreakdown { get; set; } = new();
    }
    public class DailyPurchasePointDto
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public decimal NetPurchases { get; set; }
    }

    public class SupplierPurchaseReportRowDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = default!;
        public int InvoiceCount { get; set; }
        public decimal TotalPurchased { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
    }

    public class StockValuationReportRowDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public string ProductCode { get; set; } = default!;
        public string? CategoryName { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitSalePrice { get; set; }
        public decimal ValueAtCost { get; set; }
        public decimal ValueAtSalePrice { get; set; }
        public decimal PotentialProfit { get; set; }
    }

    public class StockValuationSummaryDto
    {
        public decimal TotalValueAtCost { get; set; }
        public decimal TotalValueAtSalePrice { get; set; }
        public decimal TotalPotentialProfit { get; set; }
        public List<StockValuationReportRowDto> Items { get; set; } = new();
    }

    public class StockMovementRowDto
    {
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = default!;
        public string? ReferenceNo { get; set; }
        public decimal QuantityIn { get; set; }
        public decimal QuantityOut { get; set; }
        public decimal RunningBalance { get; set; }
        public string? Remarks { get; set; }
    }

    public class LowStockReportRowDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public string ProductCode { get; set; } = default!;
        public string? CategoryName { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal SuggestedReorderQuantity { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public string? PreferredSupplierName { get; set; }
    }

    public class SupplierDueReportRowDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = default!;
        public string? ContactNo { get; set; }
        public int UnpaidInvoiceCount { get; set; }
        public decimal TotalDue { get; set; }
        public DateTime? OldestUnpaidInvoiceDate { get; set; }
        public int DaysOverdue { get; set; }
    }

    // Purchase side is real now; Sale side is 0 until Sales exists (see region below)
    public class TaxReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TaxCollectedOnSales { get; set; }
        public decimal TaxPaidOnPurchases { get; set; }
        public decimal NetTaxLiability { get; set; }
        public List<DailyTaxPointDto> DailyBreakdown { get; set; } = new();
    }

    public class DailyTaxPointDto
    {
        public DateTime Date { get; set; }
        public decimal TaxCollected { get; set; }
        public decimal TaxPaid { get; set; }
    }

    // ============================================================
    // REQUIRES SALES MODULE — delete these DTOs (and the matching
    // service/controller methods) if Sale/SaleDetail don't exist yet
    // ============================================================

    public class SalesSummaryReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalInvoices { get; set; }
        public decimal TotalGrossSales { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalNetSales { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal AverageInvoiceValue { get; set; }
        public List<DailySalesPointDto> DailyBreakdown { get; set; } = new();
    }

    public class DailySalesPointDto
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public decimal NetSales { get; set; }
    }

    public class ProductSalesReportRowDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public string ProductCode { get; set; } = default!;
        public string? CategoryName { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal AverageSellingPrice { get; set; }
    }

    public class CategorySalesReportRowDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = default!;
        public decimal QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PercentOfTotalSales { get; set; }
    }

    public class ProfitReportRowDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public decimal QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal MarginPercent { get; set; }
    }

    public class ProfitSummaryReportDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalGrossProfit { get; set; }
        public decimal OverallMarginPercent { get; set; }
        public List<ProfitReportRowDto> Items { get; set; } = new();
    }

    public class CashierPerformanceRowDto
    {
        public int UserId { get; set; }
        public string UserFullName { get; set; } = default!;
        public int InvoiceCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AverageInvoiceValue { get; set; }
    }

    // ============================================================
    // REQUIRES EXPENSES MODULE — delete if Expense/ExpenseCategory
    // don't exist yet
    // ============================================================

    public class ExpenseReportRowDto
    {
        public int ExpenseCategoryId { get; set; }
        public string ExpenseCategoryName { get; set; } = default!;
        public int ExpenseCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PercentOfTotal { get; set; }
    }

    public class ExpenseSummaryReportDto
    {
        public decimal TotalExpenses { get; set; }
        public List<ExpenseReportRowDto> ByCategory { get; set; } = new();
        public List<DailyExpensePointDto> DailyBreakdown { get; set; } = new();
    }

    public class DailyExpensePointDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}

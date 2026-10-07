using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.DTOs.Dashboard
{
    public class DashboardDto
    {
        public DashboardKpisDto Kpis { get; set; } = new();
        public List<SalesTrendPointDto> SalesTrend { get; set; } = new();
        public List<DashboardCategoryShareDto> SalesByCategory { get; set; } = new();
        public List<DashboardTopProductDto> TopProducts { get; set; } = new();
        public List<DashboardRecentSaleDto> RecentSales { get; set; } = new();
        public List<DashboardLowStockItemDto> LowStockAlerts { get; set; } = new();
        public List<DashboardSupplierDueDto> TopSupplierDues { get; set; } = new();
        public List<DashboardPaymentMethodShareDto> TodayPaymentMethodBreakdown { get; set; } = new();
    }
    public class DashboardKpisDto
    {
        public decimal TodaySales { get; set; }
        public decimal YesterdaySales { get; set; }
        public decimal TodaySalesChangePercent { get; set; }

        public int TodayInvoiceCount { get; set; }

        public decimal ThisMonthSales { get; set; }
        public decimal LastMonthSales { get; set; }
        public decimal MonthSalesChangePercent { get; set; }

        public decimal ThisMonthProfit { get; set; }

        public int TotalProductsCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }

        public decimal TotalSupplierDue { get; set; }
        public int SuppliersWithDueCount { get; set; }

        public decimal? TodayExpenses { get; set; }
    }

    public class SalesTrendPointDto
    {
        public DateTime Date { get; set; }
        public decimal Sales { get; set; }
    }

    public class DashboardCategoryShareDto
    {
        public string CategoryName { get; set; } = default!;
        public decimal Revenue { get; set; }
        public decimal PercentOfTotal { get; set; }
    }

    public class DashboardTopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardRecentSaleDto
    {
        public int SaleId { get; set; }
        public string InvoiceNo { get; set; } = default!;
        public DateTime SaleDate { get; set; }
        public string? CustomerName { get; set; }
        public decimal GrandTotal { get; set; }
        public string PaymentMethodName { get; set; } = default!;
    }

    public class DashboardLowStockItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public decimal QuantityOnHand { get; set; }
        public decimal MinimumStock { get; set; }
    }

    public class DashboardSupplierDueDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = default!;
        public decimal TotalDue { get; set; }
    }

    public class DashboardPaymentMethodShareDto
    {
        public string PaymentMethodName { get; set; } = default!;
        public decimal Amount { get; set; }
        public decimal PercentOfTotal { get; set; }
    }
}

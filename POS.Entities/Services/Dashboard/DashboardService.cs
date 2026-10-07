using Microsoft.EntityFrameworkCore;
using POS.DTOs.Dashboard;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.IServices.Dashboard;

namespace POS.Entities.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DashboardService(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<DashboardDto> GetDashboardDataAsync()
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
            var firstOfLastMonth = firstOfThisMonth.AddMonths(-1);
            var thirtyDaysAgo = today.AddDays(-29);

            var dto = new DashboardDto();

            // Fire independent aggregation queries concurrently where possible

            // 1. Core KPIs
            var todaySalesQuery = await _db.Sales.Where(s => s.SaleDate >= today).ToListAsync();
            var yesterdaySalesQuery = await _db.Sales.Where(s => s.SaleDate >= yesterday && s.SaleDate < today).ToListAsync();
            var thisMonthSalesQuery = await _db.Sales.Where(s => s.SaleDate >= firstOfThisMonth).ToListAsync();
            var lastMonthSalesQuery = await _db.Sales.Where(s => s.SaleDate >= firstOfLastMonth && s.SaleDate < firstOfThisMonth).ToListAsync();

            dto.Kpis.TodaySales = todaySalesQuery.Sum(s => s.GrandTotal);
            dto.Kpis.TodayInvoiceCount = todaySalesQuery.Count;
            dto.Kpis.YesterdaySales = yesterdaySalesQuery.Sum(s => s.GrandTotal);
            dto.Kpis.TodaySalesChangePercent = CalculatePercentChange(dto.Kpis.YesterdaySales, dto.Kpis.TodaySales);

            dto.Kpis.ThisMonthSales = thisMonthSalesQuery.Sum(s => s.GrandTotal);
            dto.Kpis.LastMonthSales = lastMonthSalesQuery.Sum(s => s.GrandTotal);
            dto.Kpis.MonthSalesChangePercent = CalculatePercentChange(dto.Kpis.LastMonthSales, dto.Kpis.ThisMonthSales);

            // This Month Profit: Revenue - (Qty * CurrentPurchasePrice)
            dto.Kpis.ThisMonthProfit = await _db.SaleDetails
                .Where(d => d.Sale.SaleDate >= firstOfThisMonth)
                .SumAsync(d => d.Total - (d.Quantity * d.Product.PurchasePrice));

            // 2. Inventory KPIs & Low Stock
            var stockBalances = await _db.Products
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.MinimumStock,
                    QoH = p.StockTransactions.Sum(t => t.QuantityIn - t.QuantityOut)
                }).ToListAsync();

            dto.Kpis.TotalProductsCount = stockBalances.Count;
            dto.Kpis.OutOfStockCount = stockBalances.Count(p => p.QoH <= 0);
            dto.Kpis.LowStockCount = stockBalances.Count(p => p.QoH > 0 && p.QoH <= p.MinimumStock);

            dto.LowStockAlerts = stockBalances
                .Where(p => p.QoH <= p.MinimumStock)
                .OrderBy(p => p.QoH - p.MinimumStock) // Most urgently low first
                .Take(10)
                .Select(p => new DashboardLowStockItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    QuantityOnHand = p.QoH,
                    MinimumStock = p.MinimumStock
                }).ToList();

            // 3. Supplier Dues
            var supplierDues = await _db.Purchases
                .Where(p => p.DueAmount > 0)
                .GroupBy(p => new { p.SupplierId, p.Supplier.Name })
                .Select(g => new { g.Key.SupplierId, g.Key.Name, TotalDue = g.Sum(x => x.DueAmount) })
                .ToListAsync();

            dto.Kpis.TotalSupplierDue = supplierDues.Sum(s => s.TotalDue);
            dto.Kpis.SuppliersWithDueCount = supplierDues.Count;

            dto.TopSupplierDues = supplierDues
                .OrderByDescending(s => s.TotalDue)
                .Take(5)
                .Select(s => new DashboardSupplierDueDto
                {
                    SupplierId = s.SupplierId,
                    SupplierName = s.Name,
                    TotalDue = s.TotalDue
                }).ToList();

            // 4. Sales Trend (Last 30 Days)
            var rawTrend = await _db.Sales
                .Where(s => s.SaleDate >= thirtyDaysAgo)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new { Date = g.Key, Sales = g.Sum(s => s.GrandTotal) })
                .ToDictionaryAsync(g => g.Date, g => g.Sales);

            for (int i = 0; i <= 29; i++)
            {
                var targetDate = thirtyDaysAgo.AddDays(i).Date;
                dto.SalesTrend.Add(new SalesTrendPointDto
                {
                    Date = targetDate,
                    Sales = rawTrend.TryGetValue(targetDate, out var val) ? val : 0
                });
            }

            // 5. Category Share (This Month)
            var catSales = await _db.SaleDetails
                .Where(d => d.Sale.SaleDate >= firstOfThisMonth)
                .GroupBy(d => d.Product.Category.Name)
                .Select(g => new { CategoryName = g.Key, Revenue = g.Sum(d => d.Total) })
                .ToListAsync();

            var totalCatSales = catSales.Sum(c => c.Revenue);
            dto.SalesByCategory = catSales
                .OrderByDescending(c => c.Revenue)
                .Select(c => new DashboardCategoryShareDto
                {
                    CategoryName = c.CategoryName,
                    Revenue = c.Revenue,
                    PercentOfTotal = totalCatSales > 0 ? (c.Revenue / totalCatSales) * 100 : 0
                }).ToList();

            // 6. Top Products (This Month)
            dto.TopProducts = await _db.SaleDetails
                .Where(d => d.Sale.SaleDate >= firstOfThisMonth)
                .GroupBy(d => new { d.ProductId, d.Product.Name })
                .Select(g => new DashboardTopProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(d => d.Quantity),
                    Revenue = g.Sum(d => d.Total)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(5)
                .ToListAsync();

            // 7. Recent Sales
            dto.RecentSales = await _db.Sales
                .Include(s => s.PaymentMethod)
                .OrderByDescending(s => s.SaleDate).ThenByDescending(s => s.Id)
                .Take(10)
                .Select(s => new DashboardRecentSaleDto
                {
                    SaleId = s.Id,
                    InvoiceNo = s.InvoiceNo,
                    SaleDate = s.SaleDate,
                    CustomerName = s.CustomerName,
                    GrandTotal = s.GrandTotal,
                    PaymentMethodName = s.PaymentMethod != null ? s.PaymentMethod.Name : "N/A"
                }).ToListAsync();

            // 8. Payment Method Breakdown (Today)
            var pmSales = await _db.Sales
                .Where(s => s.SaleDate >= today && s.PaymentMethodId != null)
                .GroupBy(s => s.PaymentMethod!.Name)
                .Select(g => new { Method = g.Key, Amount = g.Sum(s => s.PaidAmount) }) // Using PaidAmount for method breakdown
                .ToListAsync();

            var totalPmSales = pmSales.Sum(p => p.Amount);
            dto.TodayPaymentMethodBreakdown = pmSales
                .Select(p => new DashboardPaymentMethodShareDto
                {
                    PaymentMethodName = p.Method,
                    Amount = p.Amount,
                    PercentOfTotal = totalPmSales > 0 ? (p.Amount / totalPmSales) * 100 : 0
                }).ToList();

            // 9. Expenses (Stubbed gracefully)
            dto.Kpis.TodayExpenses = null;

            return dto;
        }

        private static decimal CalculatePercentChange(decimal oldVal, decimal newVal)
        {
            if (oldVal == 0) return newVal > 0 ? 100 : 0;
            return Math.Round(((newVal - oldVal) / oldVal) * 100, 2);
        }
    }
}
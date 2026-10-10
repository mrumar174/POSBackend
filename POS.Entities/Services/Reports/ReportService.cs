using Microsoft.EntityFrameworkCore;
using POS.DTOs.Reports;
using POS.Entities.Data;
using POS.Entities.IServices.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // Shared helpers
        // ============================================================

        private static (DateTime from, DateTime toExclusive) ResolveRange(DateTime? fromDate, DateTime? toDate)
        {
            var to = (toDate ?? DateTime.Today).Date;
            var from = (fromDate ?? new DateTime(to.Year, to.Month, 1)).Date;
            return (from, to.AddDays(1)); // use "< toExclusive" in queries for inclusive end-of-day
        }

        private static List<DateTime> EnumerateDays(DateTime from, DateTime toExclusive)
        {
            var days = new List<DateTime>();
            for (var d = from.Date; d < toExclusive.Date; d = d.AddDays(1))
                days.Add(d);
            return days;
        }

        // ============================================================
        // AVAILABLE NOW — Purchasing reports
        // ============================================================

        public async Task<PurchaseSummaryReportDto> GetPurchaseSummaryAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);

            var purchases = await _db.Purchases
                .Where(p => p.PurchaseDate >= from && p.PurchaseDate < toExclusive)
                .Select(p => new
                {
                    p.PurchaseDate,
                    p.SubTotal,
                    p.Discount,
                    p.Tax,
                    p.GrandTotal,
                    p.PaidAmount,
                    p.DueAmount
                })
                .ToListAsync();

            var dailyGroups = purchases
                .GroupBy(p => p.PurchaseDate.Date)
                .ToDictionary(g => g.Key, g => new DailyPurchasePointDto
                {
                    Date = g.Key,
                    InvoiceCount = g.Count(),
                    NetPurchases = g.Sum(x => x.GrandTotal)
                });

            var dailyBreakdown = EnumerateDays(from, toExclusive)
                .Select(d => dailyGroups.TryGetValue(d, out var point)
                    ? point
                    : new DailyPurchasePointDto { Date = d, InvoiceCount = 0, NetPurchases = 0 })
                .ToList();

            return new PurchaseSummaryReportDto
            {
                FromDate = from,
                ToDate = toExclusive.AddDays(-1),
                TotalPurchaseInvoices = purchases.Count,
                TotalGrossPurchases = purchases.Sum(p => p.SubTotal),
                TotalDiscount = purchases.Sum(p => p.Discount),
                TotalTax = purchases.Sum(p => p.Tax),
                TotalNetPurchases = purchases.Sum(p => p.GrandTotal),
                TotalPaid = purchases.Sum(p => p.PaidAmount),
                TotalOutstandingDue = purchases.Sum(p => p.DueAmount),
                DailyBreakdown = dailyBreakdown
            };
        }

        public async Task<List<SupplierPurchaseReportRowDto>> GetPurchasesBySupplierAsync(
            DateTime? fromDate, DateTime? toDate, string sortBy)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);

            var rows = await _db.Purchases
                .Include(p => p.Supplier)
                .Where(p => p.PurchaseDate >= from && p.PurchaseDate < toExclusive)
                .GroupBy(p => new { p.SupplierId, p.Supplier.Name })
                .Select(g => new SupplierPurchaseReportRowDto
                {
                    SupplierId = g.Key.SupplierId,
                    SupplierName = g.Key.Name,
                    InvoiceCount = g.Count(),
                    TotalPurchased = g.Sum(p => p.GrandTotal),
                    TotalPaid = g.Sum(p => p.PaidAmount),
                    TotalDue = g.Sum(p => p.DueAmount)
                })
                .ToListAsync();

            return sortBy?.ToLower() == "totaldue"
                ? rows.OrderByDescending(r => r.TotalDue).ToList()
                : rows.OrderByDescending(r => r.TotalPurchased).ToList();
        }

        public async Task<List<SupplierDueReportRowDto>> GetSupplierDuesAsync(int? minDaysOverdue)
        {
            var today = DateTime.Today;

            var purchasesWithDue = await _db.Purchases
                .Include(p => p.Supplier)
                .Where(p => p.DueAmount > 0)
                .Select(p => new { p.SupplierId, p.Supplier.Name, p.Supplier.ContactNo, p.DueAmount, p.PurchaseDate })
                .ToListAsync();

            var grouped = purchasesWithDue
                .GroupBy(p => new { p.SupplierId, p.Name, p.ContactNo })
                .Select(g =>
                {
                    var oldest = g.Min(x => x.PurchaseDate);
                    var daysOverdue = (int)(today - oldest).TotalDays;
                    return new SupplierDueReportRowDto
                    {
                        SupplierId = g.Key.SupplierId,
                        SupplierName = g.Key.Name,
                        ContactNo = g.Key.ContactNo,
                        UnpaidInvoiceCount = g.Count(),
                        TotalDue = g.Sum(x => x.DueAmount),
                        OldestUnpaidInvoiceDate = oldest,
                        DaysOverdue = daysOverdue
                    };
                })
                .Where(r => !minDaysOverdue.HasValue || r.DaysOverdue >= minDaysOverdue.Value)
                .OrderByDescending(r => r.TotalDue)
                .ToList();

            return grouped;
        }

        // ============================================================
        // AVAILABLE NOW — Stock reports (computed directly from
        // StockTransaction — no separate Stock service dependency)
        // ============================================================

        // ASSUMPTION: StockTransactionType enum member names — confirm
        // against your actual enum if these don't match (PURCHASE,
        // PURCHASE_RETURN are confirmed from PurchaseService; the rest
        // are guesses following the same naming convention).
        private static string TransactionTypeLabel(object transactionType)
        {
            var name = transactionType?.ToString() ?? "UNKNOWN";
            return name switch
            {
                "PURCHASE" => "Purchase",
                "PURCHASE_RETURN" => "Purchase Return",
                "SALE" => "Sale",
                "SALE_RETURN" => "Sale Return",
                "ADJUSTMENT" => "Stock Adjustment",
                "OPENING_STOCK" => "Opening Stock",
                _ => name
            };
        }

        private async Task<Dictionary<int, decimal>> GetOnHandByProductAsync()
        {
            return await _db.StockTransactions
                .GroupBy(s => s.ProductId)
                .Select(g => new { ProductId = g.Key, OnHand = g.Sum(x => x.QuantityIn) - g.Sum(x => x.QuantityOut) })
                .ToDictionaryAsync(x => x.ProductId, x => x.OnHand);
        }

        public async Task<StockValuationSummaryDto> GetStockValuationAsync(int? categoryId)
        {
            var onHandByProduct = await GetOnHandByProductAsync();

            var productsQuery = _db.Products.Include(p => p.Category).AsQueryable();
            if (categoryId.HasValue)
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);

            var products = await productsQuery
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ProductCode,
                    CategoryName = p.Category.Name,
                    p.PurchasePrice,
                    p.SalePrice
                })
                .ToListAsync();

            var items = products.Select(p =>
            {
                var onHand = onHandByProduct.GetValueOrDefault(p.Id, 0);
                var valueAtCost = onHand * p.PurchasePrice;
                var valueAtSale = onHand * p.SalePrice;
                return new StockValuationReportRowDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ProductCode = p.ProductCode,
                    CategoryName = p.CategoryName,
                    QuantityOnHand = onHand,
                    UnitCost = p.PurchasePrice,
                    UnitSalePrice = p.SalePrice,
                    ValueAtCost = valueAtCost,
                    ValueAtSalePrice = valueAtSale,
                    PotentialProfit = valueAtSale - valueAtCost
                };
            })
            .Where(i => i.QuantityOnHand != 0) // skip products with zero stock — not useful in a valuation report
            .OrderByDescending(i => i.ValueAtCost)
            .ToList();

            return new StockValuationSummaryDto
            {
                TotalValueAtCost = items.Sum(i => i.ValueAtCost),
                TotalValueAtSalePrice = items.Sum(i => i.ValueAtSalePrice),
                TotalPotentialProfit = items.Sum(i => i.PotentialProfit),
                Items = items
            };
        }

        public async Task<List<LowStockReportRowDto>> GetLowStockAsync(int? categoryId)
        {
            var onHandByProduct = await GetOnHandByProductAsync();

            var productsQuery = _db.Products.Include(p => p.Category).AsQueryable();
            if (categoryId.HasValue)
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);

            var products = await productsQuery
                .Select(p => new { p.Id, p.Name, p.ProductCode, CategoryName = p.Category.Name, p.MinimumStock })
                .ToListAsync();

            var lowStockProducts = products
                .Select(p => new { p.Id, p.Name, p.ProductCode, p.CategoryName, p.MinimumStock, OnHand = onHandByProduct.GetValueOrDefault(p.Id, 0) })
                .Where(p => p.OnHand <= p.MinimumStock)
                .ToList();

            var result = new List<LowStockReportRowDto>();

            foreach (var p in lowStockProducts)
            {
                var lastPurchaseLine = await _db.PurchaseDetails
                    .Include(d => d.Purchase).ThenInclude(pu => pu.Supplier)
                    .Where(d => d.ProductId == p.Id)
                    .OrderByDescending(d => d.Purchase.PurchaseDate)
                    .Select(d => new { d.Purchase.PurchaseDate, d.Purchase.Supplier.Name })
                    .FirstOrDefaultAsync();

                var suggestedReorder = Math.Max(0, (p.MinimumStock * 2) - p.OnHand);

                result.Add(new LowStockReportRowDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ProductCode = p.ProductCode,
                    CategoryName = p.CategoryName,
                    QuantityOnHand = p.OnHand,
                    MinimumStock = p.MinimumStock,
                    SuggestedReorderQuantity = suggestedReorder,
                    LastPurchaseDate = lastPurchaseLine?.PurchaseDate,
                    PreferredSupplierName = lastPurchaseLine?.Name
                });
            }

            return result.OrderBy(r => r.QuantityOnHand - r.MinimumStock).ToList(); // most urgently low first
        }

        public async Task<List<StockMovementRowDto>> GetStockMovementAsync(int productId, DateTime? fromDate, DateTime? toDate)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);

            // Starting balance: everything before `from`, so RunningBalance
            // reflects the true cumulative total, not just activity in-range.
            var priorTransactions = await _db.StockTransactions
                .Where(s => s.ProductId == productId && s.TransactionDate < from)
                .ToListAsync();
            var runningBalance = priorTransactions.Sum(s => s.QuantityIn) - priorTransactions.Sum(s => s.QuantityOut);

            var transactionsInRange = await _db.StockTransactions
                .Where(s => s.ProductId == productId && s.TransactionDate >= from && s.TransactionDate < toExclusive)
                .OrderBy(s => s.TransactionDate)
                .ThenBy(s => s.Id)
                .ToListAsync();

            // Batch-resolve reference numbers by ReferenceType, avoiding N+1 queries
            var purchaseIds = transactionsInRange.Where(t => t.ReferenceType == "Purchase" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).ToList();
            var purchaseReturnIds = transactionsInRange.Where(t => t.ReferenceType == "PurchaseReturn" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).ToList();

            var purchaseInvoiceNos = await _db.Purchases.Where(p => purchaseIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.InvoiceNo);
            var purchaseReturnNos = await _db.PurchaseReturns.Where(r => purchaseReturnIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.ReturnNo);
            // Add Sale/SaleReturn/StockAdjustment lookups here once those modules exist, following the same pattern.

            var result = new List<StockMovementRowDto>();
            foreach (var t in transactionsInRange)
            {
                runningBalance += t.QuantityIn - t.QuantityOut;

                string? referenceNo = t.ReferenceType switch
                {
                    "Purchase" when t.ReferenceId.HasValue => purchaseInvoiceNos.GetValueOrDefault(t.ReferenceId.Value),
                    "PurchaseReturn" when t.ReferenceId.HasValue => purchaseReturnNos.GetValueOrDefault(t.ReferenceId.Value),
                    _ => null
                };

                result.Add(new StockMovementRowDto
                {
                    TransactionDate = t.TransactionDate,
                    TransactionType = TransactionTypeLabel(t.TransactionType),
                    ReferenceNo = referenceNo,
                    QuantityIn = t.QuantityIn,
                    QuantityOut = t.QuantityOut,
                    RunningBalance = runningBalance,
                    Remarks = t.Remarks
                });
            }

            return result;
        }

        // ============================================================
        // AVAILABLE NOW (partial) — Tax report
        // Purchase side is real; Sale side is 0 until Sales module exists.
        // ============================================================

        public async Task<TaxReportDto> GetTaxReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);

            var purchases = await _db.Purchases
                .Where(p => p.PurchaseDate >= from && p.PurchaseDate < toExclusive)
                .Select(p => new { p.PurchaseDate, p.Tax })
                .ToListAsync();

            var purchaseTaxByDay = purchases
                .GroupBy(p => p.PurchaseDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Tax));

            // ---- Uncomment once Sale exists, matching this exact shape: ----
            // var sales = await _db.Sales
            //     .Where(s => s.SaleDate >= from && s.SaleDate < toExclusive)
            //     .Select(s => new { s.SaleDate, s.Tax })
            //     .ToListAsync();
            // var salesTaxByDay = sales
            //     .GroupBy(s => s.SaleDate.Date)
            //     .ToDictionary(g => g.Key, g => g.Sum(x => x.Tax));
            var salesTaxByDay = new Dictionary<DateTime, decimal>(); // empty until Sales exists

            var dailyBreakdown = EnumerateDays(from, toExclusive)
                .Select(d => new DailyTaxPointDto
                {
                    Date = d,
                    TaxCollected = salesTaxByDay.GetValueOrDefault(d, 0),
                    TaxPaid = purchaseTaxByDay.GetValueOrDefault(d, 0)
                })
                .ToList();

            var totalCollected = dailyBreakdown.Sum(d => d.TaxCollected);
            var totalPaid = dailyBreakdown.Sum(d => d.TaxPaid);

            return new TaxReportDto
            {
                FromDate = from,
                ToDate = toExclusive.AddDays(-1),
                TaxCollectedOnSales = totalCollected,
                TaxPaidOnPurchases = totalPaid,
                NetTaxLiability = totalCollected - totalPaid,
                DailyBreakdown = dailyBreakdown
            };
        }

        // ============================================================
        // REQUIRES SALES MODULE — references Sale/SaleDetail/User.
        // Remove everything below this point (and the matching interface
        // + controller methods) if those entities don't exist yet.
        // ============================================================

        public async Task<SalesSummaryReportDto> GetSalesSummaryAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);

            var sales = await _db.Sales
                .Where(s => s.SaleDate >= from && s.SaleDate < toExclusive)
                .Select(s => new { s.SaleDate, s.SubTotal, s.Discount, s.Tax, s.GrandTotal, s.PaidAmount })
                .ToListAsync();

            var dailyGroups = sales
                .GroupBy(s => s.SaleDate.Date)
                .ToDictionary(g => g.Key, g => new DailySalesPointDto
                {
                    Date = g.Key,
                    InvoiceCount = g.Count(),
                    NetSales = g.Sum(x => x.GrandTotal)
                });

            var dailyBreakdown = EnumerateDays(from, toExclusive)
                .Select(d => dailyGroups.TryGetValue(d, out var point)
                    ? point
                    : new DailySalesPointDto { Date = d, InvoiceCount = 0, NetSales = 0 })
                .ToList();

            var totalInvoices = sales.Count;
            var totalNetSales = sales.Sum(s => s.GrandTotal);

            return new SalesSummaryReportDto
            {
                FromDate = from,
                ToDate = toExclusive.AddDays(-1),
                TotalInvoices = totalInvoices,
                TotalGrossSales = sales.Sum(s => s.SubTotal),
                TotalDiscount = sales.Sum(s => s.Discount),
                TotalTax = sales.Sum(s => s.Tax),
                TotalNetSales = totalNetSales,
                TotalCollected = sales.Sum(s => s.PaidAmount),
                AverageInvoiceValue = totalInvoices > 0 ? totalNetSales / totalInvoices : 0,
                DailyBreakdown = dailyBreakdown
            };
        }

        public async Task<List<ProductSalesReportRowDto>> GetSalesByProductAsync(
            DateTime? fromDate, DateTime? toDate, int? categoryId, string sortBy, int top)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);
            top = Math.Clamp(top <= 0 ? 20 : top, 1, 100);

            var query = _db.SaleDetails
                .Include(d => d.Sale)
                .Include(d => d.Product).ThenInclude(p => p.Category)
                .Where(d => d.Sale.SaleDate >= from && d.Sale.SaleDate < toExclusive);

            if (categoryId.HasValue)
                query = query.Where(d => d.Product.CategoryId == categoryId.Value);

            var rows = await query
                .GroupBy(d => new { d.ProductId, d.Product.Name, d.Product.ProductCode, CategoryName = d.Product.Category.Name })
                .Select(g => new ProductSalesReportRowDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    ProductCode = g.Key.ProductCode,
                    CategoryName = g.Key.CategoryName,
                    QuantitySold = g.Sum(d => d.Quantity),
                    TotalRevenue = g.Sum(d => d.Total),
                    TotalDiscount = g.Sum(d => d.Discount)
                })
                .ToListAsync();

            foreach (var row in rows)
                row.AverageSellingPrice = row.QuantitySold > 0 ? row.TotalRevenue / row.QuantitySold : 0;

            var sorted = sortBy?.ToLower() == "quantity"
                ? rows.OrderByDescending(r => r.QuantitySold)
                : rows.OrderByDescending(r => r.TotalRevenue);

            return sorted.Take(top).ToList();
        }

        public async Task<List<CategorySalesReportRowDto>> GetSalesByCategoryAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);

            var rows = await _db.SaleDetails
                .Include(d => d.Sale)
                .Include(d => d.Product).ThenInclude(p => p.Category)
                .Where(d => d.Sale.SaleDate >= from && d.Sale.SaleDate < toExclusive)
                .GroupBy(d => new { d.Product.CategoryId, d.Product.Category.Name })
                .Select(g => new CategorySalesReportRowDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    QuantitySold = g.Sum(d => d.Quantity),
                    TotalRevenue = g.Sum(d => d.Total)
                })
                .ToListAsync();

            var grandTotal = rows.Sum(r => r.TotalRevenue);
            foreach (var row in rows)
                row.PercentOfTotalSales = grandTotal > 0 ? Math.Round(row.TotalRevenue / grandTotal * 100, 2) : 0;

            return rows.OrderByDescending(r => r.TotalRevenue).ToList();
        }

        public async Task<ProfitSummaryReportDto> GetProfitReportAsync(DateTime? fromDate, DateTime? toDate, int? categoryId)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);

            var query = _db.SaleDetails
                .Include(d => d.Sale)
                .Include(d => d.Product)
                .Where(d => d.Sale.SaleDate >= from && d.Sale.SaleDate < toExclusive);

            if (categoryId.HasValue)
                query = query.Where(d => d.Product.CategoryId == categoryId.Value);

            var rows = await query
                .GroupBy(d => new { d.ProductId, d.Product.Name, d.Product.PurchasePrice })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Key.PurchasePrice,
                    QuantitySold = g.Sum(d => d.Quantity),
                    TotalRevenue = g.Sum(d => d.Total)
                })
                .ToListAsync();

            var items = rows.Select(r =>
            {
                var totalCost = r.QuantitySold * r.PurchasePrice;
                var grossProfit = r.TotalRevenue - totalCost;
                return new ProfitReportRowDto
                {
                    ProductId = r.ProductId,
                    ProductName = r.Name,
                    QuantitySold = r.QuantitySold,
                    TotalRevenue = r.TotalRevenue,
                    TotalCost = totalCost,
                    GrossProfit = grossProfit,
                    MarginPercent = r.TotalRevenue > 0 ? Math.Round(grossProfit / r.TotalRevenue * 100, 2) : 0
                };
            })
            .OrderByDescending(i => i.GrossProfit)
            .ToList();

            var totalRevenue = items.Sum(i => i.TotalRevenue);
            var totalCost = items.Sum(i => i.TotalCost);
            var totalGrossProfit = totalRevenue - totalCost;

            return new ProfitSummaryReportDto
            {
                TotalRevenue = totalRevenue,
                TotalCost = totalCost,
                TotalGrossProfit = totalGrossProfit,
                OverallMarginPercent = totalRevenue > 0 ? Math.Round(totalGrossProfit / totalRevenue * 100, 2) : 0,
                Items = items
            };
        }

        public async Task<List<CashierPerformanceRowDto>> GetCashierPerformanceAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);

            var sales = await _db.Sales
                .Where(s => s.SaleDate >= from && s.SaleDate < toExclusive && s.AddedBy.HasValue)
                .Select(s => new { s.AddedBy, s.GrandTotal })
                .ToListAsync();

            var userIds = sales.Select(s => s.AddedBy!.Value).Distinct().ToList();
            var userNames = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);

            var rows = sales
                .GroupBy(s => s.AddedBy!.Value)
                .Select(g =>
                {
                    var total = g.Sum(x => x.GrandTotal);
                    var count = g.Count();
                    return new CashierPerformanceRowDto
                    {
                        UserId = g.Key,
                        UserFullName = userNames.GetValueOrDefault(g.Key, "Unknown"),
                        InvoiceCount = count,
                        TotalSales = total,
                        AverageInvoiceValue = count > 0 ? total / count : 0
                    };
                })
                .OrderByDescending(r => r.TotalSales)
                .ToList();

            return rows;
        }

        // ============================================================
        // REQUIRES EXPENSES MODULE — references Expense/ExpenseCategory.
        // Remove if those entities don't exist yet.
        // ============================================================

        public async Task<ExpenseSummaryReportDto> GetExpenseReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (from, toExclusive) = ResolveRange(fromDate, toDate);

            var expenses = await _db.Expenses
                .Include(e => e.ExpenseCategory)
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < toExclusive)
                .Select(e => new { e.ExpenseCategoryId, e.ExpenseCategory.Name, e.ExpenseDate, e.Amount })
                .ToListAsync();

            var totalExpenses = expenses.Sum(e => e.Amount);

            var byCategory = expenses
                .GroupBy(e => new { e.ExpenseCategoryId, e.Name })
                .Select(g => new ExpenseReportRowDto
                {
                    ExpenseCategoryId = g.Key.ExpenseCategoryId,
                    ExpenseCategoryName = g.Key.Name,
                    ExpenseCount = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount),
                    PercentOfTotal = totalExpenses > 0 ? Math.Round(g.Sum(x => x.Amount) / totalExpenses * 100, 2) : 0
                })
                .OrderByDescending(r => r.TotalAmount)
                .ToList();

            var dailyGroups = expenses
                .GroupBy(e => e.ExpenseDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var dailyBreakdown = EnumerateDays(from, toExclusive)
                .Select(d => new DailyExpensePointDto { Date = d, Amount = dailyGroups.GetValueOrDefault(d, 0) })
                .ToList();

            return new ExpenseSummaryReportDto
            {
                TotalExpenses = totalExpenses,
                ByCategory = byCategory,
                DailyBreakdown = dailyBreakdown
            };
        }
    }
}

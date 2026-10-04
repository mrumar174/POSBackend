using Microsoft.EntityFrameworkCore;
using POS.DTOs.Common;
using POS.DTOs.Sales;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Inventory;
using POS.Entities.IServices;
using POS.Entities.IServices.Inventory;
using POS.Entities.IServices.Sales;
using POS.Entities.Sales;

namespace POS.Entities.Services.Sales
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IStockService _stockService;

        public SaleService(ApplicationDbContext db, ICurrentUserService currentUser, IStockService stockService)
        {
            _db = db;
            _currentUser = currentUser;
            _stockService = stockService;
        }

        private IQueryable<SaleDto> ProjectToDto() =>
            _db.Sales
                .Include(s => s.PaymentMethod)
                .Include(s => s.SaleDetails).ThenInclude(d => d.Product)
                .Select(s => new SaleDto
                {
                    Id = s.Id,
                    InvoiceNo = s.InvoiceNo,
                    SaleDate = s.SaleDate,
                    SubTotal = s.SubTotal,
                    Discount = s.Discount,
                    Tax = s.Tax,
                    GrandTotal = s.GrandTotal,
                    PaidAmount = s.PaidAmount,
                    DueAmount = s.DueAmount,
                    PaymentMethodId = s.PaymentMethodId,
                    PaymentMethodName = s.PaymentMethod != null ? s.PaymentMethod.Name : null,
                    Remarks = s.Remarks,
                    Items = s.SaleDetails.Select(d => new SaleDetailDto
                    {
                        Id = d.Id,
                        ProductId = d.ProductId,
                        ProductName = d.Product.Name,
                        Quantity = d.Quantity,
                        SalePrice = d.SalePrice,
                        Discount = d.Discount,
                        Tax = d.Tax,
                        Total = d.Total
                    }).ToList()
                });

        public async Task<List<SaleDto>> GetAllAsync()
            => await ProjectToDto().OrderByDescending(s => s.SaleDate).ThenByDescending(s => s.Id).ToListAsync();

        public async Task<SaleDto?> GetByIdAsync(int id)
            => await ProjectToDto().FirstOrDefaultAsync(s => s.Id == id);

        public async Task<SaleDto> CreateAsync(CreateSaleDto dto)
        {
            ValidateItems(dto.Items);

            var shopId = _currentUser.ShopId ?? throw new InvalidOperationException("No shop selected.");
            var invoiceNo = await GenerateNextInvoiceNoAsync(shopId);

            // Group requested items to handle duplicate product rows in the same cart gracefully
            var productDemands = dto.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                .ToList();

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // CRITICAL: Stock Validation
                foreach (var demand in productDemands)
                {
                    var currentStock = await _stockService.GetProductStockAsync(demand.ProductId);
                    if (demand.TotalQuantity > currentStock)
                    {
                        var productName = await _db.Products.Where(p => p.Id == demand.ProductId).Select(p => p.Name).FirstOrDefaultAsync();
                        throw new InvalidOperationException($"Insufficient stock for '{productName}'. Available: {currentStock:0.###}, Requested: {demand.TotalQuantity:0.###}");
                    }
                }

                var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.Discount, dto.Tax);

                var sale = new Sale
                {
                    InvoiceNo = invoiceNo,
                    SaleDate = dto.SaleDate,
                    SubTotal = subTotal,
                    Discount = dto.Discount,
                    Tax = dto.Tax,
                    GrandTotal = grandTotal,
                    PaidAmount = dto.PaidAmount,
                    // If PaidAmount > GrandTotal, DueAmount is 0 (Change is frontend computed)
                    DueAmount = Math.Max(0, grandTotal - dto.PaidAmount),
                    PaymentMethodId = dto.PaymentMethodId,
                    Remarks = dto.Remarks
                };

                foreach (var item in dto.Items)
                {
                    var lineTotal = (item.Quantity * item.SalePrice) - item.Discount + item.Tax;

                    sale.SaleDetails.Add(new SaleDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice,
                        Discount = item.Discount,
                        Tax = item.Tax,
                        Total = Math.Max(0, lineTotal)
                    });
                }

                _db.Sales.Add(sale);
                await _db.SaveChangesAsync(); // Generates Sale.Id for StockTransactions

                ApplyStockTransactions(sale, dto.Items);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetByIdAsync(sale.Id) ?? throw new InvalidOperationException("Sale created but failed to reload.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SaleDto> UpdateAsync(int id, CreateSaleDto dto)
        {
            var sale = await _db.Sales
                .Include(s => s.SaleDetails)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Sale {id} not found.");

            var hasReturns = await _db.SaleReturns.AnyAsync(r => r.SaleId == id);
            if (hasReturns)
                throw new InvalidOperationException("Cannot edit a sale that has returns recorded against it.");

            ValidateItems(dto.Items);

            var productDemands = dto.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                .ToList();

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Reverse old stock impact completely
                var oldStockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "Sale" && s.ReferenceId == sale.Id)
                    .ToListAsync();
                _db.StockTransactions.RemoveRange(oldStockRows);

                // Save changes here so the reversed stock is visible to IStockService validation
                await _db.SaveChangesAsync();

                // 2. Validate new stock demands against restored inventory
                foreach (var demand in productDemands)
                {
                    var currentStock = await _stockService.GetProductStockAsync(demand.ProductId);
                    if (demand.TotalQuantity > currentStock)
                    {
                        var productName = await _db.Products.Where(p => p.Id == demand.ProductId).Select(p => p.Name).FirstOrDefaultAsync();
                        throw new InvalidOperationException($"Insufficient stock for '{productName}'. Available: {currentStock:0.###}, Requested: {demand.TotalQuantity:0.###}");
                    }
                }

                // 3. Clear old items
                _db.SaleDetails.RemoveRange(sale.SaleDetails);
                sale.SaleDetails.Clear();

                // 4. Update Header
                var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.Discount, dto.Tax);

                sale.SaleDate = dto.SaleDate;
                sale.SubTotal = subTotal;
                sale.Discount = dto.Discount;
                sale.Tax = dto.Tax;
                sale.GrandTotal = grandTotal;
                sale.PaidAmount = dto.PaidAmount;
                sale.DueAmount = Math.Max(0, grandTotal - dto.PaidAmount);
                sale.PaymentMethodId = dto.PaymentMethodId;
                sale.Remarks = dto.Remarks;

                // 5. Append new items
                foreach (var item in dto.Items)
                {
                    var lineTotal = (item.Quantity * item.SalePrice) - item.Discount + item.Tax;
                    sale.SaleDetails.Add(new SaleDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice,
                        Discount = item.Discount,
                        Tax = item.Tax,
                        Total = Math.Max(0, lineTotal)
                    });
                }

                await _db.SaveChangesAsync();

                // 6. Re-apply new stock out transactions
                ApplyStockTransactions(sale, dto.Items);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetByIdAsync(sale.Id) ?? throw new InvalidOperationException("Sale updated but failed to reload.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var sale = await _db.Sales
                .Include(s => s.SaleDetails)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Sale {id} not found.");

            var hasReturns = await _db.SaleReturns.AnyAsync(r => r.SaleId == id);
            if (hasReturns)
                throw new InvalidOperationException("Cannot delete a sale that has returns recorded against it. Delete the return(s) first.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var stockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "Sale" && s.ReferenceId == id)
                    .ToListAsync();

                _db.StockTransactions.RemoveRange(stockRows);
                _db.SaleDetails.RemoveRange(sale.SaleDetails);
                _db.Sales.Remove(sale);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private void ApplyStockTransactions(Sale sale, List<CreateSaleDetailDto> items)
        {
            foreach (var item in items)
            {
                _db.StockTransactions.Add(new StockTransaction
                {
                    ProductId = item.ProductId,
                    TransactionType = StockTransactionType.SALE,
                    QuantityIn = 0,
                    QuantityOut = item.Quantity,
                    ReferenceType = "Sale",
                    ReferenceId = sale.Id,
                    TransactionDate = sale.SaleDate,
                    Remarks = $"Sale against invoice No.: {sale.InvoiceNo}"
                });
            }
        }

        private static (decimal subTotal, decimal grandTotal) CalculateTotals(List<CreateSaleDetailDto> items, decimal headerDiscount, decimal headerTax)
        {
            decimal subTotal = 0;

            foreach (var item in items)
            {
                var lineTotal = (item.Quantity * item.SalePrice) - item.Discount + item.Tax;
                subTotal += Math.Max(0, lineTotal);
            }

            var grandTotal = Math.Max(0, subTotal - headerDiscount + headerTax);
            return (subTotal, grandTotal);
        }

        private static void ValidateItems(List<CreateSaleDetailDto> items)
        {
            if (items is null || items.Count == 0)
                throw new InvalidOperationException("A sale must have at least one item.");

            foreach (var item in items)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException("Item quantity must be greater than zero.");
                if (item.SalePrice < 0)
                    throw new InvalidOperationException("Item sale price cannot be negative.");
            }
        }

        private async Task<string> GenerateNextInvoiceNoAsync(int shopId)
        {
            var shop = await _db.Shops.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == shopId)
                ?? throw new InvalidOperationException("Shop not found.");

            var prefix = string.IsNullOrWhiteSpace(shop.InvoicePrefix) ? "SAL" : shop.InvoicePrefix;

            var invoiceNos = await _db.Sales
                .IgnoreQueryFilters()
                .Where(s => s.ShopId == shopId)
                .Select(s => s.InvoiceNo)
                .ToListAsync();

            var maxNumber = 0;
            var searchPrefix = prefix + "-";
            foreach (var invoiceNo in invoiceNos)
            {
                if (invoiceNo.StartsWith(searchPrefix) &&
                    int.TryParse(invoiceNo.AsSpan(searchPrefix.Length), out var num) && num > maxNumber)
                    maxNumber = num;
            }

            return $"{prefix}-{(maxNumber + 1):D5}";
        }
    }
}
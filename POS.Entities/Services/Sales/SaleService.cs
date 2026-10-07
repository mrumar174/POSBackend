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
                    CustomerName = s.CustomerName,
                    SaleDate = s.SaleDate,
                    SubTotal = s.SubTotal,
                    DiscountType = s.DiscountType,
                    DiscountValue = s.DiscountValue,
                    Discount = s.Discount,
                    Tax = s.Tax,
                    GrandTotal = s.GrandTotal,
                    PaidAmount = s.PaidAmount,
                    DueAmount = s.DueAmount,
                    ChangeAmount = s.ChangeAmount,
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
                        DiscountType = d.DiscountType,
                        DiscountValue = d.DiscountValue,
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

            var productDemands = dto.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                .ToList();

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var demand in productDemands)
                {
                    var currentStock = await _stockService.GetProductStockAsync(demand.ProductId);
                    if (demand.TotalQuantity > currentStock)
                    {
                        var productName = await _db.Products.Where(p => p.Id == demand.ProductId).Select(p => p.Name).FirstOrDefaultAsync();
                        throw new InvalidOperationException($"Insufficient stock for '{productName}'. Available: {currentStock:0.###}, Requested: {demand.TotalQuantity:0.###}");
                    }
                }

                var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.DiscountType, dto.DiscountValue, dto.Tax, out var headerDiscount);

                var sale = new Sale
                {
                    InvoiceNo = invoiceNo,
                    CustomerName = dto.CustomerName,
                    SaleDate = dto.SaleDate,
                    SubTotal = subTotal,
                    DiscountType = dto.DiscountType,
                    DiscountValue = dto.DiscountValue,
                    Discount = headerDiscount,
                    Tax = dto.Tax,
                    GrandTotal = grandTotal,
                    PaidAmount = dto.PaidAmount,
                    DueAmount = Math.Max(0, grandTotal - dto.PaidAmount),
                    ChangeAmount = dto.PaidAmount > grandTotal ? dto.PaidAmount - grandTotal : 0,
                    PaymentMethodId = dto.PaymentMethodId,
                    Remarks = dto.Remarks
                };

                foreach (var item in dto.Items)
                {
                    var baseAmount = item.Quantity * item.SalePrice;
                    var itemDiscount = ComputeDiscountAmount(item.DiscountType, item.DiscountValue, baseAmount, item.Quantity);
                    var lineTotal = Math.Max(0, baseAmount - itemDiscount + item.Tax);

                    sale.SaleDetails.Add(new SaleDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice,
                        DiscountType = item.DiscountType,
                        DiscountValue = item.DiscountValue,
                        Discount = itemDiscount,
                        Tax = item.Tax,
                        Total = lineTotal
                    });
                }

                _db.Sales.Add(sale);
                await _db.SaveChangesAsync();

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
                var oldStockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "Sale" && s.ReferenceId == sale.Id)
                    .ToListAsync();
                _db.StockTransactions.RemoveRange(oldStockRows);

                await _db.SaveChangesAsync();

                foreach (var demand in productDemands)
                {
                    var currentStock = await _stockService.GetProductStockAsync(demand.ProductId);
                    if (demand.TotalQuantity > currentStock)
                    {
                        var productName = await _db.Products.Where(p => p.Id == demand.ProductId).Select(p => p.Name).FirstOrDefaultAsync();
                        throw new InvalidOperationException($"Insufficient stock for '{productName}'. Available: {currentStock:0.###}, Requested: {demand.TotalQuantity:0.###}");
                    }
                }

                _db.SaleDetails.RemoveRange(sale.SaleDetails);
                sale.SaleDetails.Clear();

                var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.DiscountType, dto.DiscountValue, dto.Tax, out var headerDiscount);

                sale.CustomerName = dto.CustomerName;
                sale.SaleDate = dto.SaleDate;
                sale.SubTotal = subTotal;
                sale.DiscountType = dto.DiscountType;
                sale.DiscountValue = dto.DiscountValue;
                sale.Discount = headerDiscount;
                sale.Tax = dto.Tax;
                sale.GrandTotal = grandTotal;
                sale.PaidAmount = dto.PaidAmount;
                sale.DueAmount = Math.Max(0, grandTotal - dto.PaidAmount);
                sale.ChangeAmount = dto.PaidAmount > grandTotal ? dto.PaidAmount - grandTotal : 0;
                sale.PaymentMethodId = dto.PaymentMethodId;
                sale.Remarks = dto.Remarks;

                foreach (var item in dto.Items)
                {
                    var baseAmount = item.Quantity * item.SalePrice;
                    var itemDiscount = ComputeDiscountAmount(item.DiscountType, item.DiscountValue, baseAmount, item.Quantity);
                    var lineTotal = Math.Max(0, baseAmount - itemDiscount + item.Tax);

                    sale.SaleDetails.Add(new SaleDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice,
                        DiscountType = item.DiscountType,
                        DiscountValue = item.DiscountValue,
                        Discount = itemDiscount,
                        Tax = item.Tax,
                        Total = lineTotal
                    });
                }

                await _db.SaveChangesAsync();

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

        private static (decimal subTotal, decimal grandTotal) CalculateTotals(
            List<CreateSaleDetailDto> items,
            DiscountType headerDiscountType,
            decimal headerDiscountValue,
            decimal headerTax,
            out decimal headerDiscountAmount)
        {
            decimal subTotal = 0;

            foreach (var item in items)
            {
                var baseAmount = item.Quantity * item.SalePrice;
                var itemDiscount = ComputeDiscountAmount(item.DiscountType, item.DiscountValue, baseAmount, item.Quantity);
                subTotal += Math.Max(0, baseAmount - itemDiscount + item.Tax);
            }

            headerDiscountAmount = ComputeDiscountAmount(headerDiscountType, headerDiscountValue, subTotal, items.Sum(i => i.Quantity));
            var grandTotal = Math.Max(0, subTotal - headerDiscountAmount + headerTax);

            return (subTotal, grandTotal);
        }

        private static decimal ComputeDiscountAmount(DiscountType type, decimal discountValue, decimal baseAmount, decimal quantity)
        {
            return type switch
            {
                DiscountType.Percentage => Math.Round(baseAmount * (discountValue / 100m), 2),
                DiscountType.PerPiece => Math.Round(discountValue * quantity, 2),
                DiscountType.Flat => discountValue,
                _ => discountValue
            };
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
                if (item.DiscountType == DiscountType.Percentage && item.DiscountValue > 100)
                    throw new InvalidOperationException("Percentage discount cannot exceed 100.");
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
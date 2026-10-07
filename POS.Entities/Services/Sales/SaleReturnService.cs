using Microsoft.EntityFrameworkCore;
using POS.DTOs.Common;
using POS.DTOs.Sales;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Inventory;
using POS.Entities.IServices;
using POS.Entities.IServices.Sales;
using POS.Entities.Sales;

namespace POS.Entities.Services.Sales
{
    public class SaleReturnService : ISaleReturnService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly ISaleService _saleService; // Reused for authoritative sale data

        public SaleReturnService(ApplicationDbContext db, ICurrentUserService currentUser, ISaleService saleService)
        {
            _db = db;
            _currentUser = currentUser;
            _saleService = saleService;
        }

        private IQueryable<SaleReturnDto> ProjectToDto() =>
            _db.SaleReturns
                .Include(r => r.Sale)
                .Include(r => r.SaleReturnDetails).ThenInclude(d => d.Product)
                .Select(r => new SaleReturnDto
                {
                    Id = r.Id,
                    ReturnNo = r.ReturnNo,
                    SaleId = r.SaleId,
                    InvoiceNo = r.Sale != null ? r.Sale.InvoiceNo : null,
                    CustomerName = r.Sale != null ? r.Sale.CustomerName : null,
                    ReturnDate = r.ReturnDate,
                    SubTotal = r.SubTotal,
                    Discount = r.Discount,
                    Tax = r.Tax,
                    GrandTotal = r.GrandTotal,
                    Remarks = r.Remarks,
                    Items = r.SaleReturnDetails.Select(d => new SaleReturnDetailDto
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

        public async Task<PagedResultDto<SaleReturnDto>> SearchAsync(SaleReturnQueryDto query)
        {
            var q = ProjectToDto();

            if (!string.IsNullOrWhiteSpace(query.ReturnNo))
                q = q.Where(r => r.ReturnNo.ToLower().Contains(query.ReturnNo.ToLower()));

            if (query.SaleId.HasValue)
                q = q.Where(r => r.SaleId == query.SaleId.Value);

            if (query.FromDate.HasValue)
                q = q.Where(r => r.ReturnDate >= query.FromDate.Value.Date);

            if (query.ToDate.HasValue)
                q = q.Where(r => r.ReturnDate < query.ToDate.Value.Date.AddDays(1));

            q = q.OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id);

            var totalCount = await q.CountAsync();
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDto<SaleReturnDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<SaleReturnDto?> GetByIdAsync(int id)
            => await ProjectToDto().FirstOrDefaultAsync(r => r.Id == id);

        public async Task<SaleReturnDto> CreateAsync(CreateSaleReturnDto dto)
        {
            ValidateItems(dto.Items);

            var shopId = _currentUser.ShopId ?? throw new InvalidOperationException("No shop selected.");

            if (dto.SaleId.HasValue)
                await ValidateAgainstSaleAsync(dto.SaleId.Value, dto.Items, excludingReturnId: null);

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var returnNo = await GenerateNextReturnNoAsync(shopId);
                var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.Discount, dto.Tax);

                var saleReturn = new SaleReturn
                {
                    ReturnNo = returnNo,
                    SaleId = dto.SaleId,
                    ReturnDate = dto.ReturnDate,
                    SubTotal = subTotal,
                    Discount = dto.Discount,
                    Tax = dto.Tax,
                    GrandTotal = grandTotal,
                    Remarks = dto.Remarks
                };

                foreach (var item in dto.Items)
                {
                    saleReturn.SaleReturnDetails.Add(new SaleReturnDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice,
                        Discount = item.Discount,
                        Tax = item.Tax,
                        Total = item.Total
                    });
                }

                _db.SaleReturns.Add(saleReturn);
                await _db.SaveChangesAsync();

                AddStockTransactions(saleReturn, dto.Items);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return await GetByIdAsync(saleReturn.Id) ?? throw new InvalidOperationException("Failed to reload sale return.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SaleReturnDto> UpdateAsync(int id, CreateSaleReturnDto dto)
        {
            var saleReturn = await _db.SaleReturns
                .Include(r => r.SaleReturnDetails)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Sale return {id} not found.");

            ValidateItems(dto.Items);

            if (dto.SaleId.HasValue)
                await ValidateAgainstSaleAsync(dto.SaleId.Value, dto.Items, excludingReturnId: id);

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var oldStockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "SaleReturn" && s.ReferenceId == saleReturn.Id)
                    .ToListAsync();
                _db.StockTransactions.RemoveRange(oldStockRows);

                _db.SaleReturnDetails.RemoveRange(saleReturn.SaleReturnDetails);
                saleReturn.SaleReturnDetails.Clear();

                var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.Discount, dto.Tax);

                saleReturn.SaleId = dto.SaleId;
                saleReturn.ReturnDate = dto.ReturnDate;
                saleReturn.SubTotal = subTotal;
                saleReturn.Discount = dto.Discount;
                saleReturn.Tax = dto.Tax;
                saleReturn.GrandTotal = grandTotal;
                saleReturn.Remarks = dto.Remarks;

                foreach (var item in dto.Items)
                {
                    saleReturn.SaleReturnDetails.Add(new SaleReturnDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice,
                        Discount = item.Discount,
                        Tax = item.Tax,
                        Total = item.Total
                    });
                }

                await _db.SaveChangesAsync();

                AddStockTransactions(saleReturn, dto.Items);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return await GetByIdAsync(saleReturn.Id) ?? throw new InvalidOperationException("Failed to reload sale return.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var saleReturn = await _db.SaleReturns
                .Include(r => r.SaleReturnDetails)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Sale return {id} not found.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var stockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "SaleReturn" && s.ReferenceId == id)
                    .ToListAsync();

                _db.StockTransactions.RemoveRange(stockRows);
                _db.SaleReturnDetails.RemoveRange(saleReturn.SaleReturnDetails);
                _db.SaleReturns.Remove(saleReturn);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task ValidateAgainstSaleAsync(int saleId, List<CreateSaleReturnDetailDto> items, int? excludingReturnId)
        {
            var sale = await _saleService.GetByIdAsync(saleId)
                ?? throw new InvalidOperationException($"Sale {saleId} not found.");

            var alreadyReturnedQuery = _db.SaleReturnDetails.Where(d => d.SaleReturn.SaleId == saleId);

            if (excludingReturnId.HasValue)
                alreadyReturnedQuery = alreadyReturnedQuery.Where(d => d.SaleReturnId != excludingReturnId.Value);

            var alreadyReturnedByProduct = await alreadyReturnedQuery
                .GroupBy(d => d.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

            foreach (var item in items)
            {
                var originalLine = sale.Items.FirstOrDefault(i => i.ProductId == item.ProductId)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} was not part of the original sale.");

                var alreadyReturned = alreadyReturnedByProduct.GetValueOrDefault(item.ProductId, 0);
                var maxReturnable = originalLine.Quantity - alreadyReturned;

                if (item.Quantity > maxReturnable)
                    throw new InvalidOperationException(
                        $"Cannot return {item.Quantity} of '{originalLine.ProductName}' — only {maxReturnable:0.###} remaining returnable from this sale.");
            }
        }

        private void AddStockTransactions(SaleReturn saleReturn, List<CreateSaleReturnDetailDto> items)
        {
            foreach (var item in items)
            {
                _db.StockTransactions.Add(new StockTransaction
                {
                    ProductId = item.ProductId,
                    TransactionType = StockTransactionType.SALE_RETURN,
                    QuantityIn = item.Quantity, // Item comes back INTO inventory
                    QuantityOut = 0,
                    ReferenceType = "SaleReturn",
                    ReferenceId = saleReturn.Id,
                    TransactionDate = saleReturn.ReturnDate,
                    Remarks = $"Sale return {saleReturn.ReturnNo}"
                });
            }
        }

        private static (decimal subTotal, decimal grandTotal) CalculateTotals(List<CreateSaleReturnDetailDto> items, decimal headerDiscount, decimal headerTax)
        {
            var subTotal = items.Sum(i => i.Total);
            var grandTotal = Math.Max(0, subTotal - headerDiscount + headerTax);
            return (subTotal, grandTotal);
        }

        private static void ValidateItems(List<CreateSaleReturnDetailDto> items)
        {
            if (items is null || items.Count == 0)
                throw new InvalidOperationException("A sale return must have at least one item.");

            foreach (var item in items)
            {
                if (item.Quantity <= 0) throw new InvalidOperationException("Item quantity must be greater than zero.");
                if (item.SalePrice < 0) throw new InvalidOperationException("Item sale price cannot be negative.");
            }
        }

        private async Task<string> GenerateNextReturnNoAsync(int shopId)
        {
            var prefix = "SRET";
            var returnNos = await _db.SaleReturns
                .IgnoreQueryFilters()
                .Where(r => r.ShopId == shopId)
                .Select(r => r.ReturnNo)
                .ToListAsync();

            var maxNumber = 0;
            var searchPrefix = prefix + "-";
            foreach (var returnNo in returnNos)
            {
                if (returnNo.StartsWith(searchPrefix) && int.TryParse(returnNo.AsSpan(searchPrefix.Length), out var num) && num > maxNumber)
                    maxNumber = num;
            }

            return $"{prefix}-{(maxNumber + 1):D5}";
        }
    }
}
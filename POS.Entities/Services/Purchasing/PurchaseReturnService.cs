using Microsoft.EntityFrameworkCore;
using POS.DTOs.Common;
using POS.DTOs.Purchasing;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Inventory;
using POS.Entities.IServices;
using POS.Entities.Purchasing;

namespace POS.Entities.Services
{
    public class PurchaseReturnService : IPurchaseReturnService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IPurchaseService _purchaseService; // reused for authoritative purchase/item data

        public PurchaseReturnService(ApplicationDbContext db, ICurrentUserService currentUser, IPurchaseService purchaseService)
        {
            _db = db;
            _currentUser = currentUser;
            _purchaseService = purchaseService;
        }

        private IQueryable<PurchaseReturnDto> ProjectToDto() =>
            _db.PurchaseReturns
                .Include(r => r.Supplier)
                .Include(r => r.PurchaseReturnDetails).ThenInclude(d => d.Product)
                .Select(r => new PurchaseReturnDto
                {
                    Id = r.Id,
                    ReturnNo = r.ReturnNo,
                    SupplierId = r.SupplierId,
                    SupplierName = r.Supplier.Name,
                    PurchaseId = r.PurchaseId,
                    ReturnDate = r.ReturnDate,
                    SubTotal = r.SubTotal,
                    Discount = r.Discount,
                    Tax = r.Tax,
                    GrandTotal = r.GrandTotal,
                    Remarks = r.Remarks,
                    Items = r.PurchaseReturnDetails.Select(d => new PurchaseReturnDetailDto
                    {
                        Id = d.Id,
                        ProductId = d.ProductId,
                        ProductName = d.Product.Name,
                        Quantity = d.Quantity,
                        PurchasePrice = d.PurchasePrice,
                        Discount = d.Discount,
                        Tax = d.Tax,
                        Total = d.Total
                    }).ToList()
                });

        public async Task<PagedResultDto<PurchaseReturnDto>> SearchAsync(PurchaseReturnQueryDto query)
        {
            var q = ProjectToDto();

            if (!string.IsNullOrWhiteSpace(query.ReturnNo))
                q = q.Where(r => r.ReturnNo.ToLower().Contains(query.ReturnNo.ToLower()));

            if (query.SupplierId.HasValue)
                q = q.Where(r => r.SupplierId == query.SupplierId.Value);

            if (query.PurchaseId.HasValue)
                q = q.Where(r => r.PurchaseId == query.PurchaseId.Value);

            if (query.FromDate.HasValue)
                q = q.Where(r => r.ReturnDate >= query.FromDate.Value.Date);

            if (query.ToDate.HasValue)
                q = q.Where(r => r.ReturnDate < query.ToDate.Value.Date.AddDays(1));

            q = q.OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id);

            var totalCount = await q.CountAsync();
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDto<PurchaseReturnDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PurchaseReturnDto?> GetByIdAsync(int id)
            => await ProjectToDto().FirstOrDefaultAsync(r => r.Id == id);

        public async Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnDto dto)
        {
            ValidateItems(dto.Items);

            var shopId = _currentUser.ShopId
                ?? throw new InvalidOperationException("No shop selected for the current session.");

            if (!await _db.Suppliers.AnyAsync(s => s.Id == dto.SupplierId))
                throw new InvalidOperationException($"Supplier {dto.SupplierId} not found.");

            // If linked to a purchase, use IPurchaseService (not a raw query) to
            // get the authoritative item list — same source of truth Purchases
            // itself uses — and validate the supplier matches and quantities
            // don't exceed what was actually purchased minus what's already returned.
            if (dto.PurchaseId.HasValue)
                await ValidateAgainstPurchaseAsync(dto.PurchaseId.Value, dto.SupplierId, dto.Items, excludingReturnId: null);

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var returnNo = await GenerateNextReturnNoAsync(shopId);
                var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.Discount, dto.Tax);

                var purchaseReturn = new PurchaseReturn
                {
                    ReturnNo = returnNo,
                    SupplierId = dto.SupplierId,
                    PurchaseId = dto.PurchaseId,
                    ReturnDate = dto.ReturnDate,
                    SubTotal = subTotal,
                    Discount = dto.Discount,
                    Tax = dto.Tax,
                    GrandTotal = grandTotal,
                    Remarks = dto.Remarks
                };

                foreach (var item in dto.Items)
                {
                    purchaseReturn.PurchaseReturnDetails.Add(new PurchaseReturnDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        PurchasePrice = item.PurchasePrice,
                        Discount = item.Discount,
                        Tax = item.Tax,
                        Total = item.Total
                    });
                }

                _db.PurchaseReturns.Add(purchaseReturn);
                await _db.SaveChangesAsync();

                AddStockTransactions(purchaseReturn, dto.Items);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return await GetByIdAsync(purchaseReturn.Id)
                    ?? throw new InvalidOperationException("Purchase return was created but could not be reloaded.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PurchaseReturnDto> UpdateAsync(int id, CreatePurchaseReturnDto dto)
        {
            var purchaseReturn = await _db.PurchaseReturns
                .Include(r => r.PurchaseReturnDetails)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Purchase return {id} not found.");

            ValidateItems(dto.Items);

            if (!await _db.Suppliers.AnyAsync(s => s.Id == dto.SupplierId))
                throw new InvalidOperationException($"Supplier {dto.SupplierId} not found.");

            if (dto.PurchaseId.HasValue)
                await ValidateAgainstPurchaseAsync(dto.PurchaseId.Value, dto.SupplierId, dto.Items, excludingReturnId: id);

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var oldStockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "PurchaseReturn" && s.ReferenceId == purchaseReturn.Id)
                    .ToListAsync();
                _db.StockTransactions.RemoveRange(oldStockRows);

                _db.PurchaseReturnDetails.RemoveRange(purchaseReturn.PurchaseReturnDetails);
                purchaseReturn.PurchaseReturnDetails.Clear();

                var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.Discount, dto.Tax);

                purchaseReturn.SupplierId = dto.SupplierId;
                purchaseReturn.PurchaseId = dto.PurchaseId;
                purchaseReturn.ReturnDate = dto.ReturnDate;
                purchaseReturn.SubTotal = subTotal;
                purchaseReturn.Discount = dto.Discount;
                purchaseReturn.Tax = dto.Tax;
                purchaseReturn.GrandTotal = grandTotal;
                purchaseReturn.Remarks = dto.Remarks;

                foreach (var item in dto.Items)
                {
                    purchaseReturn.PurchaseReturnDetails.Add(new PurchaseReturnDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        PurchasePrice = item.PurchasePrice,
                        Discount = item.Discount,
                        Tax = item.Tax,
                        Total = item.Total
                    });
                }

                await _db.SaveChangesAsync();

                AddStockTransactions(purchaseReturn, dto.Items);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return await GetByIdAsync(purchaseReturn.Id)
                    ?? throw new InvalidOperationException("Purchase return was updated but could not be reloaded.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var purchaseReturn = await _db.PurchaseReturns
                .Include(r => r.PurchaseReturnDetails)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Purchase return {id} not found.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var stockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "PurchaseReturn" && s.ReferenceId == id)
                    .ToListAsync();

                _db.StockTransactions.RemoveRange(stockRows);
                _db.PurchaseReturnDetails.RemoveRange(purchaseReturn.PurchaseReturnDetails);
                _db.PurchaseReturns.Remove(purchaseReturn);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Cross-checks the return against the real purchase via IPurchaseService
        // (not a raw duplicate query) — supplier must match, and quantity
        // returned (across ALL returns against this purchase, minus whatever
        // this specific return already contributed if editing) can never
        // exceed what was actually purchased for that product.
        private async Task ValidateAgainstPurchaseAsync(int purchaseId, int supplierId, List<CreatePurchaseReturnDetailDto> items, int? excludingReturnId)
        {
            var purchase = await _purchaseService.GetByIdAsync(purchaseId)
                ?? throw new InvalidOperationException($"Purchase {purchaseId} not found.");

            if (purchase.SupplierId != supplierId)
                throw new InvalidOperationException("The selected purchase does not belong to the selected supplier.");

            var alreadyReturnedQuery = _db.PurchaseReturnDetails
                .Where(d => d.PurchaseReturn.PurchaseId == purchaseId);

            if (excludingReturnId.HasValue)
                alreadyReturnedQuery = alreadyReturnedQuery.Where(d => d.PurchaseReturnId != excludingReturnId.Value);

            var alreadyReturnedByProduct = await alreadyReturnedQuery
                .GroupBy(d => d.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

            foreach (var item in items)
            {
                var originalLine = purchase.Items.FirstOrDefault(i => i.ProductId == item.ProductId)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} was not part of the original purchase.");

                var alreadyReturned = alreadyReturnedByProduct.GetValueOrDefault(item.ProductId, 0);
                var maxReturnable = originalLine.Quantity - alreadyReturned;

                if (item.Quantity > maxReturnable)
                    throw new InvalidOperationException(
                        $"Cannot return {item.Quantity} of '{originalLine.ProductName}' — only {maxReturnable} remaining returnable from this purchase.");
            }
        }

        private void AddStockTransactions(PurchaseReturn purchaseReturn, List<CreatePurchaseReturnDetailDto> items)
        {
            foreach (var item in items)
            {
                _db.StockTransactions.Add(new StockTransaction
                {
                    ProductId = item.ProductId,
                    TransactionType = StockTransactionType.PURCHASE_RETURN, // ASSUMPTION — confirm exact enum member name
                    QuantityIn = 0,
                    QuantityOut = item.Quantity,
                    ReferenceType = "PurchaseReturn",
                    ReferenceId = purchaseReturn.Id,
                    TransactionDate = purchaseReturn.ReturnDate,
                    Remarks = $"Purchase return {purchaseReturn.ReturnNo}"
                });
            }
        }

        private static (decimal subTotal, decimal grandTotal) CalculateTotals(List<CreatePurchaseReturnDetailDto> items, decimal headerDiscount, decimal headerTax)
        {
            var subTotal = items.Sum(i => i.Total);
            var grandTotal = Math.Max(0, subTotal - headerDiscount + headerTax);
            return (subTotal, grandTotal);
        }

        private static void ValidateItems(List<CreatePurchaseReturnDetailDto> items)
        {
            if (items is null || items.Count == 0)
                throw new InvalidOperationException("A purchase return must have at least one item.");

            foreach (var item in items)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException("Item quantity must be greater than zero.");
                if (item.PurchasePrice < 0)
                    throw new InvalidOperationException("Item purchase price cannot be negative.");
            }
        }

        private async Task<string> GenerateNextReturnNoAsync(int shopId)
        {
            var prefix = "RET";

            var returnNos = await _db.PurchaseReturns
                .IgnoreQueryFilters()
                .Where(r => r.ShopId == shopId)
                .Select(r => r.ReturnNo)
                .ToListAsync();

            var maxNumber = 0;
            var searchPrefix = prefix + "-";
            foreach (var returnNo in returnNos)
            {
                if (returnNo.StartsWith(searchPrefix) &&
                    int.TryParse(returnNo.AsSpan(searchPrefix.Length), out var num) && num > maxNumber)
                    maxNumber = num;
            }

            return $"{prefix}-{(maxNumber + 1):D5}";
        }
    }
}
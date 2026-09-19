using Microsoft.EntityFrameworkCore;
using POS.DTOs.Purchasing;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Inventory;
using POS.Entities.IServices;
using POS.Entities.Purchasing;

namespace POS.Entities.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public PurchaseService(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        private IQueryable<PurchaseDto> ProjectToDto() =>
            _db.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.PaymentMethod)
                .Include(p => p.PurchaseDetails).ThenInclude(d => d.Product)
                .Select(p => new PurchaseDto
                {
                    Id = p.Id,
                    InvoiceNo = p.InvoiceNo,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier.Name,
                    PurchaseDate = p.PurchaseDate,
                    SubTotal = p.SubTotal,
                    Discount = p.Discount,
                    Tax = p.Tax,
                    GrandTotal = p.GrandTotal,
                    PaidAmount = p.PaidAmount,
                    DueAmount = p.DueAmount,
                    PaymentMethodId = p.PaymentMethodId,
                    PaymentMethodName = p.PaymentMethod != null ? p.PaymentMethod.Name : null,
                    Remarks = p.Remarks,
                    Items = p.PurchaseDetails.Select(d => new PurchaseDetailDto
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

        public async Task<List<PurchaseDto>> GetAllAsync()
            => await ProjectToDto().OrderByDescending(p => p.PurchaseDate).ThenByDescending(p => p.Id).ToListAsync();

        public async Task<PurchaseDto?> GetByIdAsync(int id)
            => await ProjectToDto().FirstOrDefaultAsync(p => p.Id == id);

        public async Task<PurchaseDto> CreateAsync(CreatePurchaseDto dto)
        {
            ValidateItems(dto.Items);
            await ValidateSupplierAndPaymentMethodAsync(dto.SupplierId, dto.PaymentMethodId);

            var shopId = _currentUser.ShopId
                ?? throw new InvalidOperationException("No shop selected for the current session.");

            var invoiceNo = await GenerateNextInvoiceNoAsync(shopId);
            var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.Discount, dto.Tax);

            var purchase = new Purchase
            {
                InvoiceNo = invoiceNo,
                SupplierId = dto.SupplierId,
                PurchaseDate = dto.PurchaseDate,
                SubTotal = subTotal,
                Discount = dto.Discount,
                Tax = dto.Tax,
                GrandTotal = grandTotal,
                PaidAmount = dto.PaidAmount,
                DueAmount = grandTotal - dto.PaidAmount,
                PaymentMethodId = dto.PaymentMethodId,
                Remarks = dto.Remarks
            };

            foreach (var item in dto.Items)
            {
                purchase.PurchaseDetails.Add(new PurchaseDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PurchasePrice = item.PurchasePrice,
                    Discount = item.Discount,
                    Tax = item.Tax,
                    Total = item.Total
                });
            }

            _db.Purchases.Add(purchase);
            await _db.SaveChangesAsync(); // needed so purchase.Id and detail.ProductId are available below

            AddStockTransactions(purchase, dto.Items);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(purchase.Id)
                ?? throw new InvalidOperationException("Purchase was created but could not be reloaded.");
        }

        public async Task<PurchaseDto> UpdateAsync(int id, CreatePurchaseDto dto)
        {
            var purchase = await _db.Purchases
                .Include(p => p.PurchaseDetails)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Purchase {id} not found.");

            var hasReturns = await _db.PurchaseReturns.AnyAsync(r => r.PurchaseId == id);
            if (hasReturns)
                throw new InvalidOperationException("Cannot edit a purchase that already has returns recorded against it.");

            ValidateItems(dto.Items);
            await ValidateSupplierAndPaymentMethodAsync(dto.SupplierId, dto.PaymentMethodId);

            // Reverse the old stock impact before applying the new one —
            // simplest correct approach: delete old stock rows for this
            // purchase, then re-add fresh ones for the new item list.
            var oldStockRows = await _db.StockTransactions
                .Where(s => s.ReferenceType == "Purchase" && s.ReferenceId == purchase.Id)
                .ToListAsync();
            _db.StockTransactions.RemoveRange(oldStockRows);

            _db.PurchaseDetails.RemoveRange(purchase.PurchaseDetails);
            purchase.PurchaseDetails.Clear();

            var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.Discount, dto.Tax);

            purchase.SupplierId = dto.SupplierId;
            purchase.PurchaseDate = dto.PurchaseDate;
            purchase.SubTotal = subTotal;
            purchase.Discount = dto.Discount;
            purchase.Tax = dto.Tax;
            purchase.GrandTotal = grandTotal;
            purchase.PaidAmount = dto.PaidAmount;
            purchase.DueAmount = grandTotal - dto.PaidAmount;
            purchase.PaymentMethodId = dto.PaymentMethodId;
            purchase.Remarks = dto.Remarks;

            foreach (var item in dto.Items)
            {
                purchase.PurchaseDetails.Add(new PurchaseDetail
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

            AddStockTransactions(purchase, dto.Items);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(purchase.Id)
                ?? throw new InvalidOperationException("Purchase was updated but could not be reloaded.");
        }

        public async Task DeleteAsync(int id)
        {
            var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Purchase {id} not found.");

            var hasReturns = await _db.PurchaseReturns.AnyAsync(r => r.PurchaseId == id);
            var hasPayments = await _db.SupplierPayments.AnyAsync(sp => sp.PurchaseId == id);
            if (hasReturns || hasPayments)
                throw new InvalidOperationException("Cannot delete a purchase that has returns or payments recorded against it.");

            var stockRows = await _db.StockTransactions
                .Where(s => s.ReferenceType == "Purchase" && s.ReferenceId == id)
                .ToListAsync();
            _db.StockTransactions.RemoveRange(stockRows);

            _db.Purchases.Remove(purchase); // soft delete via SaveChanges override; details cascade via query filter, not physically removed
            await _db.SaveChangesAsync();
        }

        private void AddStockTransactions(Purchase purchase, List<CreatePurchaseDetailDto> items)
        {
            // purchase.PurchaseDetails is now populated with real IDs after
            // the first SaveChangesAsync, matched back to dto.Items by
            // position since both were built in the same order.
            var savedDetails = purchase.PurchaseDetails.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                _db.StockTransactions.Add(new StockTransaction
                {
                    ProductId = items[i].ProductId,
                    TransactionType = StockTransactionType.PURCHASE,
                    QuantityIn = items[i].Quantity,
                    QuantityOut = 0,
                    ReferenceType = "Purchase",
                    ReferenceId = purchase.Id,
                    TransactionDate = purchase.PurchaseDate,
                    Remarks = $"Purchase against invoice No.: {purchase.InvoiceNo}"
                });
            }
        }

        private static (decimal subTotal, decimal grandTotal) CalculateTotals(
            List<CreatePurchaseDetailDto> items, decimal headerDiscount, decimal headerTax)
        {
            var subTotal = items.Sum(i => i.Total);
            var grandTotal = subTotal - headerDiscount + headerTax;
            return (subTotal, grandTotal);
        }

        private static void ValidateItems(List<CreatePurchaseDetailDto> items)
        {
            if (items is null || items.Count == 0)
                throw new InvalidOperationException("A purchase must have at least one item.");

            foreach (var item in items)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException("Item quantity must be greater than zero.");
                if (item.PurchasePrice < 0)
                    throw new InvalidOperationException("Item purchase price cannot be negative.");
            }
        }

        private async Task ValidateSupplierAndPaymentMethodAsync(int supplierId, int? paymentMethodId)
        {
            if (!await _db.Suppliers.AnyAsync(s => s.Id == supplierId))
                throw new InvalidOperationException($"Supplier {supplierId} not found.");

            if (paymentMethodId.HasValue && !await _db.PaymentMethods.AnyAsync(m => m.Id == paymentMethodId.Value))
                throw new InvalidOperationException($"Payment method {paymentMethodId} not found.");
        }

        // Scans every purchase ever made at this shop (including soft-deleted)
        // so a deleted purchase's number is never reused.
        private async Task<string> GenerateNextInvoiceNoAsync(int shopId)
        {
            var shop = await _db.Shops.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == shopId)
                ?? throw new InvalidOperationException("Shop not found.");

            var prefix = string.IsNullOrWhiteSpace(shop.InvoicePrefix) ? "PUR" : shop.InvoicePrefix;

            var invoiceNos = await _db.Purchases
                .IgnoreQueryFilters()
                .Where(p => p.ShopId == shopId)
                .Select(p => p.InvoiceNo)
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
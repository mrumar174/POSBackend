using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using POS.DTOs.Common;
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
                    DiscountType = p.DiscountType,
                    DiscountValue = p.DiscountValue,
                    Discount = p.Discount,
                    Tax = p.Tax,
                    GrandTotal = p.GrandTotal,
                    // The cached field matching the sum of active SupplierPayments
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
                        DiscountType = d.DiscountType,
                        DiscountValue = d.DiscountValue,
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
            try
            {
                ValidateItems(dto.Items);
                await ValidateSupplierAndPaymentMethodAsync(dto.SupplierId, dto.PaymentMethodId);

                var shopId = _currentUser.ShopId
                    ?? throw new InvalidOperationException("No shop selected for the current session.");

                var invoiceNo = await GenerateNextInvoiceNoAsync(shopId);
                var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.DiscountType, dto.DiscountValue, dto.Tax, out _);
                var headerDiscount = ComputeDiscountAmount(dto.DiscountType, dto.DiscountValue, subTotal, dto.Items.Sum(i => i.Quantity));

                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    var purchase = new Purchase
                    {
                        InvoiceNo = invoiceNo,
                        SupplierId = dto.SupplierId,
                        PurchaseDate = dto.PurchaseDate,
                        SubTotal = subTotal,
                        DiscountType = dto.DiscountType,
                        DiscountValue = dto.DiscountValue,
                        Discount = headerDiscount,
                        Tax = dto.Tax,
                        GrandTotal = grandTotal,
                        PaidAmount = dto.PaidAmount,
                        DueAmount = grandTotal - dto.PaidAmount,
                        PaymentMethodId = dto.PaymentMethodId,
                        Remarks = dto.Remarks
                    };

                    foreach (var item in dto.Items)
                    {
                        if (item.DiscountType == DiscountType.Percentage && item.DiscountValue > 100)
                            throw new InvalidOperationException("Percentage discount cannot exceed 100.");

                        var baseAmount = item.Quantity * item.PurchasePrice;
                        var itemDiscount = ComputeDiscountAmount(item.DiscountType, item.DiscountValue, baseAmount, item.Quantity);

                        purchase.PurchaseDetails.Add(new PurchaseDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            PurchasePrice = item.PurchasePrice,
                            DiscountType = item.DiscountType,
                            DiscountValue = item.DiscountValue,
                            Discount = itemDiscount,
                            Tax = item.Tax,
                            Total = Math.Max(0, baseAmount - itemDiscount + item.Tax)
                        });
                    }

                    _db.Purchases.Add(purchase);
                    await _db.SaveChangesAsync(); // Save 1: Generates Purchase.Id required for related tables

                    // Auto-generate the SupplierPayment if cash was handed over
                    if (dto.PaidAmount > 0)
                    {
                        if (!dto.PaymentMethodId.HasValue)
                            throw new InvalidOperationException("Payment Method is required when an amount is paid.");

                        var paymentNo = await GenerateNextPaymentNoAsync(shopId);
                        var payment = new SupplierPayment
                        {
                            PaymentNo = paymentNo,
                            SupplierId = purchase.SupplierId,
                            PurchaseId = purchase.Id,
                            PaymentDate = purchase.PurchaseDate,
                            Amount = dto.PaidAmount,
                            PaymentMethodId = dto.PaymentMethodId.Value,
                            ReferenceNo = purchase.InvoiceNo,
                            Remarks = $"Initial payment for Invoice {purchase.InvoiceNo}"
                        };

                        _db.SupplierPayments.Add(payment);
                    }

                    // Append Inventory changes
                    AddStockTransactions(purchase, dto.Items);

                    await _db.SaveChangesAsync(); // Save 2: Commits Payments and Stock Transactions
                    await transaction.CommitAsync();

                    return await GetByIdAsync(purchase.Id)
                        ?? throw new InvalidOperationException("Purchase was created but could not be reloaded.");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception)
            {

                throw;
            }
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

            var (subTotal, grandTotal) = CalculateTotals(dto.Items, dto.DiscountType, dto.DiscountValue, dto.Tax, out _);

            if (dto.PaidAmount > grandTotal)
                throw new InvalidOperationException("The paid amount cannot exceed the new grand total of the invoice.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Reverse old stock impact
                var oldStockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "Purchase" && s.ReferenceId == purchase.Id)
                    .ToListAsync();
                _db.StockTransactions.RemoveRange(oldStockRows);

                // 2. Clear old items
                _db.PurchaseDetails.RemoveRange(purchase.PurchaseDetails);
                purchase.PurchaseDetails.Clear();

                // 3. Update Purchase Header
                var headerDiscount = ComputeDiscountAmount(dto.DiscountType, dto.DiscountValue, subTotal, dto.Items.Sum(i => i.Quantity));

                purchase.DiscountType = dto.DiscountType;
                purchase.DiscountValue = dto.DiscountValue;
                purchase.Discount = headerDiscount;
                purchase.Tax = dto.Tax;
                purchase.SubTotal = subTotal;
                purchase.GrandTotal = grandTotal;
                purchase.Remarks = dto.Remarks;
                purchase.PaymentMethodId = dto.PaymentMethodId;
                purchase.PaidAmount = dto.PaidAmount;
                purchase.DueAmount = grandTotal - dto.PaidAmount;

                // 4. Sync SupplierPayment Table (Update, Create, or Delete)
                var existingPayments = await _db.SupplierPayments
                    .Where(sp => sp.PurchaseId == purchase.Id)
                    .ToListAsync();

                if (existingPayments.Any())
                {
                    if (dto.PaidAmount == 0)
                    {
                        // If they changed it to 0, soft-delete the existing payment(s)
                        _db.SupplierPayments.RemoveRange(existingPayments);
                    }
                    else
                    {
                        // Update the existing payment amount to match the screen
                        var firstPayment = existingPayments.First();
                        firstPayment.Amount = dto.PaidAmount;

                        if (dto.PaymentMethodId.HasValue)
                            firstPayment.PaymentMethodId = dto.PaymentMethodId.Value;

                        // Clean up any extra fragments if there were multiple payments 
                        // so the total strictly matches the inline form
                        if (existingPayments.Count > 1)
                            _db.SupplierPayments.RemoveRange(existingPayments.Skip(1));
                    }
                }
                else if (dto.PaidAmount > 0)
                {
                    // No existing payment was found, but user entered a PaidAmount > 0
                    if (!dto.PaymentMethodId.HasValue)
                        throw new InvalidOperationException("Payment Method is required when recording a payment.");

                    var shopId = _currentUser.ShopId ?? purchase.ShopId;
                    var paymentNo = await GenerateNextPaymentNoAsync(shopId);

                    _db.SupplierPayments.Add(new SupplierPayment
                    {
                        PaymentNo = paymentNo,
                        SupplierId = purchase.SupplierId,
                        PurchaseId = purchase.Id,
                        PaymentDate = DateTime.Now,
                        Amount = dto.PaidAmount,
                        PaymentMethodId = dto.PaymentMethodId.Value,
                        ReferenceNo = purchase.InvoiceNo,
                        Remarks = $"Payment recorded during invoice update."
                    });
                }

                // 5. Append new detail rows
                foreach (var item in dto.Items)
                {
                    var baseAmount = item.Quantity * item.PurchasePrice;
                    var itemDiscount = ComputeDiscountAmount(item.DiscountType, item.DiscountValue, baseAmount, item.Quantity);

                    purchase.PurchaseDetails.Add(new PurchaseDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        PurchasePrice = item.PurchasePrice,
                        DiscountType = item.DiscountType,
                        DiscountValue = item.DiscountValue,
                        Discount = itemDiscount,
                        Tax = item.Tax,
                        Total = Math.Max(0, baseAmount - itemDiscount + item.Tax)
                    });
                }

                await _db.SaveChangesAsync();

                // 6. Re-apply stock
                AddStockTransactions(purchase, dto.Items);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetByIdAsync(purchase.Id)
                    ?? throw new InvalidOperationException("Purchase was updated but could not be reloaded.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var purchase = await _db.Purchases
                .Include(p => p.PurchaseDetails)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Purchase {id} not found.");

            // Returns still block deletion — reversing stock/financials against a
            // return that references this purchase is a much bigger operation than
            // a simple cascade and should be handled deliberately, not silently.
            var hasReturns = await _db.PurchaseReturns.AnyAsync(r => r.PurchaseId == id);
            if (hasReturns)
                throw new InvalidOperationException("Cannot delete a purchase that has returns recorded against it. Delete the return(s) first.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var stockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "Purchase" && s.ReferenceId == id)
                    .ToListAsync();

                var payments = await _db.SupplierPayments
                    .Where(sp => sp.PurchaseId == id)
                    .ToListAsync();

                _db.StockTransactions.RemoveRange(stockRows);
                _db.SupplierPayments.RemoveRange(payments); // cascade soft-delete
                _db.PurchaseDetails.RemoveRange(purchase.PurchaseDetails);
                _db.Purchases.Remove(purchase);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private void AddStockTransactions(Purchase purchase, List<CreatePurchaseDetailDto> items)
        {
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

        private static (decimal subTotal, decimal grandTotal) CalculateTotals(List<CreatePurchaseDetailDto> items, DiscountType headerDiscountType, decimal headerDiscountValue, decimal headerTax, out List<decimal> computedItemTotals)
        {
            computedItemTotals = new List<decimal>();
            decimal subTotal = 0;

            foreach (var item in items)
            {
                var baseAmount = item.Quantity * item.PurchasePrice;
                var discount = ComputeDiscountAmount(item.DiscountType, item.DiscountValue, baseAmount, item.Quantity);
                var total = Math.Max(0, baseAmount - discount + item.Tax);
                computedItemTotals.Add(total);
                subTotal += total;
            }

            var headerDiscount = ComputeDiscountAmount(headerDiscountType, headerDiscountValue, subTotal, items.Sum(i => i.Quantity));
            var grandTotal = Math.Max(0, subTotal - headerDiscount + headerTax);
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

        private async Task<string> GenerateNextPaymentNoAsync(int shopId)
        {
            var prefix = "PAY"; // Alternatively configure a 'PaymentPrefix' in Shop settings

            var paymentNos = await _db.SupplierPayments
                .IgnoreQueryFilters()
                .Where(sp => sp.ShopId == shopId)
                .Select(sp => sp.PaymentNo)
                .ToListAsync();

            var maxNumber = 0;
            var searchPrefix = prefix + "-";
            foreach (var paymentNo in paymentNos)
            {
                if (paymentNo.StartsWith(searchPrefix) &&
                    int.TryParse(paymentNo.AsSpan(searchPrefix.Length), out var num) && num > maxNumber)
                    maxNumber = num;
            }

            return $"{prefix}-{(maxNumber + 1):D5}";
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
        public async Task<List<PurchaseDto>> GetBySupplierAsync(int supplierId)
            => await ProjectToDto().Where(p => p.SupplierId == supplierId).OrderByDescending(p => p.PurchaseDate).ToListAsync();
    }
}
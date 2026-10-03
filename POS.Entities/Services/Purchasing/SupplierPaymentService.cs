using Microsoft.EntityFrameworkCore;
using POS.DTOs.Common;
using POS.DTOs.Purchasing;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.Purchasing;

namespace POS.Entities.Services
{
    public class SupplierPaymentService : ISupplierPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public SupplierPaymentService(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        private IQueryable<SupplierPaymentDto> ProjectToDto() =>
            _db.SupplierPayments
                .Include(sp => sp.Supplier)
                .Include(sp => sp.PaymentMethod)
                .Include(sp => sp.Purchase)
                .Select(sp => new SupplierPaymentDto
                {
                    Id = sp.Id,
                    PaymentNo = sp.PaymentNo,
                    SupplierId = sp.SupplierId,
                    SupplierName = sp.Supplier.Name,
                    PurchaseId = sp.PurchaseId,
                    PaymentDate = sp.PaymentDate,
                    Amount = sp.Amount,
                    PaymentMethodId = sp.PaymentMethodId,
                    PaymentMethodName = sp.PaymentMethod.Name,
                    ReferenceNo = sp.ReferenceNo,
                    Remarks = sp.Remarks
                });

        public async Task<PagedResultDto<SupplierPaymentDto>> SearchAsync(SupplierPaymentQueryDto query)
        {
            var q = ProjectToDto();

            if (query.SupplierId.HasValue)
                q = q.Where(p => p.SupplierId == query.SupplierId.Value);

            if (query.PurchaseId.HasValue)
                q = q.Where(p => p.PurchaseId == query.PurchaseId.Value);

            if (!string.IsNullOrWhiteSpace(query.PaymentNo))
                q = q.Where(p => p.PaymentNo.ToLower().Contains(query.PaymentNo.ToLower()));

            if (query.FromDate.HasValue)
                q = q.Where(p => p.PaymentDate >= query.FromDate.Value.Date);

            if (query.ToDate.HasValue)
                q = q.Where(p => p.PaymentDate < query.ToDate.Value.Date.AddDays(1));

            q = q.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id);

            var totalCount = await q.CountAsync();
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDto<SupplierPaymentDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<SupplierPaymentDto?> GetByIdAsync(int id)
            => await ProjectToDto().FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<UnpaidPurchaseDto>> GetUnpaidPurchasesAsync(int? supplierId, int? excludePaymentId)
        {
            var q = _db.Purchases.Include(p => p.Supplier).Where(p => p.DueAmount > 0);

            if (supplierId.HasValue)
                q = q.Where(p => p.SupplierId == supplierId.Value);

            var results = await q
                .OrderByDescending(p => p.PurchaseDate)
                .Select(p => new UnpaidPurchaseDto
                {
                    Id = p.Id,
                    InvoiceNo = p.InvoiceNo,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier.Name,
                    PurchaseDate = p.PurchaseDate,
                    GrandTotal = p.GrandTotal,
                    PaidAmount = p.PaidAmount,
                    DueAmount = p.DueAmount
                })
                .ToListAsync();

            // When editing an existing payment, the purchase it was originally
            // recorded against may now show DueAmount == 0 (fully paid by this
            // very payment) and would otherwise be missing from the dropdown —
            // add it back in explicitly so the edit form can still show it selected.
            if (excludePaymentId.HasValue)
            {
                var existingPayment = await _db.SupplierPayments
                    .Include(sp => sp.Purchase).ThenInclude(p => p.Supplier)
                    .FirstOrDefaultAsync(sp => sp.Id == excludePaymentId.Value);

                if (existingPayment?.Purchase != null && !results.Any(r => r.Id == existingPayment.Purchase.Id))
                {
                    results.Insert(0, new UnpaidPurchaseDto
                    {
                        Id = existingPayment.Purchase.Id,
                        InvoiceNo = existingPayment.Purchase.InvoiceNo,
                        SupplierId = existingPayment.Purchase.SupplierId,
                        SupplierName = existingPayment.Purchase.Supplier.Name,
                        PurchaseDate = existingPayment.Purchase.PurchaseDate,
                        GrandTotal = existingPayment.Purchase.GrandTotal,
                        PaidAmount = existingPayment.Purchase.PaidAmount,
                        DueAmount = existingPayment.Purchase.DueAmount
                    });
                }
            }

            return results;
        }

        public async Task<SupplierPaymentDto> CreateAsync(CreateSupplierPaymentDto dto)
        {
            await ValidateAsync(dto, excludingPaymentId: null);

            var shopId = _currentUser.ShopId
                ?? throw new InvalidOperationException("No shop selected for the current session.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var paymentNo = await GenerateNextPaymentNoAsync(shopId);

                var payment = new SupplierPayment
                {
                    PaymentNo = paymentNo,
                    SupplierId = dto.SupplierId,
                    PurchaseId = dto.PurchaseId,
                    PaymentDate = dto.PaymentDate,
                    Amount = dto.Amount,
                    PaymentMethodId = dto.PaymentMethodId,
                    ReferenceNo = dto.ReferenceNo,
                    Remarks = dto.Remarks
                };

                _db.SupplierPayments.Add(payment);

                if (dto.PurchaseId.HasValue)
                    await ApplyPaymentToPurchaseAsync(dto.PurchaseId.Value, dto.Amount);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetByIdAsync(payment.Id)
                    ?? throw new InvalidOperationException("Payment was created but could not be reloaded.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SupplierPaymentDto> UpdateAsync(int id, CreateSupplierPaymentDto dto)
        {
            var payment = await _db.SupplierPayments.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Payment {id} not found.");

            await ValidateAsync(dto, excludingPaymentId: id);

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Reverse the old payment's effect on its old purchase (if any)
                if (payment.PurchaseId.HasValue)
                    await ApplyPaymentToPurchaseAsync(payment.PurchaseId.Value, -payment.Amount);

                payment.SupplierId = dto.SupplierId;
                payment.PurchaseId = dto.PurchaseId;
                payment.PaymentDate = dto.PaymentDate;
                payment.Amount = dto.Amount;
                payment.PaymentMethodId = dto.PaymentMethodId;
                payment.ReferenceNo = dto.ReferenceNo;
                payment.Remarks = dto.Remarks;

                // Apply the new payment's effect on its (possibly different) purchase
                if (dto.PurchaseId.HasValue)
                    await ApplyPaymentToPurchaseAsync(dto.PurchaseId.Value, dto.Amount);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetByIdAsync(payment.Id)
                    ?? throw new InvalidOperationException("Payment was updated but could not be reloaded.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var payment = await _db.SupplierPayments.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Payment {id} not found.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                if (payment.PurchaseId.HasValue)
                    await ApplyPaymentToPurchaseAsync(payment.PurchaseId.Value, -payment.Amount);

                _db.SupplierPayments.Remove(payment); // soft delete via SaveChanges override
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // delta: positive when adding/increasing a payment, negative when
        // reversing/removing one. Keeps Purchase.PaidAmount/DueAmount in sync
        // with the SupplierPayments actually recorded against it.
        private async Task ApplyPaymentToPurchaseAsync(int purchaseId, decimal delta)
        {
            var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.Id == purchaseId)
                ?? throw new InvalidOperationException($"Purchase {purchaseId} not found.");

            purchase.PaidAmount += delta;
            purchase.DueAmount = purchase.GrandTotal - purchase.PaidAmount;

            if (purchase.PaidAmount < 0 || purchase.DueAmount < 0)
                throw new InvalidOperationException("This payment would make the purchase's paid amount invalid. Check the amount entered.");
        }

        private async Task ValidateAsync(CreateSupplierPaymentDto dto, int? excludingPaymentId)
        {
            if (!await _db.Suppliers.AnyAsync(s => s.Id == dto.SupplierId))
                throw new InvalidOperationException($"Supplier {dto.SupplierId} not found.");

            if (!await _db.PaymentMethods.AnyAsync(m => m.Id == dto.PaymentMethodId))
                throw new InvalidOperationException($"Payment method {dto.PaymentMethodId} not found.");

            if (dto.Amount <= 0)
                throw new InvalidOperationException("Payment amount must be greater than zero.");

            if (dto.PurchaseId.HasValue)
            {
                var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.Id == dto.PurchaseId.Value)
                    ?? throw new InvalidOperationException($"Purchase {dto.PurchaseId} not found.");

                if (purchase.SupplierId != dto.SupplierId)
                    throw new InvalidOperationException("The selected purchase does not belong to the selected supplier.");

                // Allow up to the purchase's current due PLUS whatever this
                // specific payment (if editing) already contributed, since
                // that amount is about to be reversed and re-applied.
                var existingContribution = excludingPaymentId.HasValue
                    ? (await _db.SupplierPayments.FirstOrDefaultAsync(p => p.Id == excludingPaymentId.Value))?.Amount ?? 0
                    : 0;

                var allowedMax = purchase.DueAmount + existingContribution;
                if (dto.Amount > allowedMax)
                    throw new InvalidOperationException($"Payment amount cannot exceed the purchase's due amount ({allowedMax:0.00}).");
            }
        }

        private async Task<string> GenerateNextPaymentNoAsync(int shopId)
        {
            var prefix = "PAY";

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
    }
}
using Microsoft.EntityFrameworkCore;
using POS.DTOs.Finance;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Finance;
using POS.Entities.IServices.Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.Services.Finance
{
    public class DailyCashClosingService : IDailyCashClosingService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DailyCashClosingService(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // ---------- projections ----------
        private static readonly Expression<Func<DailyCashClosing, DailyCashClosingDto>> ToDtoExpr = c => new DailyCashClosingDto
        {
            Id = c.Id,
            ClosingDate = c.ClosingDate,
            OpeningBalance = c.OpeningBalance,
            TotalSales = c.TotalSales,
            OtherCashIn = c.TotalCashIn - c.TotalSales,
            TotalCashIn = c.TotalCashIn,
            TotalExpenses = c.TotalExpenses,
            OtherCashOut = c.TotalCashOut - c.TotalExpenses,
            TotalCashOut = c.TotalCashOut,
            ClosingBalance = c.ClosingBalance,
            Remarks = c.Remarks
        };
        private static readonly Func<DailyCashClosing, DailyCashClosingDto> ToDto = ToDtoExpr.Compile();

        private sealed record DayTotals(decimal Sales, decimal Expenses, decimal OtherIn, decimal OtherOut)
        {
            public decimal CashIn => Sales + OtherIn;
            public decimal CashOut => Expenses + OtherOut;
        }

        private sealed record PreviousClosing(DateTime ClosingDate, decimal ClosingBalance);

        // ---------- queries ----------
        public async Task<List<DailyCashClosingDto>> GetAllAsync(DateTime? fromDate, DateTime? toDate)
        {
            var shopId = RequireShopId();
            var q = _db.DailyCashClosings.AsNoTracking().Where(c => c.ShopId == shopId);

            if (fromDate.HasValue) { var from = ToDay(fromDate.Value); q = q.Where(c => c.ClosingDate >= from); }
            if (toDate.HasValue) { var to = ToDay(toDate.Value); q = q.Where(c => c.ClosingDate <= to); }

            return await q.OrderByDescending(c => c.ClosingDate).Select(ToDtoExpr).ToListAsync();
        }

        public async Task<DailyCashClosingDto?> GetByIdAsync(int id)
        {
            var shopId = RequireShopId();
            return await _db.DailyCashClosings.AsNoTracking()
                .Where(c => c.Id == id && c.ShopId == shopId)
                .Select(ToDtoExpr)
                .FirstOrDefaultAsync();
        }

        // ---------- preview ----------
        public async Task<DailyCashClosingPreviewDto> CalculateAsync(DateTime date)
        {
            var shopId = RequireShopId();
            var day = ValidateDate(date);

            // Already closed: show what was SAVED, not a fresh calculation that may differ.
            var saved = await _db.DailyCashClosings.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ShopId == shopId && c.ClosingDate == day);
            if (saved is not null)
            {
                var savedTotals = new DayTotals(
                    saved.TotalSales, saved.TotalExpenses,
                    saved.TotalCashIn - saved.TotalSales, saved.TotalCashOut - saved.TotalExpenses);
                return BuildPreview(day, null, saved.OpeningBalance, savedTotals, "This date is already closed.", alreadyClosed: true);
            }

            var previous = await GetPreviousClosingAsync(shopId, day);
            var totals = await SumDayAsync(shopId, day);
            var reason = await GetBlockReasonAsync(shopId, day);

            return BuildPreview(day, previous?.ClosingDate, previous?.ClosingBalance ?? 0m, totals, reason, alreadyClosed: false);
        }

        // ---------- save ----------
        public async Task<DailyCashClosingDto> CreateAsync(CreateDailyCashClosingDto dto)
        {
            var shopId = RequireShopId();
            var day = ValidateDate(dto.ClosingDate);

            if (dto.Remarks is { Length: > 500 })
                throw new InvalidOperationException("Remarks cannot exceed 500 characters.");

            var reason = await GetBlockReasonAsync(shopId, day);
            if (reason is not null) throw new InvalidOperationException(reason);

            // Never trust client numbers: recompute from source data.
            var previous = await GetPreviousClosingAsync(shopId, day);
            var opening = previous?.ClosingBalance ?? 0m;
            var totals = await SumDayAsync(shopId, day);
            var closing = opening + totals.CashIn - totals.CashOut;

            if (dto.ExpectedClosingBalance.HasValue &&
                decimal.Round(dto.ExpectedClosingBalance.Value, 2) != decimal.Round(closing, 2))
                throw new InvalidOperationException(
                    "The figures changed since you reviewed them (a sale or expense was recorded). Please review the updated figures and confirm again.");

            var entity = new DailyCashClosing
            {
                ClosingDate = day,
                OpeningBalance = opening,
                TotalSales = totals.Sales,
                TotalExpenses = totals.Expenses,
                TotalCashIn = totals.CashIn,
                TotalCashOut = totals.CashOut,
                ClosingBalance = closing,
                Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim()
            };

            _db.DailyCashClosings.Add(entity);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Unique index (ShopId, ClosingDate) caught a concurrent double-submit.
                _db.Entry(entity).State = EntityState.Detached;
                if (await _db.DailyCashClosings.AnyAsync(c => c.ShopId == shopId && c.ClosingDate == day))
                    throw new InvalidOperationException("This date is already closed.");
                throw;
            }

            return ToDto(entity);
        }

        // ---------- reopen ----------
        public async Task DeleteAsync(int id)
        {
            var shopId = RequireShopId();
            var entity = await _db.DailyCashClosings.FirstOrDefaultAsync(c => c.Id == id && c.ShopId == shopId)
                ?? throw new KeyNotFoundException($"Closing {id} not found.");

            if (await _db.DailyCashClosings.AnyAsync(c => c.ShopId == shopId && c.ClosingDate > entity.ClosingDate))
                throw new InvalidOperationException(
                    "Only the most recent closing can be reopened. Reopen the later closing(s) first.");

            _db.DailyCashClosings.Remove(entity);
            await _db.SaveChangesAsync();
        }

        // ---------- helpers ----------
        private int RequireShopId() => _currentUser.ShopId ?? throw new InvalidOperationException("No shop selected.");

        // The browser sends UTC midnight of the picked date; this recovers the calendar date whatever the server's time zone.
        private static DateTime ToDay(DateTime d) => d.Kind == DateTimeKind.Unspecified ? d.Date : d.ToUniversalTime().Date;

        private static DateTime ValidateDate(DateTime date)
        {
            if (date == default) throw new InvalidOperationException("Date is required.");
            return ToDay(date);
        }

        // No client's "today" is ever beyond UTC date + 1, so this never blocks a legitimate "today".
        private static DateTime MaxClosableDate => DateTime.UtcNow.Date.AddDays(1);

        private async Task<PreviousClosing?> GetPreviousClosingAsync(int shopId, DateTime day)
            => await _db.DailyCashClosings.AsNoTracking()
                .Where(c => c.ShopId == shopId && c.ClosingDate < day)
                .OrderByDescending(c => c.ClosingDate)
                .Select(c => new PreviousClosing(c.ClosingDate, c.ClosingBalance))
                .FirstOrDefaultAsync();

        // Single source of truth for "can this date be closed?", used by preview AND save.
        private async Task<string?> GetBlockReasonAsync(int shopId, DateTime day)
        {
            if (day > MaxClosableDate)
                return "A future date can't be closed.";

            if (await _db.DailyCashClosings.AnyAsync(c => c.ShopId == shopId && c.ClosingDate == day))
                return "This date is already closed.";

            var later = await _db.DailyCashClosings
                .Where(c => c.ShopId == shopId && c.ClosingDate > day)
                .OrderByDescending(c => c.ClosingDate)
                .Select(c => (DateTime?)c.ClosingDate)
                .FirstOrDefaultAsync();

            return later is null
                ? null
                : $"A closing already exists for {later.Value:dd MMM yyyy}. Days must be closed in order: reopen the later closing(s) first.";
        }

        private async Task<DayTotals> SumDayAsync(int shopId, DateTime day)
        {
            var next = day.AddDays(1);

            // Per your spec: GrandTotal. (Cash actually collected would be PaidAmount - ChangeAmount.)
            var sales = await _db.Sales.AsNoTracking()
                .Where(s => s.ShopId == shopId && s.SaleDate >= day && s.SaleDate < next)
                .SumAsync(s => (decimal?)s.GrandTotal) ?? 0m;

            var expenses = await _db.Expenses.AsNoTracking()
                .Where(e => e.ShopId == shopId && e.ExpenseDate >= day && e.ExpenseDate < next)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var cash = await _db.CashTransactions.AsNoTracking()
                .Where(c => c.ShopId == shopId && c.TransactionDate >= day && c.TransactionDate < next)
                .GroupBy(c => c.TransactionType)
                .Select(g => new
                {
                    Type = g.Key,
                    AmountIn = g.Sum(x => x.AmountIn),
                    AmountOut = g.Sum(x => x.AmountOut)
                })
                .ToListAsync();

            var otherIn = cash
                .Where(x => CashTransactionRules.IsInflow(x.Type))
                .Sum(x => x.AmountIn);

            var otherOut = cash
                .Where(x => CashTransactionRules.IsOutflow(x.Type))
                .Sum(x => x.AmountOut);

            return new DayTotals(sales, expenses, otherIn, otherOut);
        }

        private static DailyCashClosingPreviewDto BuildPreview(
            DateTime day, DateTime? previousDate, decimal opening, DayTotals t, string? blockedReason, bool alreadyClosed) => new()
            {
                ClosingDate = day,
                PreviousClosingDate = previousDate,
                OpeningBalance = opening,
                TotalSales = t.Sales,
                OtherCashIn = t.OtherIn,
                TotalCashIn = t.CashIn,
                TotalExpenses = t.Expenses,
                OtherCashOut = t.OtherOut,
                TotalCashOut = t.CashOut,
                ClosingBalance = opening + t.CashIn - t.CashOut,
                AlreadyClosed = alreadyClosed,
                CanClose = blockedReason is null,
                BlockedReason = blockedReason
            };
    }
}

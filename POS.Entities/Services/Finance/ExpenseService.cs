using Microsoft.EntityFrameworkCore;
using POS.DTOs.Finance;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Finance;
using POS.Entities.IServices;
using POS.Entities.IServices.Finance;

namespace POS.Entities.Services.Finance
{
    public class ExpenseService : IExpenseService
    {
        private const string NumberPrefix = "EXP";
        private const int MaxPageSize = 100;

        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public ExpenseService(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        private static IQueryable<ExpenseDto> Project(IQueryable<Expense> q) =>
            q.Select(e => new ExpenseDto
            {
                Id = e.Id,
                ExpenseNo = e.ExpenseNo,
                ExpenseCategoryId = e.ExpenseCategoryId,
                ExpenseCategoryName = e.ExpenseCategory.Name,
                ExpenseDate = e.ExpenseDate,
                Amount = e.Amount,
                PaymentMethodId = e.PaymentMethodId,
                PaymentMethodName = e.PaymentMethod.Name,
                Description = e.Description
            });

        public async Task<ExpensePageDto> GetPagedAsync(ExpenseQueryDto q)
        {
            var page = Math.Max(1, q.Page);
            var pageSize = Math.Clamp(q.PageSize, 1, MaxPageSize);

            var query = _db.Expenses.AsNoTracking().AsQueryable();

            if (q.ExpenseCategoryId.HasValue)
                query = query.Where(e => e.ExpenseCategoryId == q.ExpenseCategoryId.Value);

            if (q.FromDate.HasValue)
            {
                var from = q.FromDate.Value.Date;
                query = query.Where(e => e.ExpenseDate >= from);
            }

            if (q.ToDate.HasValue)
            {
                var toExclusive = q.ToDate.Value.Date.AddDays(1);
                query = query.Where(e => e.ExpenseDate < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim().ToLower();
                query = query.Where(e =>
                    e.ExpenseNo.ToLower().Contains(s) ||
                    (e.Description != null && e.Description.ToLower().Contains(s)));
            }

            var totalCount = await query.CountAsync();
            var totalAmount = await query.SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var items = await Project(
                    query.OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.Id)
                         .Skip((page - 1) * pageSize).Take(pageSize))
                .ToListAsync();

            return new ExpensePageDto
            {
                Items = items,
                TotalCount = totalCount,
                TotalAmount = totalAmount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ExpenseDto?> GetByIdAsync(int id)
            => await Project(_db.Expenses.AsNoTracking().Where(e => e.Id == id)).FirstOrDefaultAsync();

        public async Task<ExpenseDto> CreateAsync(CreateExpenseDto dto)
        {
            await ValidateAsync(dto);

            var shopId = _currentUser.ShopId ?? throw new InvalidOperationException("No shop selected.");

            var expense = new Expense
            {
                ExpenseNo = await GenerateNextExpenseNoAsync(shopId),
                ExpenseCategoryId = dto.ExpenseCategoryId,
                ExpenseDate = dto.ExpenseDate,
                Amount = dto.Amount,
                PaymentMethodId = dto.PaymentMethodId,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();

            // TODO (cash ledger): write a CashTransaction (type Expense, AmountOut = expense.Amount,
            // ReferenceId = expense.Id, ReferenceNo = expense.ExpenseNo) inside this same unit of work
            // once your cash service is ready. Mirror it in Update/Delete.

            return await GetByIdAsync(expense.Id) ?? throw new InvalidOperationException("Expense created but failed to reload.");
        }

        public async Task<ExpenseDto> UpdateAsync(int id, CreateExpenseDto dto)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new KeyNotFoundException($"Expense {id} not found.");

            await ValidateAsync(dto);

            // ExpenseNo is immutable once issued.
            expense.ExpenseCategoryId = dto.ExpenseCategoryId;
            expense.ExpenseDate = dto.ExpenseDate;
            expense.Amount = dto.Amount;
            expense.PaymentMethodId = dto.PaymentMethodId;
            expense.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            await _db.SaveChangesAsync();
            return await GetByIdAsync(id) ?? throw new InvalidOperationException("Expense updated but failed to reload.");
        }

        public async Task DeleteAsync(int id)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new KeyNotFoundException($"Expense {id} not found.");

            _db.Expenses.Remove(expense);
            await _db.SaveChangesAsync();
        }

        private async Task ValidateAsync(CreateExpenseDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("Amount must be greater than zero.");
            if (dto.ExpenseDate == default)
                throw new InvalidOperationException("Expense date is required.");
            if (dto.Description is { Length: > 500 })
                throw new InvalidOperationException("Description cannot exceed 500 characters.");

            if (!await _db.ExpenseCategories.AnyAsync(c => c.Id == dto.ExpenseCategoryId))
                throw new InvalidOperationException("Selected expense category does not exist.");
            if (!await _db.PaymentMethods.AnyAsync(p => p.Id == dto.PaymentMethodId))
                throw new InvalidOperationException("Selected payment method does not exist.");
        }

        private async Task<string> GenerateNextExpenseNoAsync(int shopId)
        {
            var searchPrefix = NumberPrefix + "-";

            var numbers = await _db.Expenses
                .IgnoreQueryFilters()
                .Where(e => e.ShopId == shopId && e.ExpenseNo.StartsWith(searchPrefix))
                .Select(e => e.ExpenseNo)
                .ToListAsync();

            var max = 0;
            foreach (var no in numbers)
                if (int.TryParse(no.AsSpan(searchPrefix.Length), out var n) && n > max)
                    max = n;

            return $"{searchPrefix}{(max + 1):D5}";
        }
    }
}
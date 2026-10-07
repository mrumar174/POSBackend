using POS.DTOs.Common;

namespace POS.DTOs.Finance
{
    // ---------- ExpenseCategory ----------
    public class ExpenseCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class CreateExpenseCategoryDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class UpdateExpenseCategoryDto : CreateExpenseCategoryDto
    {
        public int Id { get; set; }
    }

    // ---------- Expense ----------
    public class ExpenseDto
    {
        public int Id { get; set; }
        public string ExpenseNo { get; set; } = default!;
        public int ExpenseCategoryId { get; set; }
        public string? ExpenseCategoryName { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public int PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public string? Description { get; set; }
    }

    // ExpenseNo generated server-side per Shop.
    public class CreateExpenseDto
    {
        public int ExpenseCategoryId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public int PaymentMethodId { get; set; }
        public string? Description { get; set; }
    }

    // ---------- CashTransaction (read-only via API — written internally
    // whenever a Sale/Expense/SupplierPayment/etc. moves cash) ----------
    public class CashTransactionDto
    {
        public int Id { get; set; }
        public string TransactionNo { get; set; } = default!;
        public DateTime TransactionDate { get; set; }
        public CashTransactionType TransactionType { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceNo { get; set; }
        public int PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }
        public string? Remarks { get; set; }
    }

    // ---------- DailyCashClosing ----------
    public class DailyCashClosingDto
    {
        public int Id { get; set; }
        public DateTime ClosingDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal OtherCashIn { get; set; }     // TotalCashIn - TotalSales
        public decimal TotalCashIn { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal OtherCashOut { get; set; }    // TotalCashOut - TotalExpenses
        public decimal TotalCashOut { get; set; }
        public decimal ClosingBalance { get; set; }
        public string? Remarks { get; set; }
    }

    // OpeningBalance/TotalCashIn/TotalCashOut/ExpectedCash are computed
    // server-side from CashTransactions for the day — the till operator only
    // enters what they actually counted.
    public class CreateDailyCashClosingDto
    {
        public DateTime ClosingDate { get; set; }
        public decimal? ExpectedClosingBalance { get; set; }
        public string? Remarks { get; set; }
    }
    public class ExpenseQueryDto
    {
        public int? ExpenseCategoryId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
    public class ExpensePageDto
    {
        public List<ExpenseDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }   // sum over the whole filtered set, not just this page
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
    public class DailyCashClosingPreviewDto
    {
        public DateTime ClosingDate { get; set; }
        public DateTime? PreviousClosingDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal OtherCashIn { get; set; }
        public decimal TotalCashIn { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal OtherCashOut { get; set; }
        public decimal TotalCashOut { get; set; }
        public decimal ClosingBalance { get; set; }
        public bool AlreadyClosed { get; set; }
        public bool CanClose { get; set; }
        public string? BlockedReason { get; set; }
    }
}

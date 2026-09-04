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
        public DateOnly ClosingDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalCashIn { get; set; }
        public decimal TotalCashOut { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal Difference { get; set; }
        public string? Remarks { get; set; }
    }

    // OpeningBalance/TotalCashIn/TotalCashOut/ExpectedCash are computed
    // server-side from CashTransactions for the day — the till operator only
    // enters what they actually counted.
    public class CreateDailyCashClosingDto
    {
        public DateOnly ClosingDate { get; set; }
        public decimal ActualCash { get; set; }
        public string? Remarks { get; set; }
    }
}

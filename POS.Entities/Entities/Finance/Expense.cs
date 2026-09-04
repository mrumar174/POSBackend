using POS.Entities.Common;
using POS.Entities.Settings;

namespace POS.Entities.Finance
{
    /// <summary>Shop-scoped: rent/electricity/salary is usually paid per-branch. ExpenseNo uniqueness becomes UQ(ShopId, ExpenseNo).</summary>
    public class Expense : ShopAuditableEntity
    {
        public string ExpenseNo { get; set; } = default!;

        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory ExpenseCategory { get; set; } = default!;

        public DateTime ExpenseDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }

        public int PaymentMethodId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = default!;

        public string? Description { get; set; }
    }
}

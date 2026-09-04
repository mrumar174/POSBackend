using POS.Entities.Common;

namespace POS.Entities.Finance
{
    /// <summary>Tenant-scoped: shared expense categories across all branches (Rent, Electricity, Salary, ...).</summary>
    public class ExpenseCategory : TenantAuditableEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}

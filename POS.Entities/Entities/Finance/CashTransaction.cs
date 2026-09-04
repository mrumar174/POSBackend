using POS.Entities.Common;
using POS.Entities.Settings;

namespace POS.Entities.Finance
{
    /// <summary>Shop-scoped: each branch has its own cash drawer/till. TransactionNo uniqueness becomes UQ(ShopId, TransactionNo).</summary>
    public class CashTransaction : ShopAuditableEntity
    {
        public string TransactionNo { get; set; } = default!;
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public CashTransactionType TransactionType { get; set; }

        public int? ReferenceId { get; set; }
        public string? ReferenceNo { get; set; }

        public int PaymentMethodId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = default!;

        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }

        public string? Remarks { get; set; }
    }
}

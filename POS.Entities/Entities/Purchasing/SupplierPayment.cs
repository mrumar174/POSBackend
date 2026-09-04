using POS.Entities.Common;
using POS.Entities.Settings;

namespace POS.Entities.Purchasing
{
    /// <summary>Shop-scoped: cash/bank paid out of a specific branch's till/account.</summary>
    public class SupplierPayment : ShopAuditableEntity
    {
        public string PaymentNo { get; set; } = default!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = default!;

        public int? PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }

        public int PaymentMethodId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = default!;

        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
    }
}

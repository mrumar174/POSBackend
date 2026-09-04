using POS.Entities.Common;
using POS.Entities.Settings;

namespace POS.Entities.Purchasing
{
    /// <summary>
    /// Shop-scoped: each branch places and receives its own purchase invoices.
    /// InvoiceNo uniqueness moves from global to UQ(TenantId, ShopId, InvoiceNo)
    /// (or UQ(ShopId, InvoiceNo)) so branches can independently number their
    /// own PUR-00001 without colliding — use Shop.InvoicePrefix to keep them
    /// human-distinguishable on reports.
    /// </summary>
    public class Purchase : ShopAuditableEntity
    {
        public string InvoiceNo { get; set; } = default!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = default!;

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }

        public int? PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        public string? Remarks { get; set; }

        public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();
        public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();
        public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
    }
}

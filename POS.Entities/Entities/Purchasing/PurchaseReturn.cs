using POS.Entities.Common;

namespace POS.Entities.Purchasing
{
    /// <summary>Shop-scoped: returns to supplier are recorded from the branch that received/returns the stock.</summary>
    public class PurchaseReturn : ShopAuditableEntity
    {
        public string ReturnNo { get; set; } = default!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = default!;

        public int? PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }

        public DateTime ReturnDate { get; set; } = DateTime.Now;

        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }

        public string? Remarks { get; set; }

        public ICollection<PurchaseReturnDetail> PurchaseReturnDetails { get; set; } = new List<PurchaseReturnDetail>();
    }
}

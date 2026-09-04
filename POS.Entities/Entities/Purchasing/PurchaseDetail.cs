using POS.Entities.Catalog;
using POS.Entities.Common;

namespace POS.Entities.Purchasing
{
    /// <summary>Shop-scoped (inherits from Purchase). Line items of a purchase invoice.</summary>
    public class PurchaseDetail : ShopAuditableEntity
    {
        public int PurchaseId { get; set; }
        public Purchase Purchase { get; set; } = default!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }
}

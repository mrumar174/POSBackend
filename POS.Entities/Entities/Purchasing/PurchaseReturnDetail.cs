using POS.Entities.Catalog;
using POS.Entities.Common;

namespace POS.Entities.Purchasing
{
    public class PurchaseReturnDetail : ShopAuditableEntity
    {
        public int PurchaseReturnId { get; set; }
        public PurchaseReturn PurchaseReturn { get; set; } = default!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }
}

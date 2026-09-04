using POS.Entities.Catalog;
using POS.Entities.Common;

namespace POS.Entities.Sales
{
    public class SaleDetail : ShopAuditableEntity
    {
        public int SaleId { get; set; }
        public Sale Sale { get; set; } = default!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public decimal Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }
}

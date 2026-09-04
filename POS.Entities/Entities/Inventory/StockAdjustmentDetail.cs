using POS.Entities.Catalog;
using POS.Entities.Common;

namespace POS.Entities.Inventory
{
    public class StockAdjustmentDetail : ShopAuditableEntity
    {
        public int StockAdjustmentId { get; set; }
        public StockAdjustment StockAdjustment { get; set; } = default!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public decimal SystemQuantity { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public decimal DifferenceQuantity { get; set; }
        public decimal UnitCost { get; set; }

        public string? Remarks { get; set; }
    }
}

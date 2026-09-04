using POS.Entities.Common;

namespace POS.Entities.Inventory
{
    /// <summary>Shop-scoped: a physical stock count only makes sense for one branch's shelves.</summary>
    public class StockAdjustment : ShopAuditableEntity
    {
        public string AdjustmentNo { get; set; } = default!;
        public DateTime AdjustmentDate { get; set; } = DateTime.Now;
        public string Reason { get; set; } = default!;
        public string? Remarks { get; set; }

        public ICollection<StockAdjustmentDetail> StockAdjustmentDetails { get; set; } = new List<StockAdjustmentDetail>();
    }
}

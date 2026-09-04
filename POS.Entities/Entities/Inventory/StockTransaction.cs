using POS.Entities.Catalog;
using POS.Entities.Common;

namespace POS.Entities.Inventory
{
    /// <summary>
    /// Shop-scoped: THIS is the key table that makes multi-shop stock actually
    /// work. Stock for a given Product is never a single global number — it's
    /// SUM(QuantityIn - QuantityOut) filtered by (TenantId, ShopId, ProductId).
    /// The same Product can show 40 pieces at the Gulberg branch and 12 pieces
    /// at the DHA branch, purely by having separate StockTransaction rows per
    /// ShopId. TransferOut/TransferIn between two shops of the same tenant is
    /// just two StockTransaction rows (one per shop) referencing each other via
    /// ReferenceNo — no schema change needed.
    /// </summary>
    public class StockTransaction : ShopAuditableEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public StockTransactionType TransactionType { get; set; }

        public int? ReferenceId { get; set; }
        public string? ReferenceNo { get; set; }

        public decimal QuantityIn { get; set; }
        public decimal QuantityOut { get; set; }
        public decimal UnitCost { get; set; }

        public string? Remarks { get; set; }
    }
}

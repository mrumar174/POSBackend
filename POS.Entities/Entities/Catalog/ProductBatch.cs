using POS.Entities.Catalog;
using POS.Entities.Common;

namespace POS.Entities.Entities.Catalog
{
    /// <summary>Shop-scoped: stock lives per shop, so batches do too.
    /// BatchNo / ExpiryDate are optional depending on the product's tracking flags.</summary>
    public class ProductBatch : ShopAuditableEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public string? BatchNo { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }   // date part only
    }
}
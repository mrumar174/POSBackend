using POS.Entities.Common;

namespace POS.Entities.Catalog
{
    /// <summary>Tenant-scoped (inherits scope from Product). UQ_ProductBarcodes_Barcode becomes UQ(TenantId, Barcode) so two different tenants CAN reuse the same barcode value.</summary>
    public class ProductBarcode : TenantAuditableEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public string Barcode { get; set; } = default!;
        public bool IsPrimary { get; set; }
    }
}

using POS.Entities.Common;
using POS.Entities.Identity;
using POS.Entities.Inventory;
using POS.Entities.Purchasing;
using POS.Entities.Sales;

namespace POS.Entities.Catalog
{
    /// <summary>
    /// Tenant-scoped: the product catalog is centrally managed once per tenant
    /// and shared by every shop the tenant owns (this is what makes "add a
    /// product once, sell it from any branch" possible). PurchasePrice/SalePrice
    /// here are just the current defaults — actual historical prices are always
    /// captured on PurchaseDetail/SaleDetail so old invoices never change.
    /// Stock (CurrentStock) is intentionally NOT here: it's derived per-Shop
    /// from StockTransactions (see Inventory).
    /// </summary>
    public class Product : TenantAuditableEntity
    {
        public string ProductCode { get; set; } = default!;  // PRO-00001, unique per tenant
        public string Name { get; set; } = default!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = default!;

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public int UnitId { get; set; }
        public Unit Unit { get; set; } = default!;

        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinimumStock { get; set; }

        public string? Description { get; set; }

        // Navigation
        public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
        public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();
        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
        public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }
}

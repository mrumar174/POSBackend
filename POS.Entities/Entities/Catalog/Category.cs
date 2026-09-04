using POS.Entities.Common;

namespace POS.Entities.Catalog
{
    /// <summary>Tenant-scoped master data, shared across all of the tenant's shops. UQ_Categories_Name becomes UQ(TenantId, Name).</summary>
    public class Category : TenantAuditableEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

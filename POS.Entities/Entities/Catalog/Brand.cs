using POS.Entities.Common;

namespace POS.Entities.Catalog
{
    /// <summary>Tenant-scoped master data. UQ_Brands_Name becomes UQ(TenantId, Name).</summary>
    public class Brand : TenantAuditableEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

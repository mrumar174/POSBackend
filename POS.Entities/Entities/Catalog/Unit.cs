using POS.Entities.Common;

namespace POS.Entities.Catalog
{
    /// <summary>Tenant-scoped master data. UQ_Units_Name / UQ_Units_ShortName become UQ(TenantId, Name) / UQ(TenantId, ShortName).</summary>
    public class Unit : TenantAuditableEntity
    {
        public string Name { get; set; } = default!;
        public string ShortName { get; set; } = default!;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

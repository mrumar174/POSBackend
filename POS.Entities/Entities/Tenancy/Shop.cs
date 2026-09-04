using POS.Entities.Common;

namespace POS.Entities.Tenancy
{
    /// <summary>
    /// A physical branch/outlet belonging to a Tenant. A tenant with a single
    /// shop still gets exactly one row here (created automatically at signup),
    /// so the rest of the schema never has to special-case "single shop vs
    /// multi shop" — it's always shop-scoped.
    /// </summary>
    public class Shop : TenantAuditableEntity
    {
        public string Code { get; set; } = default!;      // SHOP-00001
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ContactNo { get; set; }
        public bool IsMainBranch { get; set; }

        /// <summary>Invoice/document number prefixes can be per-shop so two branches never collide, e.g. "LHR-SAL-00001" vs "ISB-SAL-00001".</summary>
        public string? InvoicePrefix { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = default!;
        public ICollection<UserShop> UserShops { get; set; } = new List<UserShop>();
    }
}

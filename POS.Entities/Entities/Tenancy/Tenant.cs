using POS.Entities.Common;
using POS.Entities.Identity;

namespace POS.Entities.Tenancy
{
    /// <summary>
    /// A Tenant = one customer/business that subscribes to your SaaS POS
    /// (e.g. "Ali General Store", "Fatima Electronics"). This is the top of
    /// the multi-tenancy tree. Every other table either belongs directly to a
    /// Tenant (shared master data) or to a Shop under a Tenant (branch data).
    /// One deployment, one database, N Tenants — this is what lets you onboard
    /// a new customer with zero extra hosting cost.
    /// </summary>
    public class Tenant : AuditableEntity
    {
        public string Code { get; set; } = default!;            // e.g. TEN-00001, used internally/support
        public string BusinessName { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string ContactNo { get; set; } = default!;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }

        /// <summary>Unique short slug used for subdomain/tenant resolution, e.g. "ali-store" -> ali-store.yourpos.pk</summary>
        public string Slug { get; set; } = default!;

        public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Trial;
        public string? SubscriptionPlanCode { get; set; }        // FK-less reference to a plan (Basic/Pro/Enterprise) kept simple on purpose
        public DateTime SubscriptionStartDate { get; set; } = DateTime.Now;
        public DateTime? SubscriptionEndDate { get; set; }

        /// <summary>Caps enforced in application code, not the DB — keeps pricing tiers simple.</summary>
        public int MaxShops { get; set; } = 1;
        public int MaxUsers { get; set; } = 5;

        /// <summary>99% of tenants = Shared. Only flip to Isolated for a big customer with their own DB.</summary>
        public DataIsolationMode DataIsolationMode { get; set; } = DataIsolationMode.Shared;

        /// <summary>Only populated when DataIsolationMode = Isolated. Store encrypted, never return via API/DTO.</summary>
        public string? IsolatedConnectionString { get; set; }

        // Navigation
        public ICollection<Shop> Shops { get; set; } = new List<Shop>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}

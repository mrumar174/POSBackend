using POS.Entities.Common;

namespace POS.Entities.Settings
{
    /// <summary>Tenant-scoped master data (Cash/Card/Bank/Credit), shared across the tenant's shops.</summary>
    public class PaymentMethod : TenantAuditableEntity
    {
        public string Name { get; set; } = default!;
    }
}

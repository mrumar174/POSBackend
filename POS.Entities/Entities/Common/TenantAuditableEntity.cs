namespace POS.Entities.Common
{
    /// <summary>
    /// Base for master/shared data that belongs to one Tenant (customer) but is shared
    /// across every Shop that tenant owns. Examples: Categories, Brands, Units, Products,
    /// Suppliers, Users, Roles.
    /// </summary>
    public abstract class TenantAuditableEntity : AuditableEntity, ITenantScoped
    {
        public int TenantId { get; set; }
    }
}

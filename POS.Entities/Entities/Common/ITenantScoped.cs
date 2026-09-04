namespace POS.Entities.Common
{
    /// <summary>
    /// Marks an entity as belonging to a single Tenant (customer/business account).
    /// EF Core applies a global query filter on TenantId so one customer can never
    /// see another customer's rows, even though they all live in the same database/tables.
    /// </summary>
    public interface ITenantScoped
    {
        int TenantId { get; set; }
    }
}

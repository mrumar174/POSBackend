namespace POS.Entities.Common
{
    /// <summary>
    /// Marks an entity as belonging to a specific Shop (branch/outlet) under a Tenant.
    /// Used for data that must be isolated per physical store: sales, purchases, stock,
    /// cash, expenses. Master/catalog data (Products, Categories, Users) is Tenant-scoped
    /// only, so it is shared across all of a tenant's shops.
    /// </summary>
    public interface IShopScoped : ITenantScoped
    {
        int ShopId { get; set; }
    }
}

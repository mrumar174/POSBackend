namespace POS.Entities.Common
{
    /// <summary>
    /// Base for transactional data that belongs to a single Shop (branch) within a Tenant.
    /// Examples: Sales, Purchases, StockTransactions, Expenses, CashTransactions.
    /// Carries TenantId too (denormalized) so a single global query filter/index on
    /// (TenantId, ShopId) can be used everywhere without an extra join.
    /// </summary>
    public abstract class ShopAuditableEntity : AuditableEntity, IShopScoped
    {
        public int TenantId { get; set; }
        public int ShopId { get; set; }
    }
}

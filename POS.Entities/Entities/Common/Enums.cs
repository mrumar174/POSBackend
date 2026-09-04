namespace POS.Entities.Common
{
    public enum StockTransactionType
    {
        OPENING = 1,
        PURCHASE = 2,
        SALE = 3,
        SALE_RETURN = 4,
        PURCHASE_RETURN = 5,
        ADJUSTMENT = 6
    }

    public enum CashTransactionType
    {
        SALE = 1,
        SALE_RETURN = 2,
        EXPENSE = 3,
        SUPPLIER_PAYMENT = 4,
        CASH_DEPOSIT = 5,
        CASH_WITHDRAWAL = 6,
        ADJUSTMENT = 7
    }

    /// <summary>
    /// Subscription state of a Tenant (customer). Drives whether the tenant's
    /// users are allowed to log in / transact.
    /// </summary>
    public enum SubscriptionStatus
    {
        Trial = 1,
        Active = 2,
        PastDue = 3,
        Suspended = 4,
        Cancelled = 5
    }

    /// <summary>
    /// How a Tenant's data is physically hosted. Almost every tenant will be
    /// Shared. Reserve Isolated for a large customer who pays for a dedicated DB.
    /// See ITenantConnectionResolver.
    /// </summary>
    public enum DataIsolationMode
    {
        Shared = 1,
        Isolated = 2
    }
}

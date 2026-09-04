namespace POS.DTOs.Common
{
    // Mirrors POS.Entities.Common.Enums exactly (same names/values). Duplicated
    // here — rather than referencing POS.Entities — because Services now live
    // in POS.Entities and need to consume POS.DTOs. If POS.DTOs also referenced
    // POS.Entities, that would be a circular project reference. AutoMapper maps
    // between the two copies by member name (configured in each Profile).
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

    public enum SubscriptionStatus
    {
        Trial = 1,
        Active = 2,
        PastDue = 3,
        Suspended = 4,
        Cancelled = 5
    }

    public enum DataIsolationMode
    {
        Shared = 1,
        Isolated = 2
    }
}

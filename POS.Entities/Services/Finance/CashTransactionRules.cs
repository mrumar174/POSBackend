using POS.Entities.Common; // adjust if CashTransactionType lives elsewhere

namespace POS.Entities.Services.Finance
{
    /// <summary>
    /// CashTransaction has one Amount column, so direction comes from the type.
    /// ONLY types listed here are added to the closing. Types auto-written for Sales/Expenses are NOT
    /// listed on purpose: those are already counted from the Sales/Expenses tables (no double counting).
    /// If you later write SupplierPayment rows to the ledger, add that type to Outflows.
    /// !! I haven't seen your enum: replace these members with your real manual cash-in / cash-out values.
    /// </summary>
    public static class CashTransactionRules
    {
        private static readonly HashSet<CashTransactionType> Inflows = new() { CashTransactionType.CASH_IN };
        private static readonly HashSet<CashTransactionType> Outflows = new() { CashTransactionType.CASH_OUT };

        public static bool IsInflow(CashTransactionType t) => Inflows.Contains(t);
        public static bool IsOutflow(CashTransactionType t) => Outflows.Contains(t);
    }
}
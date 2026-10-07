using POS.Entities.Common;

namespace POS.Entities.Finance
{
    /// <summary>Shop-scoped: end-of-day closing is done per branch, per day. UQ_DailyCashClosing_ClosingDate becomes UQ(ShopId, ClosingDate) — one closing per shop per day.</summary>
    public class DailyCashClosing : ShopAuditableEntity
    {
        public DateTime ClosingDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalCashIn { get; set; }    // TotalSales + other cash-in
        public decimal TotalCashOut { get; set; }   // TotalExpenses + other cash-out
        public decimal ClosingBalance { get; set; }
        public string? Remarks { get; set; }
    }
}

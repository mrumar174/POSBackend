using POS.Entities.Common;

namespace POS.Entities.Finance
{
    /// <summary>Shop-scoped: end-of-day closing is done per branch, per day. UQ_DailyCashClosing_ClosingDate becomes UQ(ShopId, ClosingDate) — one closing per shop per day.</summary>
    public class DailyCashClosing : ShopAuditableEntity
    {
        public DateOnly ClosingDate { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal TotalCashIn { get; set; }
        public decimal TotalCashOut { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal Difference { get; set; }
        public string? Remarks { get; set; }
    }
}

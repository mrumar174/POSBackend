using POS.Entities.Common;
using POS.Entities.Settings;

namespace POS.Entities.Sales
{
    /// <summary>
    /// Shop-scoped: the POS invoice header, tied to the till/branch that made
    /// the sale. InvoiceNo uniqueness becomes UQ(ShopId, InvoiceNo) so every
    /// branch can run its own SAL-00001 sequence independently.
    /// </summary>
    public class Sale : ShopAuditableEntity
    {
        public string InvoiceNo { get; set; } = default!;
        public DateTime SaleDate { get; set; } = DateTime.Now;

        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }

        public int? PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        public string? Remarks { get; set; }

        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
        public ICollection<SaleReturn> SaleReturns { get; set; } = new List<SaleReturn>();
    }
}

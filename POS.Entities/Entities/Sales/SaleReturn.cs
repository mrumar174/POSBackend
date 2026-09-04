using POS.Entities.Common;

namespace POS.Entities.Sales
{
    public class SaleReturn : ShopAuditableEntity
    {
        public string ReturnNo { get; set; } = default!;

        public int? SaleId { get; set; }
        public Sale? Sale { get; set; }

        public DateTime ReturnDate { get; set; } = DateTime.Now;

        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }

        public string? Remarks { get; set; }

        public ICollection<SaleReturnDetail> SaleReturnDetails { get; set; } = new List<SaleReturnDetail>();
    }
}

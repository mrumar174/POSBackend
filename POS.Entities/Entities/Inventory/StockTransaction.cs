using POS.DTOs.Common;
using POS.Entities.Catalog;
using POS.Entities.Common;
using POS.Entities.Entities.Catalog;

public class StockTransaction : ShopAuditableEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public StockTransactionType TransactionType { get; set; }
    public string ReferenceType { get; set; } = default!;
    public int? ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal UnitCost { get; set; }
    public string? Remarks { get; set; }
    public int? BatchId { get; set; }
    public ProductBatch? Batch { get; set; }
}
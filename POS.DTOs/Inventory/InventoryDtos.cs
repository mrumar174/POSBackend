using POS.DTOs.Common;

namespace POS.DTOs.Inventory
{
    // ---------- StockTransaction (read-only via API — written internally by
    // the Sales/Purchases/Adjustments services, never posted directly by a client) ----------
    public class StockTransactionDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public DateTime TransactionDate { get; set; }
        public StockTransactionType TransactionType { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceNo { get; set; }
        public decimal QuantityIn { get; set; }
        public decimal QuantityOut { get; set; }
        public decimal UnitCost { get; set; }
        public string? Remarks { get; set; }
    }

    // ---------- StockAdjustment ----------
    public class StockAdjustmentDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal SystemQuantity { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public decimal DifferenceQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? Remarks { get; set; }
    }

    public class CreateStockAdjustmentDetailDto
    {
        public int ProductId { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? Remarks { get; set; }
        // SystemQuantity and DifferenceQuantity are computed server-side at
        // the moment of posting, from the live StockTransaction balance —
        // never trust a client-supplied "what the system currently shows".
    }

    public class StockAdjustmentDto
    {
        public int Id { get; set; }
        public string AdjustmentNo { get; set; } = default!;
        public DateTime AdjustmentDate { get; set; }
        public string Reason { get; set; } = default!;
        public string? Remarks { get; set; }
        public List<StockAdjustmentDetailDto> Items { get; set; } = new();
    }

    public class CreateStockAdjustmentDto
    {
        public DateTime AdjustmentDate { get; set; }
        public string Reason { get; set; } = default!;
        public string? Remarks { get; set; }
        public List<CreateStockAdjustmentDetailDto> Items { get; set; } = new();
    }
}

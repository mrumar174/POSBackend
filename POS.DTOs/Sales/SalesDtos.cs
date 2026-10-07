namespace POS.DTOs.Sales
{
    // ---------- Sale ----------
    public class SaleDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class CreateSaleDetailDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.Flat;
        public decimal DiscountValue { get; set; }
        public decimal Tax { get; set; }
    }

    public class SaleDto
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = default!;
        public string? CustomerName { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal SubTotal { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public int? PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public string? Remarks { get; set; }
        public List<SaleDetailDto> Items { get; set; } = new();
    }

    // InvoiceNo generated server-side per Shop, same reasoning as Purchase.
    public class CreateSaleDto
    {
        public string? CustomerName { get; set; }
        public DateTime SaleDate { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.Flat;
        public decimal DiscountValue { get; set; }
        public decimal Tax { get; set; }
        public decimal PaidAmount { get; set; }
        public int? PaymentMethodId { get; set; }
        public string? Remarks { get; set; }
        public List<CreateSaleDetailDto> Items { get; set; } = new();
    }

    // ---------- SaleReturn ----------
    public class SaleReturnDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class CreateSaleReturnDetailDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class SaleReturnDto
    {
        public int Id { get; set; }
        public string ReturnNo { get; set; } = default!;
        public int? SaleId { get; set; }
        public string? InvoiceNo { get; set; }
        public string? CustomerName { get; set; }
        public DateTime ReturnDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public string? Remarks { get; set; }
        public List<SaleReturnDetailDto> Items { get; set; } = new();
    }

    public class CreateSaleReturnDto
    {
        public int? SaleId { get; set; }
        public DateTime ReturnDate { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public string? Remarks { get; set; }
        public List<CreateSaleReturnDetailDto> Items { get; set; } = new();
    }
    public class SaleReturnQueryDto
    {
        public string? ReturnNo { get; set; }
        public int? SaleId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

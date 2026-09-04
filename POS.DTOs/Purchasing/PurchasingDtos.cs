namespace POS.DTOs.Purchasing
{
    // ---------- Supplier ----------
    public class SupplierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? ContactPerson { get; set; }
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
    }

    public class CreateSupplierDto
    {
        public string Name { get; set; } = default!;
        public string? ContactPerson { get; set; }
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
    }

    public class UpdateSupplierDto : CreateSupplierDto
    {
        public int Id { get; set; }
    }

    // ---------- Purchase ----------
    public class PurchaseDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class CreatePurchaseDetailDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class PurchaseDto
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = default!;
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public int? PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public string? Remarks { get; set; }
        public List<PurchaseDetailDto> Items { get; set; } = new();
    }

    // InvoiceNo is intentionally NOT here — generate it server-side per Shop
    // (e.g. from Shop.InvoicePrefix + a running counter) so two concurrent
    // requests can never collide or let a client pick a duplicate number.
    public class CreatePurchaseDto
    {
        public int SupplierId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal PaidAmount { get; set; }
        public int? PaymentMethodId { get; set; }
        public string? Remarks { get; set; }
        public List<CreatePurchaseDetailDto> Items { get; set; } = new();
    }

    // ---------- PurchaseReturn ----------
    public class PurchaseReturnDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class CreatePurchaseReturnDetailDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class PurchaseReturnDto
    {
        public int Id { get; set; }
        public string ReturnNo { get; set; } = default!;
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int? PurchaseId { get; set; }
        public DateTime ReturnDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public string? Remarks { get; set; }
        public List<PurchaseReturnDetailDto> Items { get; set; } = new();
    }

    public class CreatePurchaseReturnDto
    {
        public int SupplierId { get; set; }
        public int? PurchaseId { get; set; }
        public DateTime ReturnDate { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public string? Remarks { get; set; }
        public List<CreatePurchaseReturnDetailDto> Items { get; set; } = new();
    }

    // ---------- SupplierPayment ----------
    public class SupplierPaymentDto
    {
        public int Id { get; set; }
        public string PaymentNo { get; set; } = default!;
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int? PurchaseId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public int PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
    }

    public class CreateSupplierPaymentDto
    {
        public int SupplierId { get; set; }
        public int? PurchaseId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public int PaymentMethodId { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
    }
}

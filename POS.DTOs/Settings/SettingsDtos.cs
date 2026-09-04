namespace POS.DTOs.Settings
{
    // ---------- PaymentMethod ----------
    public class PaymentMethodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class CreatePaymentMethodDto
    {
        public string Name { get; set; } = default!;
    }

    public class UpdatePaymentMethodDto : CreatePaymentMethodDto
    {
        public int Id { get; set; }
    }

    // ---------- CompanySettings (one row per Tenant — Get/Update only, no Create/Delete via API) ----------
    public class CompanySettingsDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = default!;
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public string? Email { get; set; }
        public string? TaxNumber { get; set; }
        public string Currency { get; set; } = "PKR";
        public string? ReceiptHeader { get; set; }
        public string? ReceiptFooter { get; set; }
        public string? LogoPath { get; set; }
    }

    public class UpdateCompanySettingsDto
    {
        public string CompanyName { get; set; } = default!;
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public string? Email { get; set; }
        public string? TaxNumber { get; set; }
        public string Currency { get; set; } = "PKR";
        public string? ReceiptHeader { get; set; }
        public string? ReceiptFooter { get; set; }
        public string? LogoPath { get; set; }
    }
}

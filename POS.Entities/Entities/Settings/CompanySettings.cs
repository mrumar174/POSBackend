using POS.Entities.Common;

namespace POS.Entities.Settings
{
    /// <summary>
    /// Tenant-scoped: one row per Tenant (not one row globally anymore, and not
    /// per-shop). Holds the business profile used on receipts/reports.
    /// If a shop later needs its own receipt header/footer/logo, add an
    /// optional ShopSettings table that falls back to CompanySettings — don't
    /// overload this table with ShopId to keep the "one company, N branches"
    /// concept clean.
    /// </summary>
    public class CompanySettings : TenantAuditableEntity
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

using POS.Entities.Common;

namespace POS.Entities.Purchasing
{
    /// <summary>Tenant-scoped: a supplier usually serves the whole business, not one branch.</summary>
    public class Supplier : TenantAuditableEntity
    {
        public string Name { get; set; } = default!;
        public string? ContactPerson { get; set; }
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }

        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();
        public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
    }
}

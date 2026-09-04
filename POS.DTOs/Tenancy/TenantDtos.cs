using POS.DTOs.Common;

namespace POS.DTOs.Tenancy
{
    public class TenantDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = default!;
        public string BusinessName { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string ContactNo { get; set; } = default!;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Slug { get; set; } = default!;
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public string? SubscriptionPlanCode { get; set; }
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public int MaxShops { get; set; }
        public int MaxUsers { get; set; }
        public DataIsolationMode DataIsolationMode { get; set; }
        // IsolatedConnectionString deliberately NOT exposed — internal only.
        public int ShopCount { get; set; }
        public int UserCount { get; set; }
    }

    public class CreateTenantDto
    {
        public string BusinessName { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string ContactNo { get; set; } = default!;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? SubscriptionPlanCode { get; set; }
        public int MaxShops { get; set; } = 1;
        public int MaxUsers { get; set; } = 5;

        // Seeds the first (main branch) shop created automatically with the tenant.
        public string MainShopName { get; set; } = "Main Branch";
    }

    public class UpdateTenantDto
    {
        public int Id { get; set; }
        public string BusinessName { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string ContactNo { get; set; } = default!;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public int MaxShops { get; set; }
        public int MaxUsers { get; set; }
    }

    public class UpdateSubscriptionDto
    {
        public int TenantId { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public string? SubscriptionPlanCode { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }
}
using POS.Entities.Common;
using POS.Entities.Tenancy;

namespace POS.Entities.Identity
{
    /// <summary>
    /// Tenant-scoped: one login belongs to exactly one Tenant. Which Shop(s)
    /// that login can operate at is controlled by UserShop.
    /// NOTE: UserName must be unique per-Tenant, not globally
    /// (UQ_Users_TenantId_UserName), since two different shop owners in
    /// Pakistan may both want to use "admin" as their username.
    /// </summary>
    public class User : TenantAuditableEntity
    {
        public string UserName { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string? ContactNo { get; set; }
        public string? Address { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = default!;
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<UserShop> UserShops { get; set; } = new List<UserShop>();
    }
}

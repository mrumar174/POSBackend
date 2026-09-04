using POS.Entities.Common;

namespace POS.Entities.Identity
{
    /// <summary>
    /// Tenant-scoped: each tenant manages its own roles (Admin/Manager/Cashier/
    /// Storekeeper/Accountant). A default set is seeded automatically when a
    /// new Tenant is created; the tenant admin can add custom roles afterwards.
    /// </summary>
    public class Role : TenantAuditableEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}

namespace POS.Entities.Identity
{
    /// <summary>Composite PK (RoleId, PermissionId). Tenant isolation is inherited from Role.</summary>
    public class RolePermission
    {
        public int RoleId { get; set; }
        public Role Role { get; set; } = default!;

        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = default!;
    }
}

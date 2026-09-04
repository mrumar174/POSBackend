using POS.Entities.Common;

namespace POS.Entities.Identity
{
    /// <summary>
    /// NOT tenant-scoped. Permissions are the fixed, system-wide list of
    /// actions your application understands (Products.View, Sales.Create,
    /// Reports.Profit, ...). They are seeded once for the whole platform and
    /// simply get attached to each tenant's Roles via RolePermission.
    /// </summary>
    public class Permission : AuditableEntity
    {
        public string Name { get; set; } = default!;     // "Sales.Create"
        public string Module { get; set; } = default!;    // "Sales"
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}

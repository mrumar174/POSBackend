namespace POS.Entities.Identity
{
    /// <summary>Composite PK (UserId, RoleId). Tenant isolation is inherited from User/Role.</summary>
    public class UserRole
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = default!;
    }
}

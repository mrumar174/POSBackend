namespace POS.DTOs.Identity
{
    // ---------- User ----------
    // PasswordHash is never exposed. Create uses a plain Password field, hashed server-side.
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<int> ShopIds { get; set; } = new();
    }

    public class CreateUserDto
    {
        public string UserName { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public List<int> RoleIds { get; set; } = new();
        public List<int> ShopIds { get; set; } = new();
    }

    public class UpdateUserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public List<int> RoleIds { get; set; } = new();
        public List<int> ShopIds { get; set; } = new();
    }

    public class ChangePasswordDto
    {
        public int UserId { get; set; }
        public string CurrentPassword { get; set; } = default!;
        public string NewPassword { get; set; } = default!;
    }

    // ---------- Role ----------
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public class CreateRoleDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public List<int> PermissionIds { get; set; } = new();
    }

    public class UpdateRoleDto : CreateRoleDto
    {
        public int Id { get; set; }
    }

    // ---------- Permission (global, seeded — read-only via API in practice) ----------
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Module { get; set; } = default!;
        public string? Description { get; set; }
    }
    public class CreatePermissionDto
    {
        public string Name { get; set; } = default!;      // "Sales.Create"
        public string Module { get; set; } = default!;     // "Sales"
        public string? Description { get; set; }
    }
}

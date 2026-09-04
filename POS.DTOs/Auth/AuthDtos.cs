using POS.DTOs.Identity;

namespace POS.DTOs.Auth
{
    public class LoginDto
    {
        public int TenantId { get; set; }
        public string UserName { get; set; } = default!;
        public string Password { get; set; } = default!;

        // Optional: which shop this session operates at. If omitted, the
        // user's first assigned shop is used. Required if the user has
        // access to more than one shop.
        public int? ShopId { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = default!;
        public DateTime ExpiresAtUtc { get; set; }
        public int TenantId { get; set; }
        public int ShopId { get; set; }
        public UserDto User { get; set; } = default!;
    }
    public class TenantSignupDto
    {
        // Tenant fields
        public string BusinessName { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string ContactNo { get; set; } = default!;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string MainShopName { get; set; } = "Main Branch";

        // First admin user
        public string AdminUserName { get; set; } = default!;
        public string AdminFullName { get; set; } = default!;
        public string AdminPassword { get; set; } = default!;
    }
}
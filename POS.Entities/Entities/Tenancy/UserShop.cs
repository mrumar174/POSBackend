using POS.Entities.Identity;

namespace POS.Entities.Tenancy
{
    /// <summary>
    /// Which shop(s) a user is allowed to operate at. A cashier is usually
    /// pinned to one shop; a tenant owner/admin can be granted every shop.
    /// Composite PK (UserId, ShopId).
    /// </summary>
    public class UserShop
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public int ShopId { get; set; }
        public Shop Shop { get; set; } = default!;

        /// <summary>The shop selected automatically after login / on the POS till screen.</summary>
        public bool IsDefault { get; set; }
    }
}

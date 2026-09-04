using System.Security.Claims;
using POS.Entities.Common;

namespace POS.Web.Services
{
    /// <summary>
    /// Reads TenantId / ShopId / UserId from the authenticated user's JWT claims.
    /// Register as Scoped. These claims must be added when you issue the login
    /// token — never trust a TenantId/ShopId sent in the request body/query
    /// string, always resolve it here.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public int TenantId =>
            int.TryParse(Principal?.FindFirst("tenant_id")?.Value, out var id) ? id : 0;

        public int? ShopId =>
            int.TryParse(Principal?.FindFirst("shop_id")?.Value, out var id) ? id : null;

        public int? UserId =>
            int.TryParse(Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public bool IsPlatformAdmin => Principal?.IsInRole("PlatformAdmin") ?? false;
    }
}

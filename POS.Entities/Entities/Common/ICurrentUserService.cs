namespace POS.Entities.Common
{
    /// <summary>
    /// Abstraction the Entities-layer services depend on to know "who is calling
    /// right now" — which Tenant, which Shop, which User. The concrete
    /// implementation lives in the API layer (it reads the JWT claims /
    /// HttpContext) and is registered as Scoped in DI. Every service in this
    /// project should resolve TenantId/ShopId from here rather than accepting
    /// them as raw parameters from the client, so a malicious client can never
    /// pass someone else's TenantId.
    /// </summary>
    public interface ICurrentUserService
    {
        int TenantId { get; }
        int? ShopId { get; }
        int? UserId { get; }
        bool IsAuthenticated { get; }

        /// <summary>True for a platform-level super admin (you, the SaaS owner) who can cross tenant boundaries for support/reporting.</summary>
        bool IsPlatformAdmin { get; }
    }
}

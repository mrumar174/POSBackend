using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace POS.Web.Authorization
{
    public class CustomAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        // Use the default handler for scenarios we don't want to customize (like 401 Unauthorized)
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public async Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                // This matches exactly what your Angular frontend expects (err.error.message)
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "You do not have the required permissions to perform this action."
                });

                return; // Short-circuit the request pipeline
            }

            // Fallback to the default behavior for success or other types of failures
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
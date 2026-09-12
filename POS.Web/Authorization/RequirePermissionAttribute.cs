using Microsoft.AspNetCore.Authorization;

namespace POS.Web.Authorization
{
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(string permission) : base()
        {
            Policy = $"Permission:{permission}";
        }
    }
}
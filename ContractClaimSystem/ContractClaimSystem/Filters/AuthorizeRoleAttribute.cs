using ContractClaimSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ContractClaimSystem.Filters
{
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _allowedRoles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var sessionService = context.HttpContext.RequestServices.GetService<SessionService>();
            var userRole = sessionService?.GetUserRole();

            if (string.IsNullOrEmpty(userRole) || !_allowedRoles.Contains(userRole))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}

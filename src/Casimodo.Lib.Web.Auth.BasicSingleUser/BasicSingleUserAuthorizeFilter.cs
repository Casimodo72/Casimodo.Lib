// TODO: Not used yet. Move to Web MVC lib?
#if (false)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ga.Web.Web.Auth
{
    public class BasicSingleUserAuthorizeFilter : IAuthorizationFilter
    {
        readonly string _realm;
        readonly BasicSingleUserAuthInfo _userInfo;

        public BasicSingleUserAuthorizeFilter(string realm, BasicSingleUserAuthInfo userInfo)
        {
            _realm = realm;
            _userInfo = userInfo;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var authResult = BasicSingleUserAuthHelper.Evaluate(context.HttpContext.Request, _userInfo);
            if (authResult != BasicAuthResult.Ok)
            {
#if (false)
                // Return authentication scheme. This should cause browsers to show a login page.
                context.HttpContext.Response.Headers["WWW-Authenticate"] = "Basic";

                if (!string.IsNullOrEmpty(_realm))
                    context.HttpContext.Response.Headers["WWW-Authenticate"] += $@" realm=""{_realm}""";
#endif

                // Unauthorized
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
#endif


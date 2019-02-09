using System.Web;
using Casimodo.Lib.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Casimodo.Lib.Web.Auth
{
    // KABU TODO: REFACT: This is an action auth attribute but
    //   we use it like a view auth thingy.
    public class MvcActionAuthAttribute :
        Microsoft.AspNetCore.Authorization.AuthorizeAttribute,
        Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter
    {
        public string Part { get; set; }
        public string Group { get; set; }
        public string VRole { get; set; }

        public void OnAuthorization(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!context.HttpContext.RequestServices
                .GetRequiredService<ActionAuthManager>()
                .IsPermitted(context.HttpContext.User,
                    action: CommonAuthVerb.View, part: Part, group: Group, vrole: VRole))
            {
                context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
            }
        }
    }
}

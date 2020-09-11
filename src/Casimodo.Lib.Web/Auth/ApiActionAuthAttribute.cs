using Casimodo.Lib.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Casimodo.Lib.Web.Auth
{
    // KABU TODO: also see https://stackoverflow.com/questions/31464359/how-do-you-create-a-custom-authorizeattribute-in-asp-net-core/41348219#41348219

    public class ApiActionAuthAttribute
        : Microsoft.AspNetCore.Authorization.AuthorizeAttribute,
          Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter
    {
        public string Part { get; set; }
        public string Group { get; set; }
        public string VRole { get; set; }
        public string Action { get; set; }

        public void OnAuthorization(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // KABU TODO: IMPORTANT: NOTE that we use vrole: "*". This is hopefully just a temporary
            //   workaround for not yet defining api-actions and permissions for
            //   every defined view-action and its permissions. I.e. currently we are using the
            //   view-actions permissions also for api-action authorization.
            if (!context.HttpContext.RequestServices
                .GetRequiredService<ActionAuthManager>()
                .IsPermitted(context.HttpContext.User, action: Action, part: Part, group: Group, vrole: "*"))
            {
                context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
            }
        }
    }
}

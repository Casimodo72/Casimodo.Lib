using Casimodo.Lib.Auth;
using Microsoft.AspNet.Identity.Owin;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;

namespace Casimodo.Lib.Web.Auth
{
    public class ApiActionAuthAttribute : System.Web.Http.AuthorizeAttribute
    {
        public string Part { get; set; }
        public string Group { get; set; }
        public string VRole { get; set; }
        public string Action { get; set; }

        protected override bool IsAuthorized(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var isAuthorized = base.IsAuthorized(actionContext);
            if (!isAuthorized)
                return false;

            // KABU TODO: IMPORTANT: NOTE that we use vrole: "*". This is hopefully just a temporary
            //   workaround for not yet defining api-actions and permissions for
            //   every defined view-action and its permissions. I.e. currently we are using the
            //   view-actions permissions also for api-action authorization.
            return actionContext.Request.GetOwinContext()
                .Get<ActionAuthManager>()
                .IsPermitted(actionContext.RequestContext.Principal, action: Action, part: Part, group: Group, vrole: "*");
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {

            string message = "Unauthorized resource";
            //base.HandleUnauthorizedRequest(actionContext);

            if (actionContext.Request.Properties.TryGetValue("Microsoft.AspNet.OData.Path", out object prop))
                if (prop is Microsoft.AspNet.OData.Routing.ODataPath odataPath)
                    message = $"Unauthorized OData resource '{odataPath.NavigationSource.Name}'";

            actionContext.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(message)
            };
        }
    }
}

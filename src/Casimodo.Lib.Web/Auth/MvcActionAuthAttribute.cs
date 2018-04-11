using System.Web;
using Casimodo.Lib.Auth;
using Microsoft.AspNet.Identity.Owin;

namespace Casimodo.Lib.Web.Auth
{
    public class MvcActionAuthAttribute : System.Web.Mvc.AuthorizeAttribute
    {
        public string Part { get; set; }
        public string Group { get; set; }
        public string VRole { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var isAuthorized = base.AuthorizeCore(httpContext);
            if (!isAuthorized)
                return false;

            return httpContext.GetOwinContext()
                .Get<ActionAuthManager>()
                .IsPermitted(httpContext.User, action: CommonAuthVerb.View, part: Part, group: Group, vrole: VRole);
        }
    }
}

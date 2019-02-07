// TODO: Not used yet. Move to Web MVC lib?
#if (false)
using Microsoft.AspNetCore.Mvc;
using System;

namespace Ga.Web.Web.Auth
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BasicSingleUserAuthorizeAttributeAttribute : TypeFilterAttribute
    {
        public BasicSingleUserAuthorizeAttributeAttribute(string realm)
            : base(typeof(BasicSingleUserAuthorizeFilter))
        {
            Arguments = new object[]
            {
                realm
            };
        }
    }
}
#endif


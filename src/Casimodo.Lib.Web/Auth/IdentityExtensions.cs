using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace Casimodo.Lib.Auth
{
    public static class IdentityExtensions
    {
        public static string GetUserId(this ClaimsPrincipal identity)
        {
            return identity.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public static string GetFullName(this IPrincipal user)
        {
            var claim = ((ClaimsIdentity)user.Identity).FindFirst(ClaimTypes.Name);
            return claim == null ? null : claim.Value;
        }

        public static string GetAddress(this IPrincipal user)
        {
            var claim = ((ClaimsIdentity)user.Identity).FindFirst(ClaimTypes.StreetAddress);
            return claim == null ? null : claim.Value;
        }
    }
}

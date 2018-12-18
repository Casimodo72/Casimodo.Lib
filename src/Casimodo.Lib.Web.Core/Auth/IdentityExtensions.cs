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

        public static bool IsInAnyRole(this IPrincipal user, params string[] roles)
        {
            if (roles == null || roles.Length == 0)
                return false;

            for (int i = 0; i < roles.Length; i++)
                if (user.IsInRole(roles[i]))
                    return true;

            return false;

            // NOTE: The approach below would also work.
#if (false)
            var claimRoles = ((ClaimsIdentity)user.Identity).FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray();
            if (claimRoles.Length == 0)
                return false;

            for (int i = 0; i < roles.Length; i++)
                for (int j = 0; j < claimRoles.Length; j++)
                    if (roles[i] == claimRoles[j])
                        return true;

            return false;
#endif
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

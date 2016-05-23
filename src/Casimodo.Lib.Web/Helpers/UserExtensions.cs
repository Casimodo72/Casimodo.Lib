﻿using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Casimodo.Lib.Web
{
    public static class UserExtensions
    {
        public static bool IsInAnyRole(this IPrincipal user, params string[] roles)
        {
            if (roles == null || roles.Length == 0)
                return false;

            return roles.Any(user.IsInRole);

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
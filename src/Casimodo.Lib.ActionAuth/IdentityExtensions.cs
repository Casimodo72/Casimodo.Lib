#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Casimodo.Lib.Auth
{
    public static class IdentityExtensions
    {
        public static IEnumerable<string> GetUserRoles(this IIdentity identity)
        {
            return ((ClaimsIdentity)identity).Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value);
        }

        public static string GetUserId(this IIdentity identity)
        {
            var userIdClaim = ((ClaimsIdentity)identity).Claims
               .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                throw new InvalidOperationException("No user ID claim found in identity.");
            }

            return userIdClaim.Value;
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
    }
}

﻿using System.Collections.Generic;
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
    }
}

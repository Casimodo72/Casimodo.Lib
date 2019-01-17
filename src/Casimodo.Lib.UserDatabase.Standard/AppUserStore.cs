using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;

namespace Casimodo.Lib.Identity
{
    public class AppUserStore : UserStore<User, Role, UserDbContext, Guid, UserClaim, UserRole, UserLogin, UserToken, RoleClaim>
    {
        public AppUserStore(UserDbContext context, IdentityErrorDescriber describer = null)
            : base(context, describer)
        { }
    }
}
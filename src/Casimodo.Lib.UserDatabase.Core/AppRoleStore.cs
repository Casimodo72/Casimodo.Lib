using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace Casimodo.Lib.Identity
{
    public class AppRoleStore : RoleStore<Role, UserDbContext, Guid, UserRole, RoleClaim>
    {
        public AppRoleStore(UserDbContext context)
            : base(context)
        { }
    }
}
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Data.Entity;

namespace Casimodo.Lib.Identity
{
    public class AppRoleStore : RoleStore<Role, Guid, UserRole>
    {
        public AppRoleStore(DbContext context)
            : base(context)
        { }
    }
}
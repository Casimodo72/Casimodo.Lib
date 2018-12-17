using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Identity.Migrations
{
    sealed partial class DbMigrationSeed
    {
        public AppUserStore UserStore;
        public UserManager<User, Guid> UserManager;       

        public DbMigrationSeed()
        {
            IsEnabled = false;
        }

        void Add(User user, string roles = null, string pw = null)
        {
            UserStore.TenantId = user.TenantId;

            var newRoles = GetRoles(roles).ToList();

            IdentityResult result;
            var prev = UserManager.FindById(user.Id);
            if (prev != null)
            {
                user = AutoMapper.Mapper.Map(user, prev);
                result = UserManager.Update(user);
            }
            else
            {
                result = UserManager.Create(user, pw);
            }

            if (!result.Succeeded)
            {
                throw new Exception(string.Format("Failed to create or update a user: " + result.Errors.Join(", ")));
            }

            IList<string> oldRoles = null;

            if (prev != null)
            {
                oldRoles = UserManager.GetRoles(user.Id);

                // Remove from roles.
                foreach (var role in oldRoles.Where(x => !newRoles.Contains(x)))
                    UserManager.RemoveFromRole(user.Id, role);
            }

            // Add to roles.
            foreach (var role in newRoles.Where(x => oldRoles == null || !oldRoles.Contains(x)))
                UserManager.AddToRole(user.Id, role);
        }

        public IEnumerable<string> GetRoles(string roles)
        {
            if (roles == null) return new string[0];

            return roles.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
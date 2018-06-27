using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Casimodo.Lib.Identity
{
    // You can add profile data for the user by adding more properties to your AppIdentityUser class,
    // please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public partial class User : IdentityUser<Guid, UserLogin, UserRole, UserClaim>, IUser<Guid>
    {
        public User()
        { }

        public User(string userName)
        {
            UserName = userName;
        }

        public Guid? TenantId { get; set; }

        public bool IsDeleted { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<User, Guid> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }
}
using Microsoft.AspNetCore.Identity;
using System;

namespace Casimodo.Lib.Identity
{
    // You can add profile data for the user by adding more properties to your AppIdentityUser class,
    // please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    //[Table("AuthUsers")]
    public partial class User : IdentityUser<Guid>
    {
        public User()
        { }

        public bool IsDeleted { get; set; }

        // KABU TODO: IMPORTANT: Do we need this in NET Core?
        //public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<User, Guid> manager)
        //{
        //    // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
        //    var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
        //    // Add custom user claims here
        //    return userIdentity;
        //}
    }
}
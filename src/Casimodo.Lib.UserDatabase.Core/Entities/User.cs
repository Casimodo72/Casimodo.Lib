using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Casimodo.Lib.Identity
{
    // You can add profile data for the user by adding more properties to your AppIdentityUser class,
    // please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    //[Table("AuthUsers")]
    public partial class User : IdentityUser<Guid>
    {
        public User()
        { }

        //public User(string userName)
        //{
        //    UserName = userName;
        //}

        public Guid? TenantId { get; set; }

        //public string FullName { get; set; }

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
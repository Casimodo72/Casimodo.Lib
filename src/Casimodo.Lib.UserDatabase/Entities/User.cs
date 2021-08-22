using Microsoft.AspNetCore.Identity;
using System;

namespace Casimodo.Lib.Identity
{
    // You can add profile data for the user by adding more properties to your AppIdentityUser class,
    // please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public partial class User : IdentityUser<Guid>
    {
        public User()
        { }

        public bool IsDeleted { get; set; }
    }
}
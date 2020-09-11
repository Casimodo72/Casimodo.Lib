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

        public Guid? TenantId { get; set; }

        public bool IsDeleted { get; set; }
    }
}
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Casimodo.Lib.Identity
{
    [Table("UserRoles")]
    public class UserRole : IdentityUserRole<Guid>
    { 
        [Key]
        [Column(Order=1)]
        public override Guid UserId { get; set; }

        [Key]
        [Column(Order=2)]
        public override Guid RoleId { get; set; }
    }
}
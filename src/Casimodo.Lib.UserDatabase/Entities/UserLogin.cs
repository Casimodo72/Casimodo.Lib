using Casimodo.Lib.Data;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Casimodo.Lib.Identity
{
    [Table("UserLogins")]
    [KeyInfo(PropName = "UserId")]
    public class UserLogin : IdentityUserLogin<Guid>
    {
        public Guid? TenantId { get; set; }
    }
}
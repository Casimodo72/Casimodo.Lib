using Casimodo.Lib.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Casimodo.Lib.Identity
{
    //[Table("AuthUserLogins")]
    [KeyInfo(PropName = "UserId")]
    public class UserLogin : IdentityUserLogin<Guid>
    {
        public Guid? TenantId { get; set; }
    }
}
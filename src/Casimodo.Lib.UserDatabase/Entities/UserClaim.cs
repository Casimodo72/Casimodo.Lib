using Casimodo.Lib.Data;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Casimodo.Lib.Identity
{
    [Table("UserClaims")]
    [KeyInfo(PropName = "Id")]
    public class UserClaim : IdentityUserClaim<Guid>
    { }
}
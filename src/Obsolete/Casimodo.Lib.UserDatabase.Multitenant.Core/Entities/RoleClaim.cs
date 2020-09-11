using Casimodo.Lib.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Casimodo.Lib.Identity
{
    //[Table("AuthRoleClaims")]
    [KeyInfo(PropName = "Id")]
    public class RoleClaim : IdentityRoleClaim<Guid>
    { }
}
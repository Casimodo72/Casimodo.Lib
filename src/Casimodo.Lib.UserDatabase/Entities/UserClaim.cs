using Casimodo.Lib.Data;
using Microsoft.AspNetCore.Identity;
using System;

namespace Casimodo.Lib.Identity
{
    //[Table("AuthUserClaims")]
    [KeyInfo(PropName = "Id")]
    public class UserClaim : IdentityUserClaim<Guid>
    { }
}
using Casimodo.Lib.Data;
using Microsoft.AspNetCore.Identity;
using System;

namespace Casimodo.Lib.Identity
{
    //[Table("AuthUserLogins")]
    [KeyInfo(PropName = "UserId")]
    public class UserLogin : IdentityUserLogin<Guid>
    { }
}
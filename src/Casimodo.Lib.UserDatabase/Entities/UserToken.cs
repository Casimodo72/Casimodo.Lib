using Microsoft.AspNetCore.Identity;
using System;

namespace Casimodo.Lib.Identity
{
    //[Table("AuthUserTokens")]
    public partial class UserToken : IdentityUserToken<Guid>
    { }
}
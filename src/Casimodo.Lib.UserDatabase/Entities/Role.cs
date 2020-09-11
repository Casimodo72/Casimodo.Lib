using Microsoft.AspNetCore.Identity;
using System;

namespace Casimodo.Lib.Identity
{
    //[Table("AuthRoles")]
    public partial class Role : IdentityRole<Guid>
    { }
}
using Microsoft.AspNet.Identity.EntityFramework;
using System;

namespace Casimodo.Lib.Identity
{
    public partial class Role : IdentityRole<Guid, UserRole>
    { }
}
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Casimodo.Lib.Identity
{
    //[Table("AuthRoles")]
    public partial class Role : IdentityRole<Guid>
    { }
}
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Casimodo.Lib.Identity
{
    //[Table("AuthUserTokens")]
    public partial class UserToken : IdentityUserToken<Guid>
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
    }
}
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace Casimodo.Lib.Identity
{
    public partial class UserDbContext : IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public UserDbContext(DbContextOptions<UserDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>().ToTable("AuthUsers");
            builder.Entity<UserClaim>().ToTable("AuthUserClaims");
            builder.Entity<UserLogin>().ToTable("AuthUserLogins");
            builder.Entity<UserToken>().ToTable("AuthUserTokens");
            builder.Entity<Role>().ToTable("AuthRoles");
            builder.Entity<RoleClaim>().ToTable("AuthRoleClaims");
            builder.Entity<UserRole>().ToTable("AuthUserRoles");

            OnModelCreatingSeed(builder);
        }
    }
}
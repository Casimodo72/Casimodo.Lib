using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Casimodo.Lib.Identity
{
    /// <summary>
    /// Multitenant ASP.NET Core identity DbContext.
    /// </summary>
    public partial class UserDbContext : UserDbContextBase<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public UserDbContext(DbContextOptions<UserDbContext> options)
            : base(options)
        { }

        //public Guid TenantId { get; set; }

        void OnCreatingMain()
        { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // User
            builder.Entity<User>(b =>
            {
                b.ToTable("AuthUsers");
                // NOTE: Uses tenant
                b.HasIndex(u => new { u.TenantId, u.NormalizedUserName }).HasName("UIX_NormalizedUserName").IsUnique();
                b.HasIndex(u => new { u.TenantId, u.NormalizedEmail }).HasName("UIX_NormalizedEmail").IsUnique();

                b.Property(u => u.TenantId).IsRequired();
                b.Property(u => u.UserName).IsRequired();
                b.Property(u => u.NormalizedUserName).IsRequired();
            });

            // User claim
            builder.Entity<UserClaim>().ToTable("AuthUserClaims");

            // User login
            builder.Entity<UserLogin>(b =>
            {
                b.ToTable("AuthUserLogins");
                // NOTE: Uses tenant
                b.HasKey(l => new { l.TenantId, l.LoginProvider, l.ProviderKey });
                b.Property(l => l.TenantId).IsRequired();
            });

            // User token
            builder.Entity<UserToken>().ToTable("AuthUserTokens");

            // Role
            builder.Entity<Role>(b =>
            {
                b.ToTable("AuthRoles");
                b.Property(u => u.Name).HasMaxLength(256);
                b.Property(u => u.NormalizedName).HasMaxLength(256);
            });

            builder.Entity<RoleClaim>(b =>
            {
                b.ToTable("AuthRoleClaims");
            });

            // User role
            builder.Entity<UserRole>().ToTable("AuthUserRoles");

            OnModelCreatingSeed(builder);
        }
    }
}
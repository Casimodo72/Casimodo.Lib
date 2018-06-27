using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.Validation;
using System.Globalization;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Casimodo.Lib.Identity
{
    // Multitenant:
    // https://www.scottbrady91.com/ASPNET-Identity/Quick-and-Easy-ASPNET-Identity-Multitenancy
    // https://github.com/JSkimming/AspNet.Identity.EntityFramework.Multitenant/blob/master/src/AspNet.Identity.EntityFramework.Multitenant/MultitenantUserStore.Generic.cs
    public partial class UserDbContext : IdentityDbContext<User, Role, Guid, UserLogin, UserRole, UserClaim>
    {
        static UserDbContext()
        {
            Database.SetInitializer<UserDbContext>(new UserDbInitializer());
        }

        public Guid TenantId { get; set; }

        public static void DummyInitialize()
        {
            using (var db = new UserDbContext())
            {
                db.Roles.Find(Guid.NewGuid());
            }
        }

        public UserDbContext()
            : base("Casimodo.Lib.Identity.UserDbContext")
        // ServiceLocator.Current.GetInstance<IDbConnectionStringProvider>().Get("Casimodo.Lib.Identity.UserDbContext")
        {
            OnCreatingMain();
        }

        void OnCreatingMain()
        {
            // KABU TODO: REMOVE hooks, because not needed anymore.
            //EnsureHooked();

            // See http://stackoverflow.com/questions/8099949/entity-framework-mvc3-temporarily-disable-validation
            Configuration.ValidateOnSaveEnabled = false;

            // See http://stackoverflow.com/questions/5917478/what-causes-attach-to-be-slow-in-ef4/5921259#5921259
            //Configuration.AutoDetectChangesEnabled = false;  

            Configuration.ProxyCreationEnabled = false;
        }

        public override int SaveChanges()
        {
            // KABU TODO: REMOVE hooks, because not needed anymore.
            // NOTE: We need to ensure hooks are initialized also here,
            // because the constructor will *not* be called when EF is seeding.
            //EnsureHooked();
            return base.SaveChanges();
        }

        // Source: https://aspnetidentity.codeplex.com/SourceControl/latest#src/Microsoft.AspNet.Identity.EntityFramework/IdentityDbContext.cs
        // Multitenant: https://github.com/JSkimming/AspNet.Identity.EntityFramework.Multitenant/blob/master/src/AspNet.Identity.EntityFramework.Multitenant/MultitenantIdentityDbContext.cs
        protected override void OnModelCreating(DbModelBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("modelBuilder");
            }

            base.OnModelCreating(builder);

            // Needed to ensure subclasses share the same table
            var user = builder.Entity<User>().ToTable("AuthUsers");

            user.Property(e => e.TenantId)
                .IsRequired()
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UIX_UserNameIndex", order: 0) { IsUnique = true }));

            user.Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UIX_UserNameIndex", order: 1) { IsUnique = true }));

            user.Property(u => u.Email).HasMaxLength(256);
            user.HasMany(u => u.Roles).WithRequired().HasForeignKey(ur => ur.UserId);
            user.HasMany(u => u.Claims).WithRequired().HasForeignKey(uc => uc.UserId);
            user.HasMany(u => u.Logins).WithRequired().HasForeignKey(ul => ul.UserId);

            // User role
            builder.Entity<UserRole>().ToTable("AuthUserRoles")
                .HasKey(r => new { r.UserId, r.RoleId });

            // User login
            var userLogin = builder.Entity<UserLogin>().ToTable("AuthUserLogins")
                .HasKey(l => new { l.TenantId, l.UserId, l.LoginProvider, l.ProviderKey });
            userLogin.Property(l => l.TenantId)
                .IsRequired();

            // User claim
            builder.Entity<UserClaim>().ToTable("AuthUserClaims");

            // Role
            var role = builder.Entity<Role>().ToTable("AuthRoles");
            role.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UIX_RoleNameIndex") { IsUnique = true }));

            role.HasMany(r => r.Users).WithRequired().HasForeignKey(ur => ur.RoleId);
        }

        global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                    _resourceManager =
                        new global::System.Resources.ResourceManager(
                            "Microsoft.AspNet.Identity.EntityFramework.IdentityResources",
                            typeof(Microsoft.AspNet.Identity.EntityFramework.IdentityUser).Assembly);

                return _resourceManager;
            }
        }
        global::System.Resources.ResourceManager _resourceManager;

        protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry,
                IDictionary<object, object> items)
        {
            if (entityEntry != null && entityEntry.State == EntityState.Added)
            {
                var errors = new List<DbValidationError>();
                var user = entityEntry.Entity as User;
                //check for uniqueness of user name and email
                if (user != null)
                {
                    if (Users.Any(u => u.TenantId == user.TenantId && String.Equals(u.UserName, user.UserName)))
                    {
                        errors.Add(new DbValidationError("User",
                            String.Format(CultureInfo.CurrentCulture, ResourceManager.GetString("DuplicateUserName"), user.UserName)));
                    }
                    if (RequireUniqueEmail && Users.Any(u => u.TenantId == user.TenantId && String.Equals(u.Email, user.Email)))
                    {
                        errors.Add(new DbValidationError("User",
                            String.Format(CultureInfo.CurrentCulture, ResourceManager.GetString("DuplicateEmail"), user.Email)));
                    }
                }
                else
                {
                    var role = entityEntry.Entity as Role;
                    //check for uniqueness of role name
                    if (role != null && Roles.Any(r => String.Equals(r.Name, role.Name)))
                    {
                        errors.Add(new DbValidationError("Role",
                            String.Format(CultureInfo.CurrentCulture, ResourceManager.GetString("RoleAlreadyExists"), role.Name)));
                    }
                }
                if (errors.Any())
                {
                    return new DbEntityValidationResult(entityEntry, errors);
                }
            }
            return base.ValidateEntity(entityEntry, items);
        }
    }
}
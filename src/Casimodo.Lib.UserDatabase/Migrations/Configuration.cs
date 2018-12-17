namespace Casimodo.Lib.Identity.Migrations
{
    using Microsoft.AspNet.Identity;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    internal sealed class Configuration : DbMigrationsConfiguration<Casimodo.Lib.Identity.UserDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            AutomaticMigrationDataLossAllowed = false;
        }

        protected override void Seed(Casimodo.Lib.Identity.UserDbContext context)
        {
            AutoMapper.Mapper.Initialize(c => c.CreateMap<User, User>());

            //  This method will be called after migrating to the latest version.
            using (var transaction = context.Database.BeginTransaction())
            {
                SeedCore(context);

                transaction.Commit();
            }
        }

        void SeedCore(UserDbContext context)
        {
            // See http://stackoverflow.com/questions/19745286/asp-net-identity-db-seed

            // Roles ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            var roleManager = new RoleManager<Role, Guid>(new AppRoleStore(context));

            if (!roleManager.RoleExists("Admin"))
            {
                roleManager.Create(new Role
                {
                    Index = 1,
                    Name = "Admin",
                    DisplayName = "Administrator",
                    Id = Guid.Parse("14eea27c-697f-4494-9413-bcdd47611c20")
                });
            }
            if (!roleManager.RoleExists("CoAdmin"))
            {
                roleManager.Create(new Role
                {
                    Index = 2,
                    Name = "CoAdmin",
                    DisplayName = "Co-Administrator",
                    Id = Guid.Parse("d40c9767-596e-44da-9698-56b177ec17d6")
                });
            }
            if (!roleManager.RoleExists("Manager"))
            {
                roleManager.Create(new Role
                {
                    Index = 2,
                    Name = "Manager",
                    DisplayName = "Manager",
                    Id = Guid.Parse("b8c9137f-9cff-4a2e-a2fc-ef82d0f837c5")
                });
            }
            if (!roleManager.RoleExists("ScheduleManager"))
            {
                roleManager.Create(new Role
                {
                    Index = 2,
                    Name = "ScheduleManager",
                    DisplayName = "Kalender Manager",
                    Id = Guid.Parse("0b7f2958-83e5-4894-9fbc-1a16c45d2d91")
                });
            }
            if (!roleManager.RoleExists("ProjectMailInboxOperator"))
            {
                roleManager.Create(new Role
                {
                    Index = 2,
                    Name = "ProjectMailInboxOperator",
                    DisplayName = "ProjectMailInboxOperator",
                    Id = Guid.Parse("49dc3550-4366-49fe-a5a0-998a632e03b4")
                });
            }
            if (!roleManager.RoleExists("AnyEmployee"))
            {
                roleManager.Create(new Role
                {
                    Index = 100,
                    Name = "AnyEmployee",
                    DisplayName = "Mitarbeiter (eigener/externer/fremder)",
                    Id = Guid.Parse("09a0692b-126e-4a2e-bbad-ee0081dd3d47")
                });
            }
            if (!roleManager.RoleExists("Employee"))
            {
                roleManager.Create(new Role
                {
                    Index = 3,
                    Name = "Employee",
                    DisplayName = "Mitarbeiter (eigener)",
                    Id = Guid.Parse("77bd192e-db17-4a6c-ab4e-29ed53b72c7a")
                });
            }
            if (!roleManager.RoleExists("ExternEmployee"))
            {
                roleManager.Create(new Role
                {
                    Index = 4,
                    Name = "ExternEmployee",
                    DisplayName = "Mitarbeiter (externer)",
                    Id = Guid.Parse("9b9a7457-0239-437e-b5c3-03065894d329")
                });
            }
            if (!roleManager.RoleExists("ForeignEmployee"))
            {
                roleManager.Create(new Role
                {
                    Index = 5,
                    Name = "ForeignEmployee",
                    DisplayName = "Mitarbeiter (fremder)",
                    Id = Guid.Parse("5179808a-afb5-479c-b482-b3663277cbd0")
                });
            }
            if (!roleManager.RoleExists("Customer"))
            {
                roleManager.Create(new Role
                {
                    Index = 5,
                    Name = "Customer",
                    DisplayName = "Kunde",
                    Id = Guid.Parse("ac26cbf0-473e-453f-8c65-e3e5fe26f0d6")
                });
            }

            // KABU TODO: REMOVE?
            //context.Roles.AddOrUpdate(x => x.Id, adminRole, coAdminRole, employeeRole, externRole, foreignRole);

            context.SaveChanges();

            // Users ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // See http://blog.falafel.com/seed-database-initial-users-mvc-5/

#if (DEBUGSEED)
            if (System.Diagnostics.Debugger.IsAttached == false)
                System.Diagnostics.Debugger.Launch();
#endif

            var seeder = new DbMigrationSeed();
            seeder.UserStore = new AppUserStore(context);
            seeder.UserManager = new UserManager<User, Guid>(seeder.UserStore);
            seeder.UserManager.UserValidator = new UserValidator<User, Guid>(seeder.UserManager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = false
            };

            seeder.UserManager.PasswordValidator = new VoidPasswordValidator();

            // KABU TODO: REMOVE
            //seeder.Level1Ps = "0815";
            //seeder.Level1Psh = new PasswordHasher().HashPassword(seeder.Level1Ps);

            //seeder.Level2Ps = "r2(36+9d2";
            //seeder.Level2Psh = new PasswordHasher().HashPassword(seeder.Level2Ps);

            //seeder.Level3Ps = "3ö)2#dDN8§nE32";
            //seeder.Level3Psh = new PasswordHasher().HashPassword(seeder.Level3Ps);

            seeder.Seed(context);

            context.SaveChanges();
        }
    }

    class VoidPasswordValidator : IIdentityValidator<string>

    {
        public VoidPasswordValidator()
        { }

        public Task<IdentityResult> ValidateAsync(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                return Task.FromResult(IdentityResult.Failed("The password must not be empty."));
            }

            //string pattern = @"^(?=.*[0-9])(?=.*[!@#$%^&*])[0-9a-zA-Z!@#$%^&*0-9]{10,}$";
            //if (!Regex.IsMatch(item, pattern))
            //{
            //    return Task.FromResult(IdentityResult.Failed("Password should have one numeral and one special character"));
            //}

            return Task.FromResult(IdentityResult.Success);
        }
    }
}
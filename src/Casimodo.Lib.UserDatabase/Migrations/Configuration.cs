namespace Casimodo.Lib.Identity.Migrations
{
    using Microsoft.AspNet.Identity;
    using System.Data.Entity.Migrations;
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

            // NOTE: Seed of default stuff removed from library code.
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
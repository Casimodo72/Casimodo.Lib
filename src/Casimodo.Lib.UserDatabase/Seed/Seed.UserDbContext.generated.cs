using System;
using System.Globalization;
using Casimodo.Lib.Data;
using Casimodo.Lib.Identity;

namespace Casimodo.Lib.Identity.Migrations
{
    partial class DbMigrationSeed : DbSeedBase
    {
        public UserDbContext Context { get; set; }

        public void Seed(UserDbContext context)
        {
            if (!IsEnabled) return;
            Context = context;
            SeedTime = DateTimeOffset.Parse("01/01/2016 02:00:00 +00:00", CultureInfo.InvariantCulture);

            SeedUsers();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public static class SeedExtensions
    {
        public static MojSeedItem AddSeed(this MojenApp app, MojSeedItemOptions options)
        {
            var seed = new MojSeedItem(app, options);
            seed.SeedBuilder.Use<EntityFromDbToSeedGen>(new EntityFromDbTransformationOptions
            {
                OrderBy = seed.OrderBy
            });

            seed.Prepare();

            app.Add(seed);

            return seed;
        }

        public static MojSeedItem AddAuthUserSeed(this MojenApp app, MojSeedItemOptions options)
        {
            var seed = new MojSeedItem(app, options);
            seed.SeedBuilder.Use<EntityUserFromDbToSeedGen>(new EntityFromDbTransformationOptions
            {
                OrderBy = seed.OrderBy
            });

            seed.Prepare();

            app.Add(seed);

            return seed;
        }
    }
}

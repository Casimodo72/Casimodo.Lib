using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public static class SeedExtensions
    {
        public static MojSeedItem AddSeedItem(this MojenApp app, MojSeedItemOptions options)
        {
            var seed = new MojSeedItem(app, options);
            seed.SeedBuilder.Use<EntityDbToSeedExporterGen>(new EntityExporterOptions
            {
                OrderBy = seed.OrderBy
            });

            seed.Prepare();

            app.Add(seed);

            return seed;
        }
    }
}

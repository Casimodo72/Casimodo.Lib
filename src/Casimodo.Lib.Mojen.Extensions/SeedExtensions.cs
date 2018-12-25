using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public static class SeedExtensions
    {
        public static void Seed(this MojenApp app, MojSeedItemOptions options)
        {
            SeedCore<EntityFromDbToSeedGen>(app, options);
        }

        public static void SeedAuthUser(this MojenApp app, MojSeedItemOptions options)
        {
            SeedCore<EntityUserFromDbToSeedGen>(app, options);
        }

        public static void SeedEnumEntity(this MojenDataLayerPackageBuildContext context, MojType type,
            Action<MojValueSetContainerBuilder> build)
        {
            var builder = context.Parent.AddItemsOfType(type)
                .UsePrimitiveKeys().Name("Name").Value("Id");

            context.App.Seed(new MojSeedItemOptions
            {
                Section = "StaticData",
                SeedBuilder = builder,
                AlwaysSeed = (seed) =>
                {
                    seed
                        .UseIndex()
                        .Seed("Name", "DisplayName", "Id");

                    build(builder);
                }
            });
        }

        public static MojValueSetContainerBuilder UsePrimitiveKeys(this MojValueSetContainerBuilder builder)
        {
            return builder
                .Use<PrimitiveKeysGen>()
                .Use<TsPrimitiveKeysGen>();
        }

        static void SeedCore<TTransformation>(MojenApp app, MojSeedItemOptions options)
            where TTransformation : EntityFromDbTransformationGenBase
        {
            if (!options.IsEnabled)
                return;

            var seed = new MojSeedItem(app, options);
            if (!seed.IsEnabled)
                return;

            if (seed.SeedConfig.IsDbImportEnabled)
            {
                if (!seed.IsDbImportEnabled)
                    return;

                // Remove all other generators when importing data from DB.
                seed.SeedBuilder.Config.UsingGenerators.Clear();
                // Add DB to seed transformation.
                seed.SeedBuilder.Use<TTransformation>(new EntityFromDbTransformationOptions
                {
                    OrderBy = seed.OrderBy
                });
            }

            seed.Build();

            app.Add(seed);
        }
    }
}

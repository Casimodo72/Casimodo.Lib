#nullable enable

namespace Casimodo.Mojen
{
    public class EnumEntitySeedOptions
    {
        public int IndexStart { get; set; }
    }

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

        public static void SeedEnumEntity(this MojenDataLayerPackage package,
            MojType type, EnumEntitySeedOptions? options,
            Action<MojValueSetContainerBuilder> build)
        {
            var builder = package.Context.AddItemsOfType(type)
                .UsePrimitiveKeys().Name("Name").Value("Id");

            package.App.Seed(new MojSeedItemOptions
            {
                Section = "StaticData",
                SeedBuilder = builder,
                AlwaysSeed = (seed) =>
                {
                    if (options != null)
                    {
                        seed.UseIndex("Index", options.IndexStart);
                    }

                    seed.Seed("Name", "DisplayName", "Id");

                    build(builder);
                }
            });
        }

        public static void SeedEnumEntity(this MojenDataLayerPackage package, MojType type, Action<MojValueSetContainerBuilder> build)
        {
            SeedEnumEntity(package, type, new EnumEntitySeedOptions(), build);
        }

        public static MojValueSetContainerBuilder UsePrimitiveKeys(this MojValueSetContainerBuilder builder)
        {
            return builder
                .ProducePrimitiveKeys()
                .Use<PrimitiveKeysGen>()
                .Use<TsPrimitiveKeysGen>();
        }

        static void SeedCore<TTransformation>(MojenApp app, MojSeedItemOptions options)
            where TTransformation : EntityFromDbTransformationGenBase
        {
            if (!options.IsEnabled)
                return;

            var seedConfig = new MojSeedConfig(app, options);
            if (!seedConfig.IsEnabled)
                return;

            if (seedConfig.GlobalSeedConfig.IsDbImportEnabled)
            {
                if (!seedConfig.IsDbImportEnabled)
                    return;

                // Remove all other generators when importing data from DB.
                seedConfig.SeedBuilder.Config.UsingGenerators.Clear();
                // Add DB to seed transformation.
                seedConfig.SeedBuilder.Use<TTransformation>(new EntityFromDbTransformationOptions
                {
                    OrderBy = seedConfig.ImportOrderBy
                });
            }

            seedConfig.Build();

            app.Add(seedConfig);
        }
    }
}

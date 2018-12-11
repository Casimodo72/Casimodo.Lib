using System;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class MojSeedItemOptions
    {
        public string Section { get; set; }
        public string OrderBy { get; set; }
        public MojValueSetContainerBuilder SeedBuilder { get; set; }
        public MojGeneratedDbSeed Seeder { get; set; }
        public Action<MojValueSetContainerBuilder> InitialSeed { get; set; }
    }

    public class MojSeedItem : MojBase
    {
        public MojSeedItem(MojenApp app, MojSeedItemOptions options)
        {
            App = app;
            SeedConfig = app.Get<MojGlobalDataSeedConfig>();

            Section = options.Section;
            OrderBy = options.OrderBy;
            SeedBuilder = options.SeedBuilder;
            InitialSeed = options.InitialSeed;

            IsEnabled = SeedConfig.IsSectionEnabled(Section);
            SeedBuilder.DbSeedEnabled(SeedConfig.IsSectionEnabledForDbSeed(Section));
            Seeder = options.Seeder;
        }

        public MojenApp App { get; set; }
        public MojGlobalDataSeedConfig SeedConfig { get; set; }
        public string Section { get; set; }
        public bool IsEnabled { get; set; }
        public string OrderBy { get; set; }
        public MojValueSetContainerBuilder SeedBuilder { get; set; }
        public Action<MojValueSetContainerBuilder> InitialSeed { get; set; }
        public Action<MojValueSetContainerBuilder> PopulateSeed { get; set; }
        public MojGeneratedDbSeed Seeder { get; set; }

        public void Prepare()
        {
            if (!SeedConfig.IsInitialSeedEnabled)
                SeedBuilder.SeedAllProps();
        }

        public void Execute()
        {
            if (!IsEnabled)
                return;

            if (SeedConfig.IsInitialSeedEnabled)
            {
                InitialSeed?.Invoke(SeedBuilder);
            }
            else
            {
                SeedBuilder.SeedAllProps();

                if (!SeedConfig.IsSourceDbDataFetchEnabled)
                {
                    if (Seeder != null)
                        Seeder.Populate(SeedBuilder);

                    PopulateSeed?.Invoke(SeedBuilder);
                }
            }
        }
    }

    public class MojDataSeedSectionConfig
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsDbSeedEnabled { get; set; }
    }

    public class MojGlobalDataSeedConfig : MojBase
    {
        public MojDataSeedSectionConfig[] Sections { get; set; } = Array.Empty<MojDataSeedSectionConfig>();
        public bool IsSeedGeneratorEnabled { get; set; }
        public bool IsSourceDbDataFetchEnabled { get; set; }
        public bool IsInitialSeedEnabled { get; set; }
        public string SourceDbConnectionString { get; set; }
        public string SourceDbDataFetchSeedXmlOutputDirPath { get; set; }
        public string SourceDbDataFetchSeedFileOutputDirPath { get; set; }

        public bool IsSectionEnabled(string name)
        {
            return Sections.Any(x => x.Name == name && x.IsEnabled);
        }

        public bool IsSectionEnabledForDbSeed(string name)
        {
            return Sections.Any(x => x.Name == name && x.IsDbSeedEnabled);
        }
    }

    public class MojGeneratedDbSeed
    {
        public void Populate(MojValueSetContainerBuilder seed)
        {
            seed.ClearSeedProps();
            PopulateCore(seed);
        }

        public virtual void PopulateCore(MojValueSetContainerBuilder seed)
        {

        }
    }
}

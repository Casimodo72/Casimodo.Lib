using System;
using System.Collections.Generic;
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
        public Action<MojValueSetContainerBuilder> Seed { get; set; }
        public Action<MojValueSetContainerBuilder> ConfigureDbImport { get; set; }
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
            ConfigureDbImport = options.ConfigureDbImport;
            Seed = options.Seed;

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
        public Action<MojValueSetContainerBuilder> Seed { get; set; }
        public Action<MojValueSetContainerBuilder> ConfigureDbImport { get; set; }
        public MojGeneratedDbSeed Seeder { get; set; }

        public void Prepare()
        {
            if (!SeedConfig.IsInitialSeedEnabled)
            {
                SeedBuilder.SeedAllProps();

                if (SeedConfig.IsDbImportEnabled)
                {
                    ConfigureDbImport?.Invoke(SeedBuilder);                   
                }

                SeedBuilder.Build();
            }
        }

        public void Execute()
        {
            if (!IsEnabled)
                return;

            if (SeedConfig.IsInitialSeedEnabled)
            {
                InitialSeed?.Invoke(SeedBuilder);
                SeedBuilder.Build();
            }
            else if (SeedConfig.IsDbImportEnabled)
            {
                // NOP 
            }
            else
            {
                Seed?.Invoke(SeedBuilder);
                SeedBuilder.Build();

                if (Seeder != null)
                    Seeder.Populate(SeedBuilder);
            }
        }
    }

    public class MojDataSeedSectionConfig
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsDbSeedEnabled { get; set; } = true;
    }

    public class MojGlobalDataSeedConfig : MojBase
    {
        public List<MojDataSeedSectionConfig> Sections { get; set; } = new List<MojDataSeedSectionConfig>();
        public bool IsDbSeedGeneratorEnabled { get; set; }
        public bool IsDbImportEnabled { get; set; }
        public bool IsInitialSeedEnabled { get; set; }
        public string DbImportConnectionString { get; set; }
        public string DbImportOutputXmlDirPath { get; set; }
        public string DbImportOutputSeedDirPath { get; set; }

        public MojGlobalDataSeedConfig AddSection(string name, bool dbseed = true)
        {
            Sections.Add(new MojDataSeedSectionConfig { Name = name, IsDbSeedEnabled = dbseed });

            return this;
        }

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

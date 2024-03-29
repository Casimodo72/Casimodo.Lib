﻿namespace Casimodo.Mojen
{
    public class MojSeedItemOptions
    {
        public bool IsEnabled { get; set; } = true;
        public string Section { get; set; }
        public string OrderBy { get; set; }
        public bool IsDbImportEnabled { get; set; } = true;

        public MojValueSetContainerBuilder SeedBuilder { get; set; }
        public MojGeneratedDbSeed Seeder { get; set; }
        public Action<MojValueSetContainerBuilder> AlwaysSeed { get; set; }
        public Action<MojValueSetContainerBuilder> InitialSeed { get; set; }
        public Action<MojValueSetContainerBuilder> Seed { get; set; }
        public Action<MojSeedConfig, MojValueSetContainerBuilder> ConfigureDbImport { get; set; }
        public Action<MojValueSetContainerBuilder> ConfigureSeeder { get; set; }
    }

    public class MojSeedConfig : MojBase
    {
        public MojSeedConfig(MojenApp app, MojSeedItemOptions options)
        {
            App = app;
            GlobalSeedConfig = app.Get<MojGlobalDataSeedConfig>();

            Section = options.Section;
            ImportOrderBy = options.OrderBy;
            SeedBuilder = options.SeedBuilder;
            TypeConfig = SeedBuilder.Config.TypeConfig;
            AlwaysSeed = options.AlwaysSeed;
            InitialSeed = options.InitialSeed;
            ConfigureDbImport = options.ConfigureDbImport;
            ConfigureSeeder = options.ConfigureSeeder;
            Seed = options.Seed;

            IsEnabled = GlobalSeedConfig.IsSectionEnabled(Section);
            SeedBuilder.DbSeedEnabled(GlobalSeedConfig.IsSectionEnabledForDbSeed(Section));
            IsDbImportEnabled = AlwaysSeed == null && options.IsDbImportEnabled;
            Seeder = options.Seeder;
        }

        public MojenApp App { get; set; }
        public MojGlobalDataSeedConfig GlobalSeedConfig { get; set; }
        public MojType TypeConfig { get; set; }
        public string Section { get; set; }
        public bool IsEnabled { get; set; }
        public string ImportOrderBy { get; set; }
        public string ImportFilter { get; set; }
        public bool IsDbImportEnabled { get; set; } = true;
        public MojValueSetContainerBuilder SeedBuilder { get; set; }
        public Action<MojValueSetContainerBuilder> AlwaysSeed { get; set; }
        public Action<MojValueSetContainerBuilder> InitialSeed { get; set; }
        public Action<MojValueSetContainerBuilder> Seed { get; set; }
        public Action<MojSeedConfig, MojValueSetContainerBuilder> ConfigureDbImport { get; set; }
        public Action<MojValueSetContainerBuilder> ConfigureSeeder { get; set; }
        public MojGeneratedDbSeed Seeder { get; set; }

        public void Build()
        {
            if (!IsEnabled)
                return;

            var config = App.Get<MojGlobalDataSeedConfig>();

            if (GlobalSeedConfig.IsInitialSeedEnabled)
            {
                if (InitialSeed != null)
                    InitialSeed(SeedBuilder);
                else
                    AlwaysSeed?.Invoke(SeedBuilder);

                SeedBuilder.Build();
            }
            else if (GlobalSeedConfig.IsDbImportEnabled)
            {
                SeedBuilder.SeedAllProps();
                ConfigureDbImport?.Invoke(this, SeedBuilder);
            }
            else if (SeedBuilder.Config.ProducesPrimitiveKeys)
            {
                if (AlwaysSeed == null)
                    throw new MojenException("Seed item must provice an 'AlwaysSeed' if used for primitive keys.");

                AlwaysSeed?.Invoke(SeedBuilder);
                SeedBuilder.Build();
            }
            else if (GlobalSeedConfig.IsDbSeedEnabled)
            {
                if (Seeder != null)
                {
                    SeedBuilder.SeedAllProps();
                    ConfigureSeeder?.Invoke(SeedBuilder);
                }

                if (Seed != null)
                    Seed(SeedBuilder);
                else
                    AlwaysSeed?.Invoke(SeedBuilder);

                if (Seeder != null)
                    Seeder.Populate(SeedBuilder);

                SeedBuilder.Build();
            }

            SeedBuilder = null;
        }
    }

    public class MojDataSeedSectionConfig
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsDbImportEnabled { get; set; } = true;
        public bool IsDbSeedEnabled { get; set; } = true;
    }

    public class MojGlobalDataSeedConfig : MojBase
    {
        public List<MojDataSeedSectionConfig> Sections { get; set; } = [];
        public bool IsDbSeedEnabled { get; set; }
       
        public bool IsInitialSeedEnabled { get; set; }

        public bool IsDbImportEnabled { get; set; }
        public string DbImportConnectionString { get; set; }
        public string DbImportOutputXmlDirPath { get; set; }
        public string DbImportOutputSeedDirPath { get; set; }

        public MojGlobalDataSeedConfig AddSection(string name, bool enabled = true, bool dbimport = false, bool dbseed = true)
        {
            Sections.Add(new MojDataSeedSectionConfig
            {
                Name = name,
                IsEnabled = enabled,
                IsDbImportEnabled = dbimport,
                IsDbSeedEnabled = dbseed
            });

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
            PopulateCore(seed);
        }

        public virtual void PopulateCore(MojValueSetContainerBuilder seed)
        {

        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class AutoMapperModelToStoreGen : AppPartGenerator
    {
        public AutoMapperModelToStoreGen()
        {
            Scope = "ModelContext";
        }

        public ViewModelLayerConfig ViewModelConfig { get; set; }

        public List<string> Namespaces { get; private set; }

        protected override void GenerateCore()
        {
            ViewModelConfig = App.Get<ViewModelLayerConfig>();

            if (string.IsNullOrEmpty(ViewModelConfig.AutoMapperDirPath)) return;
            if (ViewModelConfig.DataConfig == null) return;
            if (string.IsNullOrEmpty(ViewModelConfig.DataConfig.DbRepositoryCoreName)) return;

            PerformWrite(
                Path.Combine(ViewModelConfig.AutoMapperDirPath, "AutoMapperConfiguration.generated.cs"),
                () => GenerateAutoMapperConfiguration());
        }

        void GenerateAutoMapperConfiguration()
        {
            bool useStoreAlias = false;
            if (!string.IsNullOrEmpty(ViewModelConfig.AutoMapperStoreExternAlias))
            {
                useStoreAlias = true;
                O($"extern alias {ViewModelConfig.AutoMapperStoreExternAlias};");
                O($"using StoreTypes = {ViewModelConfig.AutoMapperStoreExternAlias}::{ViewModelConfig.DataConfig.DataNamespace};");
            }

            OUsing("System", "Casimodo.Lib", "Casimodo.Lib.Data");
            if (string.IsNullOrEmpty(ViewModelConfig.AutoMapperStoreExternAlias))
            {
                OUsing(ViewModelConfig.DataConfig.DataNamespace);
            }
         
            ONamespace(ViewModelConfig.Namespace);

            O("public static partial class AutoMapperConfiguration");
            Begin();

            // NOTE: We're using AutoMapper 8.
            O($"public static void ConfigureCore(AutoMapper.IMapperConfigurationExpression c, IDataMappingCore core)");
            Begin();
            O();

            foreach (MojType model in App.AllModels.OrderBy(x => x.Name))
            {
                if (model.Store == null)
                    continue;

                O("// " + model.Name);

                var storeClassName = model.Store.ClassName;
                if (useStoreAlias)
                {
                    storeClassName = "StoreTypes." + storeClassName;
                }
                var modelClassName = model.ClassName;

                // Mapping: entity --> model

                Oo($"c.CreateMap<{storeClassName}, {modelClassName}>()");

                var props = model
                    .GetProps()
                    .Where(x =>
                        x.Store == null
                        // || x.IsMappedToStore == false
                        || x.Store.IsExcludedFromDb);

                // Ignore non-mapped properties
                foreach (var prop in props)
                {
                    Br();
                    if (prop.Store != null && prop.Store.IsExcludedFromDb)
                    {
                        // KABU TODO: REVISIT:
                        // If the entity's property is not stored in the DB,
                        // then we can't map from entity --> model
                        // because of AutoMapper's LINQ projection.
                        // Entity framework will throw an exception in this case.
                        //
                        // AutoMapper: ExplicitExpansion: Ignores this member for LINQ projections
                        //  unless explicitly expanded during projection.
                        Oo($"    .ForMember(d => d.{prop.Name}, o => o.ExplicitExpansion())");
                    }
                    else
                    {
                        Oo($"    .ForMember(d => d.{ prop.Name}, o => o.Ignore())");
                    }
                }

                Br();

                // TODO: REMOVE? View model mapping with OData is not supported anymore.
                //if (model.IsODataOpenTypeEffective)
                //{
                //    // TODO: For now, just ignore the built-in DynamicProperties.
                //    Oo("    .ForMember(s => s.DynamicProperties, o => o.Ignore())");
                //    Br();
                //}

                Oo("    .AfterMap((s, d) => core.OnLoaded(d))");

                o(";");
                Br();

                // Mapping: model --> entity

                O($"MoAutoMapperInitializer.CreateModelToStoreMap<{modelClassName}, {storeClassName}>(c);");
                // KABU TODO: REVISIT: AfterMap is of no use to us, because it won't be called when projecting entity queries.
                //O("AutoMapper.Mapper.CreateMap<{0}, {1}>().AfterMap((e, m) => factory.OnLoaded(m));", model.Store.ClassName, model.ClassName);
                O();
            }

            End();
            End();
            End();
        }
    }
}
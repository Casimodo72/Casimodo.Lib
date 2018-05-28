using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class AutoMapperGen : AppPartGenerator
    {
        public AutoMapperGen()
        {
            Scope = "ModelContext";
        }

        public List<string> Namespaces { get; private set; }

        protected override void GenerateCore()
        {
            var context = App.Get<DataViewModelLayerConfig>();

            if (string.IsNullOrEmpty(context.DataViewModelAutoMapperDirPath)) return;
            if (context.DataConfig == null) return;
            if (string.IsNullOrEmpty(context.DataConfig.DbModelRepositoryCoreName)) return;

            PerformWrite(
                Path.Combine(context.DataViewModelAutoMapperDirPath, "AutoMapperConfiguration.generated.cs"),
                () => GenerateAutoMapperConfiguration(context));
        }

        void GenerateAutoMapperConfiguration(DataViewModelLayerConfig context)
        {
            OUsing("System", "Microsoft.Practices.ServiceLocation", "Casimodo.Lib", "Casimodo.Lib.Data");
            //App.GetForeignDataNamespaces(context.DataConfig.DataNamespace));            
            ONamespace(context.DataConfig.DataNamespace);
            O("public static partial class AutoMapperConfiguration");
            Begin();

            // NOTE: We're using AutoMapper 4.2.1.
            O("public static void ConfigureCore(AutoMapper.IMapperConfigurationExpression c)");
            Begin();

            O("var core = ServiceLocator.Current.GetInstance<{0}>();", context.DataConfig.DbModelRepositoryCoreName);

            O();

            foreach (MojType model in App.AllModels.OrderBy(x => x.Name))
            {
                if (model.Store == null)
                    continue;

                O("// " + model.Name);

                // Mapping: entity --> model

                Oo("c.CreateMap<{0}, {1}>()", model.Store.ClassName, model.ClassName);

                // Ignore non-mapped properties
                foreach (var prop in model
                    .GetProps()
                    .Where(x =>
                        x.Store == null
                        // || x.IsMappedToStore == false
                        || x.Store.IsExcludedFromDb))

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
                        Oo("    .ForMember(s => s.{0}, o => o.ExplicitExpansion())", prop.Name);
                    }
                    else
                    {
                        Oo("    .ForMember(s => s.{0}, o => o.Ignore())", prop.Name);
                    }
                }
                // KABU TODO: For now, just ignore the built-in DynamicProperties.
                Br();
                if (model.IsODataOpenTypeEffective)
                {
                    Oo("    .ForMember(s => s.DynamicProperties, (o) => o.Ignore())");
                    Br();
                }
                Oo("    .AfterMap((s, d) => core.OnLoaded(d))");

                o(";");
                Br();

                // Mapping: model --> entity

                O("MoAutoMapperInitializer.CreateMap<{0}, {1}>(c);", model.ClassName, model.Store.ClassName);
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
using Casimodo.Lib.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DbContextCreateMapGen : MojenGenerator
    {
        public DbContextCreateMapGen()
        {
            Scope = "DataContext";
        }

        public DataLayerConfig DataConfig { get; set; }

        protected override void GenerateCore()
        {
            DataConfig = App.Get<DataLayerConfig>();

            if (!DataConfig.DbContextUseMapping) return;
            if (string.IsNullOrEmpty(DataConfig.DbContextDirPath)) return;
            if (string.IsNullOrEmpty(DataConfig.DbContextName)) return;

            PerformWrite(Path.Combine(DataConfig.DbContextDirPath, DataConfig.DbContextName + ".CreateMap.generated.cs"),
                GenerateDbContextAutoMapperMapping);
        }

        void GenerateDbContextAutoMapperMapping()
        {
            var types = App.AllConcreteEntities.ToArray();

            OUsing(
                "System",
                "System.Collections.Generic",
                "System.Data.Entity",
                "System.Linq");

            ONamespace(DataConfig.DataNamespace);

            O($"public partial class {DataConfig.DbContextName}");
            Begin();

            O($"static void CreateMap()");
            Begin();

            // NOTE: We're using AutoMapper 4.2.1.
            O("AutoMapper.Mapper.Initialize(c =>");
            Begin();

            foreach (var type in types)
            {
                Oo("c.CreateMap<{0}, {0}>()", type.ClassName);
                // Ignore nagivation properties.
                foreach (var naviProp in type.GetProps()
                    // Exclude hidden collection props.
                    .Where(x => !x.IsHiddenCollectionNavigationProp)
                    .Where(x =>
                        x.IsNavigation &&
                        !x.Reference.Binding.HasFlag(MojReferenceBinding.Nested)))
                {
                    Br();
                    Oo($"    .ForMember(s => s.{naviProp.Name}, o => o.Ignore())");
                }
                oO(";");
            }

            End(");");

            End(); // Method
            End(); // Class
            End(); // Namespace
        }
    }
}
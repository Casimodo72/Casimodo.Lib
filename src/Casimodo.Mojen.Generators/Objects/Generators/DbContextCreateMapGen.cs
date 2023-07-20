using Casimodo.Lib.Data;
using System.IO;

namespace Casimodo.Mojen
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

            PerformWrite(Path.Combine(DataConfig.DbContextDirPath, DataConfig.DbContextName + ".CreateAutomapperConfig.generated.cs"),
                GenerateDbContextAutoMapperMapping);
        }

        void GenerateDbContextAutoMapperMapping()
        {
            var types = App.AllConcreteEntities.ToArray();

            O("#nullable enable");
            OUsing("System", "AutoMapper");
            O("#pragma warning disable CS8981");
            O($"using source = {DataConfig.DataNamespace};");
            O($"using dest = {DataConfig.DataNamespace};");
            O("#pragma warning restore CS8981");
            O();

            ONamespace(DataConfig.DataNamespace);

            O($"public partial class {DataConfig.DbContextName}");
            Begin();

            O($"public static void ConfigureAutoMapper(IMapperConfigurationExpression c, Action<IMapperConfigurationExpression>? configure = null)");
            Begin();

            foreach (var type in types)
            {
                Oo($"c.CreateMap<source.{type.ClassName}, dest.{type.ClassName}>()");
                // Ignore nagivation properties.
                foreach (var naviProp in type.GetProps()
                    // Exclude hidden collection props.
                    .Where(x => !x.IsHiddenCollectionNavigationProp)
                    .Where(x =>
                        x.IsNavigation &&
                        !x.Reference.Binding.HasFlag(MojReferenceBinding.Nested)))
                {
                    Br();
                    Oo($"    .ForMember(d => d.{naviProp.Name}, o => o.Ignore())");
                }
                oO(";");
            }

            O();
            O(@"configure?.Invoke(c);");

            End(); // Method
            End(); // Class
            End(); // Namespace
        }
    }
}
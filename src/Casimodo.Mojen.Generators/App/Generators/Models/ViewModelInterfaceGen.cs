using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class ViewModelInterfaceGen : ClassGen
    {
        public ViewModelInterfaceGen()
        {
            Scope = "Context";
        }

        public ViewModelLayerConfig ViewModelConfig { get; set; }

        protected override void GenerateCore()
        {
            ViewModelConfig = App.Get<ViewModelLayerConfig>();

            if (ViewModelConfig == null) return;
            if (string.IsNullOrEmpty(DataConfig.InterfaceDirPath)) return;

            foreach (var iface in App.GetTypes(MojTypeKind.Interface))
            {
                PerformWrite(Path.Combine(ViewModelConfig.InterfacesDirPath, iface.ClassName + ".generated.cs"),
                    () => GenerateInterface(iface));
            }
        }

        public void GenerateInterface(MojType type)
        {
            OUsing(ViewModelConfig.Namespaces);

            ONamespace(ViewModelConfig.Namespace);

            // Interface declaration

            O("public partial interface {0}{1}",
                type.ClassName,
                (type.EffectiveBaseClassName != null ? " : " + type.EffectiveBaseClassName : ""));

            // Interfaces
            if (type.Interfaces.Any())
            {
                if (type.HasBaseClass)
                    Oo("    , ");
                else
                    Oo("    : ");

                o(type.Interfaces.Select(x => x.Name).Join(", "));
                Br();
            }

            Begin();

            // Properties
            O();
            int i = -1;
            foreach (var prop in type.GetLocalProps(custom: false))
            {
                i++;

                if (i > 0)
                    O();

                OSummary(prop.Summary);

                O($"{GetPropertyType(type, prop)} {prop.Name} {{ get; set; }}");
            }

            End();
            End();
        }

        string GetPropertyType(MojType type, MojProp prop)
        {
            // Ensure that we reference a view model.
            if (prop.Type.IsMojType && !prop.Type.TypeConfig.IsModel())
                throw new MojenException("Property on interface is expected to be a view model.");

            return prop.Type.Name;
        }
    }
}
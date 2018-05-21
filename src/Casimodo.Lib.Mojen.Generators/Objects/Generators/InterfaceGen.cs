using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class InterfaceGen : ClassGen
    {
        public InterfaceGen()
        {
            Scope = "Context";
        }

        public DataLayerConfig DataConfig { get; set; }

        protected override void GenerateCore()
        {
            DataConfig = App.Get<DataLayerConfig>();

            if (DataConfig == null) return;
            if (string.IsNullOrEmpty(DataConfig.InterfaceDirPath)) return;

            foreach (var iface in App.GetTypes(MojTypeKind.Interface))
            {
                PerformWrite(Path.Combine(App.Get<DataLayerConfig>().InterfaceDirPath, iface.ClassName + ".generated.cs"),
                    () => GenerateInterface(iface));
            }
        }

        public void GenerateInterface(MojType type)
        {
            OUsing(App.Get<DataLayerConfig>().DataNamespaces);

            ONamespace(type.Namespace);

            // Interface declaration

            O("public partial interface {0}{1}",
                type.ClassName,
                (type.EffectiveBaseClassName != null ? " : " + type.EffectiveBaseClassName : ""));

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

                O("{0} {1} {{ get; set; }}", prop.Type.Name, prop.Name);
            }

            End();
            End();
        }
    }
}
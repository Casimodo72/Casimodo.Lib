using System.IO;

namespace Casimodo.Mojen
{
    // TODO: Currently we just return the display names.
    public class DataViewModelLocalizerGen : ClassGen
    {
        public DataViewModelLocalizerGen()
        {
            Scope = "App";

            Namespaces = new List<string>
            {
               "System"
            };
        }

        public ViewModelLayerConfig ViewModelConfig { get; set; }

        public List<string> Namespaces { get; private set; }

        protected override void GenerateCore()
        {
            ViewModelConfig = App.Get<ViewModelLayerConfig>();

            if (string.IsNullOrEmpty(ViewModelConfig.ModelsDirPath))
                return;

            PerformWrite(
                Path.Combine(ViewModelConfig.ModelsDirPath, "DataViewModelLocalizer.generated.cs"),
                () => GenerateModel(App.AllModels.ToList()));
        }

        public void GenerateModel(List<MojType> types)
        {
            OUsing("System");

            OFileScopedNamespace(ViewModelConfig.Namespace);
            O();

            O("public class ViewModelLocalizer");
            Begin();

            O("public string GetDisplayName(Type type, bool plural = false)");
            Begin();

            O("return type switch");
            Begin();
            foreach (var type in types)
            {
                var name = !string.IsNullOrEmpty(type.DisplayName)
                    ? type.DisplayName
                    : type.Name;

                var pluralName = !string.IsNullOrEmpty(type.DisplayPluralName)
                    ? type.DisplayPluralName
                    : name;

                O($"_ when type == typeof({type.ClassName}) => plural ? \"{pluralName}\" : \"{name}\",");
            }
            O($"_ => null");
            End(";");

            End();

            End(); // class
        }
    }
}
using System.IO;

namespace Casimodo.Mojen
{
    public class TsAnyKeysGen : DataLayerGenerator
    {
        public TsAnyKeysGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();

            var configs = App.GetItems<MojAnyKeysConfig>().ToArray();
            if (!configs.Any())
                return;

            PerformWrite(Path.Combine(webConfig.TypeScriptDataDirPath, $"Primitives.AnyKeys.generated.ts"), () =>
            {
                OTsNamespace(webConfig.ScriptNamespace, () =>
                {
                    foreach (var config in configs)
                        GenerateAnyKeys(config);
                });

            });
        }

        public void GenerateAnyKeys(MojAnyKeysConfig config)
        {
            OTsClass(name: config.ClassName, hasconstructor: false,
            content: () =>
            {
                foreach (var item in config.Items)
                    O($"public static {item.Key} = {Moj.JS(item.Value)};");
            });
            O();
        }
    }
}
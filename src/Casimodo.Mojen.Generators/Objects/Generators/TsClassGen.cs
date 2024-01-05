using System.IO;

namespace Casimodo.Mojen
{
    public class TsClassGen : TsGenBase
    {
        public TsClassGen()
        {
            Scope = "App";
        }

        public WebDataLayerConfig WebConfig { get; set; }

        static int GetInheritanceDepth(MojType type)
        {
            int depth = 0;
            while ((type = type.BaseClass) != null)
                depth++;

            return depth;
        }

        protected override void GenerateCore()
        {
            WebConfig = App.Get<WebDataLayerConfig>();
            var outputDirPath = WebConfig.TypeScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
                .Where(x => !x.WasGenerated)
                .Where(x => !x.IsTenant)
                .OrderBy(x => GetInheritanceDepth(x))
                .ThenBy(x => x.Name)
                .ToArray();

            PerformWrite(Path.Combine(outputDirPath, "DataTypes.generated.ts"), () =>
            {
                OTsNamespace(WebConfig.ScriptNamespace, () =>
                {
                    var gen = new TsXClassGen(new TsXClassGenOptions
                    {
                        UseCamelCase = false,
                        GenerateInterfaces = true,
                        PrefixInterfaces = true,
                        UseDefaultValues = true
                    });

                    gen.WebConfig = WebConfig;
                    gen.Use(Writer);
                    gen.Push();
                    foreach (var item in items)
                    {
                        gen.Generate(item, false);
                        O();
                    }
                    gen.Pop();
                });
            });
        }
    }
}
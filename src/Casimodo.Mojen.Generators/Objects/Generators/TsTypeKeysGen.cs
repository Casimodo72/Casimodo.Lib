using System.IO;
#nullable enable

namespace Casimodo.Mojen
{
    public class TsTypeKeysGenOptions
    {
        public bool IsModule { get; set; }
        public string[]? TypeNames { get; set; }
        public string[]? OutputDirPaths { get; set; }
        public string? FileName { get; set; }
    }

    public class TsTypeKeysGen : TsGenBase
    {
        public TsTypeKeysGen()
        {
            Scope = "App";
        }

        public TsTypeKeysGen(TsTypeKeysGenOptions? options = null)
            : this()
        {
            if (options != null)
                Options = options;
        }

        public TsTypeKeysGenOptions Options { get; set; } = new TsTypeKeysGenOptions();

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();

            var outputDirPaths = new List<string>();
            if (Options.OutputDirPaths?.Length > 0)
            {
                outputDirPaths.AddRange(Options.OutputDirPaths);
            }
            else
            {
                outputDirPaths.Add(webConfig.TypeScriptDataDirPath);
            }

            var fileName = Options.FileName ?? "Primitives.TypeKeys.generated";

            fileName += ".ts";

            foreach (var outputDirPath in outputDirPaths)
            {
                PerformWrite(Path.Combine(outputDirPath, fileName),
                    () =>
                    {
                        if (Options.IsModule)
                        {
                            GenerateTypeKeys();
                        }
                        else
                        {
                            OTsNamespace(webConfig.ScriptNamespace, () =>
                            {
                                GenerateTypeKeys();
                            });
                        }
                    });
            }
        }

        public void GenerateTypeKeys()
        {
            var className = "TypeKeys";

            OTsClass(name: className, hasconstructor: false,
                content: () =>
            {
                var types = new List<MojType>();
                foreach (var type in App.GetTypes())
                {
                    if (types.Any(x => x.Id == type.Id))
                        continue;

                    if (Options.TypeNames?.Contains(type.Name) == false)
                        continue;

                    types.Add(type);
                }

                foreach (var type in types)
                    O($@"static {type.Name} = ""{type.Id}"";");

                O();
                OB("static getNameById(id: string): string | null");
                O($"return {className}.#id2Name[id] ?? null;");
                End();

                O();
                OB("static #id2Name: { [id: string]: string } =");
                foreach (var type in types)
                    O($@"""{type.Id}"": ""{type.Name}"",");
                End(";");
            });
        }
    }
}
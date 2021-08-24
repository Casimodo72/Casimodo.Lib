using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class TsTypeKeysGenOptions
    {
        public string OutputDirPath { get; set; }
        public string FileName { get; set; }
        public string[] IncludeTypes { get; set; }
        public bool IsModule { get; set; }
    }

    public class TsTypeKeysGen : DataLayerGenerator
    {
        public TsTypeKeysGen()
        {
            Scope = "App";
        }

        public TsTypeKeysGen(TsTypeKeysGenOptions options = null)
            : this()
        {
            if (options != null)
                Options = options;
        }

        public TsTypeKeysGenOptions Options { get; set; } = new TsTypeKeysGenOptions();

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();

            var outputDirPath = Options.OutputDirPath ?? webConfig.TypeScriptDataDirPath;
            var fileName = Options.FileName ?? "Primitives.TypeKeys.generated";

            fileName += ".ts";

            if (string.IsNullOrEmpty(outputDirPath)) return;

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

                    if (Options.IncludeTypes?.Contains(type.Name) == false)
                        continue;

                    types.Add(type);
                }

                foreach (var type in types)
                    O($@"public static {type.Name} = ""{type.Id}"";");

                O();
                OB("private static _id2Name: { [id: string]: string } =");
                foreach (var type in types)
                    O($@"""{type.Id}"": ""{type.Name}"",");
                End(";");

                O();
                OB("public static getNameById(id: string): string");
                O($"return {className}._id2Name[id] || null;");
                End();
            });
        }
    }
}
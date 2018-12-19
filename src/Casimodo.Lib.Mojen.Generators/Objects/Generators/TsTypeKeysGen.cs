using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class TsTypeKeysGen : DataLayerGenerator
    {
        public TsTypeKeysGen()
        {
            Scope = "App";
        }

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();

            if (string.IsNullOrEmpty(webConfig.TypeScriptDataDirPath)) return;

            PerformWrite(Path.Combine(webConfig.TypeScriptDataDirPath, "Primitives.TypeKeys.generated.ts"),
                () =>
                {
                    OTsNamespace(webConfig.ScriptNamespace, () =>
                    {
                        GenerateTypeKeys();
                    });
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

                    types.Add(type);
                }

                foreach (var type in types)
                    O($"public static {type.Name} = '{type.Id}';");

                O();
                OB("private static _id2Name =");
                foreach (var type in types)
                    O($"'{type.Id}': '{type.Name}',");
                End();

                O();
                OB("public static getNameById(id)");
                O($"return {className}._id2Name[id] || null;");
                End();
            });
        }
    }
}
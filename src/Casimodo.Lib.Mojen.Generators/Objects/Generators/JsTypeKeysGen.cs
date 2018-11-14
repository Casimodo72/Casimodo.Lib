﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class JsTypeKeysGen : DataLayerGenerator
    {
        public JsTypeKeysGen()
        {
            Scope = "App";
        }

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();

            if (string.IsNullOrEmpty(webConfig.JavaScriptDataDirPath)) return;

            PerformWrite(Path.Combine(webConfig.JavaScriptDataDirPath, "primitives.TypeKeys.generated.js"),
                () =>
                {
                    OJsNamespace(webConfig.ScriptNamespace, () =>
                    {
                        GenerateTypeKeys();
                    });
                });
        }

        public void GenerateTypeKeys()
        {
            OJsClass(name: "TypeKeys", isstatic: true,
                constructor: () =>
            {
                var types = new List<MojType>();
                foreach (var type in App.GetTypes())
                {
                    if (types.Any(x => x.Id == type.Id))
                        continue;

                    types.Add(type);
                }

                foreach (var type in types)
                    O($"this.{type.Name} = '{type.Id}';");

                O();
                OB("var _id2Name =");
                foreach (var type in types)
                    O($"'{type.Id}': '{type.Name}',");
                End();

                O();
                OB("this.getNameById = function(id)");
                O("return _id2Name[id] || null;");
                End();
            });
        }
    }
}
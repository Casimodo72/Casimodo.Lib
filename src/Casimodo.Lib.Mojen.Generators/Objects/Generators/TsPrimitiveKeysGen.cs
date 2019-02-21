﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public partial class TsPrimitiveKeysGen : DataLayerGenerator
    {
        public TsPrimitiveKeysGen()
        {
            Scope = "Context";
        }

        public PrimitiveKeysOptions Options { get; set; }

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();
            var moduleName = webConfig.ScriptNamespace;
            var outputDirPath = webConfig.TypeScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.AllValueCollections.Where(x => x.Uses(this)).ToArray();
            if (!items.Any())
                return;

            PerformWrite(Path.Combine(outputDirPath, "Primitives.generated.ts"), () =>
            {
                OTsNamespace(moduleName, () =>
                {
                    foreach (var item in items)
                    {
                        O();
                        OTsClass(name: item.KeysContainerName, export: true,
                            hasconstructor: false,
                            content: () => GeneratePrimitiveDefinition(moduleName, item));
                    }
                });
            });
        }

        void GeneratePrimitiveDefinition(string moduleName, MojValueSetContainer config)
        {
            // ES6:
            // const myConst = 123;
            // const myConstObj = { "key": "value" };
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/const

            // ES5:
            // Object.defineProperty (mymodule, 'myConstObj', { value : 5, writable: false });

            Options = config.GetGeneratorOptions<PrimitiveKeysOptions>() ?? new PrimitiveKeysOptions();

            var typeName = config.KeysContainerName;

            if (Options.IsNamedValueEnabled)
            {
                O();
                MojValueSet item;
                var props = config.Items.Where(x => !x.IsNull).ToList();
                for (int i = 0; i < props.Count; i++)
                {
                    //if (i > 0) O();

                    item = props[i];

                    // Summary of member                
                    if (item.Has("DisplayValue"))
                        O("// DisplayValue: " + item.Get("DisplayValue").Value);

                    if (item.Has("DisplayName"))
                        O("// DisplayName: " + item.Get("DisplayName").Value);

                    if (item.Has("Display"))
                        O("// Display: " + item.Get("Display").Value);

                    if (item.Has("Description"))
                        O("// Description: " + item.Get("Description").Value);

                    // Public static member
                    var name = item.Get(config.NamePropName);
                    var val = item.Get(config.ValuePropName);
                    O("public static {0} = {1};", name.Value, Moj.JS(val.Value));
                }
            }
        }
    }
}
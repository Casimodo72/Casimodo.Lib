using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public partial class JsPrimitiveKeysGen : DataLayerGenerator
    {
        public JsPrimitiveKeysGen()
        {
            Scope = "Context";
        }

        public PrimitiveKeysOptions Options { get; set; }

        protected override void GenerateCore()
        {
            var ctx = App.Get<DataLayerConfig>();
            var moduleName = ctx.ScriptNamespace;
            var outputDirPath = ctx.JavaScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.AllValueCollections.Where(x => x.Uses(this)).ToArray();
            if (!items.Any())
                return;

            PerformWrite(Path.Combine(outputDirPath, "primitives.generated.js"), () =>
            {
                OJsNamespace(moduleName, () =>
                {
                    foreach (var item in items)
                    {
                        O();
                        OJsClass(item.KeysContainerName, true, () => GeneratePrimitiveDefinition(moduleName, item));
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
                    if (i > 0) O();

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

                    O("this.{0} = {1};", name.Value, MojenUtils.ToJsValue(val.Value));
                }
            }

            var type = config.TargetType;
            foreach (var mapping in config.Mappings)
            {
                GenerateMapping(config, mapping);
            }
        }

        void GenerateMapping(MojValueSetContainer config, MojValueSetMapping mapping)
        {
            var type = config.TargetType;

            if (config.Items.Count == 0)
                return;

            var first = config.Items.First();

            var toPropName = mapping.To;
            var toProp = type.FindProp(toPropName);
            var toPropType = toProp.Type.NameNormalized;

            var keyProps = mapping.From;
            var keyName = keyProps.Join("And");

            bool isToNamedValue = Options.IsNamedValueEnabled && toPropName == config.ValuePropName;
            //bool isFromNamedValue = Options.IsNamedValueEnabled && mapping.From.Count == 1 && keyName == config.ValuePropName;

            var dictionary = $"_{FirstCharToLower(keyName)}2{toPropName}";

            var keyType = keyProps.Select(x => type.FindProp(x).Type.NameNormalized).Join(", ");
            var keyTemplate = keyProps.Select((x, i) => $"{{{i}}}").Join(", ");

            if (mapping.From.Count > 1)
            {
                keyType = $"Tuple<{keyType}>";
                keyTemplate = $"Tuple.Create({keyTemplate})";
            }

            var dictionaryType = $"Dictionary<{keyType}, {toPropType}>";

            // Mapping dictionary
            O();
            OB($"var {dictionary} =");
            string key;
            string value;
            foreach (MojValueSet item in config.Items)
            {
                key = string.Format(keyTemplate,
                    mapping.From.Select(x =>
                        MojenUtils.ToJsValue(item.Get(x).Value, parse: true))
                   .ToArray());

                if (isToNamedValue)
                    value = "this." + item.Get(config.NamePropName).Value;
                else
                    value = MojenUtils.ToJsValue(item.Get(toPropName).Value, parse: true);

                O($"{key}: {value},");
            }
            End(";");

            // Mapping function
            O();
            key = keyProps.Select(x => FirstCharToLower(x)).Join(", ");

            OB($"this.get{toPropName}By{keyName} = function ({key})");

            key = string.Format(keyTemplate,
                keyProps.Select(x => FirstCharToLower(x))
                .ToArray());

            O($"if (typeof {key} === 'undefined' || {key} === null) return null;");

            O($"return {dictionary}['' + {key}] || null;");

            End(";");
        }
    }
}
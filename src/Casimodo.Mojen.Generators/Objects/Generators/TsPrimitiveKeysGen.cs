using System;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public partial class TsPrimitiveKeysGen : TsGenBase
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
                            content: () => GeneratePrimitiveDefinition(item));
                    }
                });
            });
        }

        void GeneratePrimitiveDefinition(MojValueSetContainer config)
        {
            Options = config.GetGeneratorOptions<PrimitiveKeysOptions>() ?? new PrimitiveKeysOptions();

            var typeName = config.KeysContainerName;

            if (Options.IsNamedValueEnabled)
            {
                O();
                MojValueSet valueSet;
                var valueSetList = config.Items.Where(x => !x.IsNull).ToList();
                for (int i = 0; i < valueSetList.Count; i++)
                {
                    valueSet = valueSetList[i];

                    // Summary of member                
                    if (valueSet.Has("DisplayValue"))
                        O("// DisplayValue: " + valueSet.Get("DisplayValue").Value);

                    if (valueSet.Has("DisplayName"))
                        O("// DisplayName: " + valueSet.Get("DisplayName").Value);

                    if (valueSet.Has("Display"))
                        O("// Display: " + valueSet.Get("Display").Value);

                    if (valueSet.Has("Description"))
                        O("// Description: " + valueSet.Get("Description").Value);

                    // Public static member
                    var name = valueSet.Get(config.NamePropName);
                    var val = valueSet.Get(config.ValuePropName);
                    O("public static {0} = {1};", name.Value, Moj.JS(val.Value));
                }
            }

            var type = config.TypeConfig;
            foreach (var mapping in config.Mappings)
            {
                GenerateMapping(config, mapping);
            }
        }

        void GenerateMapping(MojValueSetContainer config, MojValueSetMapping mapping)
        {
            var type = config.TypeConfig;

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
                throw new NotSupportedException("Multiple 'from' mappings are not suported yet for TypeScript.");
                //keyType = $"Tuple<{keyType}>";
                //keyTemplate = $"Tuple.Create({keyTemplate})";
            }

            // Mapping dictionary
            O();
            OB($"private static {dictionary} =");
            string key;
            string value;
            foreach (MojValueSet valueSet in config.Items)
            {
                key = string.Format(keyTemplate,
                    mapping.From.Select(x =>
                        Moj.JS(valueSet.Get(x).Value, parse: true))
                   .ToArray());

                if (isToNamedValue)
                    value = config.KeysContainerName + "." + valueSet.Get(config.NamePropName).Value;
                else
                    value = Moj.JS(valueSet.Get(toPropName).Value, parse: true);

                O($"{key}: {value},");
            }
            End(";");

            // Mapping function
            O();
            key = keyProps.Select(x => FirstCharToLower(x)).Join(", ");

            OB($"public static get{toPropName}By{keyName}({key})");

            key = string.Format(keyTemplate,
                keyProps.Select(x => FirstCharToLower(x))
                .ToArray());

            O($"if (typeof {key} === 'undefined' || {key} === null) return null;");

            O($"return {config.KeysContainerName}.{dictionary}['' + {key}] || null;");

            End();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class PrimitiveKeysOptions
    {
        public bool IsNamedValueEnabled { get; set; } = true;
    }

    public partial class PrimitiveKeysGen : DataLayerGenerator
    {
        public PrimitiveKeysGen()
        {
            Scope = "Context";
        }

        public PrimitiveKeysOptions Options { get; set; }

        protected override void GenerateCore()
        {
            foreach (var item in App.AllValueCollections.Where(x => x.Uses(this)))
            {
                PerformWrite(Path.Combine(App.Get<DataLayerConfig>().DataPrimitiveDirPath, item.KeysContainerName + ".generated.cs"), () =>
                {
                    OComment($"Generator: {nameof(PrimitiveKeysGen)}");
                    GeneratePrimitiveDefinition(item);
                });
            }
        }

        void GeneratePrimitiveDefinition(MojValueSetContainer config)
        {
            Options = config.GetGeneratorOptions<PrimitiveKeysOptions>() ?? new PrimitiveKeysOptions();

            OUsing("System", "System.Collections.Generic", "System.Linq");

            ONamespace(config.Namespace);

            // Summary of class
            if (config.Description != null)
                OSummary(config.Description);

            O("public static partial class {0}", config.KeysContainerName);
            Begin();

            if (Options.IsNamedValueEnabled)
            {
                var descriptions = new List<string>();
                foreach (MojValueSet item in config.Items)
                {
                    if (item.IsNull)
                        continue;

                    // Summary of member
                    descriptions.Clear();
                    //AddDescription(item, "DisplayValue", descriptions);
                    //AddDescription(item, "Display", descriptions);                    
                    if (item.Description != null)
                        descriptions.Add(item.Description);
                    OSummary(descriptions);

                    // Public static member
                    var name = item.Get(config.NamePropName);
                    var val = item.Get(config.ValuePropName);

                    O(string.Format("public {0} {1} {2} = {3};",
                        GetValueTypeModifier(config.ValueType),
                        Moj.ToCsType(config.ValueType),
                        name.Value,
                        Moj.CS(val.Value, parse: true)
                    ));
                }

                foreach (MojValueSetAggregate agg in config.Aggregates)
                {
                    OSummary(agg.Description);
                    Oo(string.Format("public static readonly {0}[] {1} = new {0}[] {{ ",
                        Moj.ToCsType(config.ValueType),
                        agg.Name
                    ));

                    foreach (var item in agg)
                        o(item + ", ");

                    o("};" + Environment.NewLine);
                }
            }

            var type = config.TypeConfig;
            foreach (var mapping in config.Mappings)
            {
                GenerateMapping(config, mapping);
            }

            End();
            End();
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
            bool isFromNamedValue = Options.IsNamedValueEnabled && mapping.From.Count == 1 && keyName == config.ValuePropName;

            var dictionary = $"_{FirstCharToLower(keyName)}2{toPropName}";

            var keyType = keyProps.Select(x => type.FindProp(x).Type.NameNormalized).Join(", ");
            var keyTemplate = keyProps.Select((x, i) => $"{{{i}}}").Join(", ");

            if (mapping.From.Count > 1)
            {
                // KABU TODO: REMOVE? We don't use multiple key props anywhere.
                keyType = $"Tuple<{keyType}>";
                keyTemplate = $"Tuple.Create({keyTemplate})";
            }

            var dictionaryType = $"Dictionary<{keyType}, {toPropType}>";

            O();
            var key =
                keyProps.Select(x =>
                    type.FindProp(x).Type.NameNormalized + " " + FirstCharToLower(x))
                .Join(", ");

            O($"public static {toPropType} Get{toPropName}By{keyName}({key})");
            Begin();

            key = string.Format(keyTemplate,
                keyProps.Select(x => FirstCharToLower(x))
                .ToArray());

            O($"return {dictionary}[{key}];");

            End();

            // Static dictionary
            O($"static readonly {dictionaryType} {dictionary} = new {dictionaryType}");
            Begin();
            string value;
            foreach (MojValueSet item in config.Items)
            {
                if (isFromNamedValue)
                    key = (string)item.Get(config.NamePropName).Value;
                else
                    key = string.Format(keyTemplate,
                        mapping.From.Select(x =>
                            Moj.CS(item.Get(x).Value, parse: true))
                       .ToArray());

                if (isToNamedValue)
                    value = (string)item.Get(config.NamePropName).Value;
                else
                    value = Moj.CS(item.Get(toPropName).Value, parse: true);

                O($"[{key}] = {value},");
            }
            End(";");
        }

        void GenerateMapping2(MojValueSetContainer config, MojValueSetMapping mapping)
        {
            var type = config.TypeConfig;

            var first = config.Items.First();

            var fromPropName = mapping.From.First();
            var fromProp = type.FindProp(fromPropName);
            var fromType = fromProp.Type.NameNormalized;

            var toProp = type.FindProp(mapping.To);
            var toType = toProp.Type.NameNormalized;

            var dictionary = $"_{FirstCharToLower(fromPropName)}To{mapping.To}";
            var dictionaryType = $"Dictionary<{fromType}, {toType}>";

            O();
            O("public static {0} Get{1}By{2}({3} by)", toType, mapping.To, fromPropName, fromType);
            Begin();
            O($"return {dictionary}[by];");
            End();

            O($"static readonly {dictionaryType} {dictionary} = new {dictionaryType}");
            Begin();
            foreach (MojValueSet item in config.Items)
            {
                var fromVal = item.Get(fromPropName);
                var toVal = item.Get(mapping.To);

                O("[{0}] = {1},",
                    Moj.CS(fromVal.Value, parse: true),
                    Moj.CS(toVal.Value, parse: true));
            }
            End(";");
        }

        void AddDescription(MojValueSet item, string name, List<string> descriptions)
        {
            if (!item.Has(name))
                return;

            var text = item.Get(name).Value as string;
            if (string.IsNullOrWhiteSpace(text))
                return;

            descriptions.Add(text.CollapseWhitespace());
        }

        string GetValueTypeModifier(Type type)
        {
            if (type == typeof(Guid) || type == typeof(Guid?))
                return "static readonly";

            return "const";
        }
    }
}
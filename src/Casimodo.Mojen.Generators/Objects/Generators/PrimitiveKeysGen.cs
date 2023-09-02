using System.IO;

namespace Casimodo.Mojen
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
            foreach (var valueSetContainer in App.AllValueCollections.Where(x => x.Uses(this)))
            {
                PerformWrite(Path.Combine(DataConfig.DataPrimitiveDirPath, valueSetContainer.KeysContainerName + ".generated.cs"), () =>
                {
                    OComment($"Generator: {nameof(PrimitiveKeysGen)}");
                    GeneratePrimitiveDefinition(valueSetContainer);
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

            O($"public static partial class {config.KeysContainerName}");
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
                keyType = $"({keyType})";
                keyTemplate = $"({keyTemplate})";
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
            foreach (MojValueSet valueSet in config.Items)
            {
                if (isFromNamedValue)
                    key = (string)valueSet.Get(config.NamePropName).Value;
                else
                    key = string.Format(keyTemplate,
                        mapping.From.Select(x =>
                            Moj.CS(valueSet.Get(x).Value, parse: true))
                       .ToArray());

                if (isToNamedValue)
                    value = (string)valueSet.Get(config.NamePropName).Value;
                else
                    value = Moj.CS(valueSet.Get(toPropName).Value, parse: true);

                O($"[{key}] = {value},");
            }
            End(";");
        }

        void GenerateMapping2(MojValueSetContainer config, MojValueSetMapping mapping)
        {
            var type = config.TypeConfig;

            var fromPropName = mapping.From.First();
            var fromProp = type.FindProp(fromPropName);
            var fromType = fromProp.Type.NameNormalized;

            var toProp = type.FindProp(mapping.To);
            var toType = toProp.Type.NameNormalized;

            var dictionary = $"_{FirstCharToLower(fromPropName)}To{mapping.To}";
            var dictionaryType = $"Dictionary<{fromType}, {toType}>";

            O();
            OFormat("public static {0} Get{1}By{2}({3} by)", toType, mapping.To, fromPropName, fromType);
            Begin();
            O($"return {dictionary}[by];");
            End();

            O($"static readonly {dictionaryType} {dictionary} = new {dictionaryType}");
            Begin();
            foreach (MojValueSet valueSet in config.Items)
            {
                var fromVal = valueSet.Get(fromPropName);
                var toVal = valueSet.Get(mapping.To);

                OFormat("[{0}] = {1},",
                    Moj.CS(fromVal.Value, parse: true),
                    Moj.CS(toVal.Value, parse: true));
            }
            End(";");
        }

        void AddDescription(MojValueSet valueSet, string name, List<string> descriptions)
        {
            if (!valueSet.Has(name))
                return;

            var text = valueSet.Get(name).Value as string;
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
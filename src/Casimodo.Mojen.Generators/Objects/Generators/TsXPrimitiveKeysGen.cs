using System.IO;
#nullable enable

namespace Casimodo.Mojen;

public class TsXPrimitiveKeysGenOptions
{
    public required string[] TypeNames { get; set; }
    public string? FileName { get; set; }
    public required string[] OutputDirPaths { get; set; }
}

/// <summary>
/// Uses TypeScriptDataDirPath as output dir path by default.
/// Override with options.OutputDirPath.
/// </summary>
public partial class TsXPrimitiveKeysGen : TsGenBase
{
    readonly TsXPrimitiveKeysGenOptions _options;

    public TsXPrimitiveKeysGen(TsXPrimitiveKeysGenOptions options)
    {
        Guard.ArgNotNull(options, nameof(options));

        _options = options;
        Scope = "Context";
    }

    // public PrimitiveKeysOptions Options { get; set; }

    protected override void GenerateCore()
    {
        var webConfig = App.Get<WebDataLayerConfig>();
        var moduleName = webConfig.ScriptNamespace;

        var outputDirPaths = new List<string>();
        if (_options.OutputDirPaths?.Length > 0)
        {
            outputDirPaths.AddRange(_options.OutputDirPaths);
        }
        else
        {
            outputDirPaths.Add(webConfig.TypeScriptDataDirPath);
        }

        foreach (var outputDirPath in outputDirPaths)
        {
            var items = App.AllValueCollections
                .Where(x => _options.TypeNames.Contains(x.TypeConfig.Name))
                .ToList();

            if (!items.Any())
                return;

            var fileName = (_options.FileName ?? "keys.generated") + ".ts";

            PerformWrite(Path.Combine(outputDirPath, fileName), () =>
            {
                foreach (var item in items)
                {
                    O();
                    OTsClass(name: item.KeysContainerName, export: true,
                        hasconstructor: false,
                        content: () => GeneratePrimitiveDefinition(moduleName, item));
                }
            });
        }
    }

    void GeneratePrimitiveDefinition(string moduleName, MojValueSetContainer config)
    {
        var typeName = config.KeysContainerName;


        var valueSets = config.Items.Where(x => !x.IsNull).ToList();
        for (int i = 0; i < valueSets.Count; i++)
        {
            MojValueSet valueSet = valueSets[i];

            // TODO: Do we need a summary?
#if (false)
                // Summary of member
                if (item.Has("DisplayValue"))
                    O("// DisplayValue: " + item.Get("DisplayValue").Value);

                if (item.Has("DisplayName"))
                    O("// DisplayName: " + item.Get("DisplayName").Value);

                if (item.Has("Display"))
                    O("// Display: " + item.Get("Display").Value);

                if (item.Has("Description"))
                    O("// Description: " + item.Get("Description").Value);
#endif

            // Public static member
            var name = valueSet.Get(config.NamePropName);
            var val = valueSet.Get(config.ValuePropName);
            O($"static readonly {name.Value} = {Moj.JS(val.Value)};");
        }


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

        bool isToNamedValue = toPropName == config.ValuePropName;
        //bool isFromNamedValue = mapping.From.Count == 1 && keyName == config.ValuePropName;

        var dictionary = $"#{FirstCharToLower(keyName)}2{toPropName}";

        var keyType = keyProps.Select(x => type.FindProp(x).Type.NameNormalized).Join(", ");
        var keyTemplate = keyProps.Select((x, i) => $"{{{i}}}").Join(", ");

        if (mapping.From.Count > 1)
        {
            throw new NotSupportedException("Multiple 'from' mappings are not suported yet for TypeScript.");
        }

        // Mapping dictionary
        O();
        OB($"static readonly {dictionary} =");
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

        // TODO: Type of key and return type.
        OB($"static get{toPropName}By{keyName}({key}: string)");

        key = string.Format(keyTemplate,
            keyProps.Select(x => FirstCharToLower(x))
            .ToArray());

        O($"return ({config.KeysContainerName}.{dictionary} as any)[{key}];");

        End();
    }
}
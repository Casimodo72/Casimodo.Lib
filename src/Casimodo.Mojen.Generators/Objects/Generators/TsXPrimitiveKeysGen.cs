using System.IO;

namespace Casimodo.Lib.Mojen
{
    public class TsXPrimitiveKeysGenOptions
    {
        public string OutputDirPath { get; set; }
        public string FileName { get; set; }
        public string[] IncludeTypes { get; set; }
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

        public PrimitiveKeysOptions Options { get; set; }

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();
            var moduleName = webConfig.ScriptNamespace;

            var outputDirPath = _options.OutputDirPath ?? webConfig.TypeScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.AllValueCollections
                .Where(x => _options.IncludeTypes.Contains(x.TypeConfig.Name))
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
                var props = config.Items.Where(x => !x.IsNull).ToList();
                for (int i = 0; i < props.Count; i++)
                {
                    //if (i > 0) O();

                    MojValueSet valueSet = props[i];

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
                    O($"public static {name.Value} = {Moj.JS(val.Value)};");
                }
            }
        }
    }
}
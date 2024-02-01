using System.IO;
#nullable enable

namespace Casimodo.Mojen
{
    public class TsXQueryableFunctionsGenOptions
    {
        public required string[] TypeNames { get; set; }
        public string? FileName { get; set; }
        public required string[] OutputDirPaths { get; set; }
    }

    /// <summary>
    /// Uses TypeScriptDataDirPath as output dir path by default.
    /// Override with options.OutputDirPath.
    /// </summary>
    public partial class TsXQueryableFunctionsGen : TsGenBase
    {
        readonly TsXQueryableFunctionsGenOptions _options;

        public TsXQueryableFunctionsGen(TsXQueryableFunctionsGenOptions options)
        {
            Guard.ArgNotNull(options, nameof(options));

            _options = options;
            Scope = "Context";
        }

        record Item(MojType Type, MojProp[] LocalProps);

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();

            var outputDirPaths = new List<string>();
            if (_options.OutputDirPaths?.Length > 0)
            {
                outputDirPaths.AddRange(_options.OutputDirPaths);
            }
            else
            {
                outputDirPaths.Add(webConfig.TypeScriptDataDirPath);
            }

            var IncludedTypeNames = _options.TypeNames?.ToArray() ?? Array.Empty<string>();

            var items = App.GetTypes(MojTypeKind.Entity)
                .Where(x => IncludedTypeNames.Contains(x.Name))
                .Select(x => new Item(x, x.GetLocalProps().Where(prop => !prop.Type.IsMojType).ToArray()))
                .Where(x => x.LocalProps.Length > 0)
                .ToList();

            foreach (var outputDirPath in outputDirPaths)
            {
                var fileName = _options.FileName ?? "queryable";
                fileName += ".ts";

                PerformWrite(Path.Combine(outputDirPath, fileName), () =>
                {
                    O("import { ODataCoreQueryBuilder } from \"@lib/data-utils\";");

                    Oo("import {");
                    foreach (var item in items)
                    {
                        o($"I{item.Type.Name},");
                    }
                    oO("} from \"@lib/data\";");
                    O();

                    foreach (var item in items)
                    {
                        GenerateQueryableFunctions(item);
                    }
                });
            }
        }

        void GenerateQueryableFunctions(Item item)
        {
            var type = item.Type;
            var localProps = item.LocalProps.Where(x => !x.IsExcludedFromDb).ToArray();

            OB($"export function selectFrom{type.Name}(q: ODataCoreQueryBuilder<I{type.Name}>)");
            Oo("q.select(\"");
            foreach (var (prop, index) in localProps.WithIndex())
            {
                o(prop.Name);

                if (localProps.HasNext(index))
                    o(",");
            }
            oO("\");");
            End();
            O();
        }
    }
}
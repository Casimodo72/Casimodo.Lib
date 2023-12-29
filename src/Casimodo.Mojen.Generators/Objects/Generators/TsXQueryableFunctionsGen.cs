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

            var types = App.GetTypes(MojTypeKind.Entity)
                .Where(x => IncludedTypeNames.Contains(x.Name))
                .ToList();

            foreach (var outputDirPath in outputDirPaths)
            {
                var fileName = _options.FileName ?? "queryable";
                fileName += ".ts";

                PerformWrite(Path.Combine(outputDirPath, fileName), () =>
                {
                    foreach (var type in types)
                    {
                        GenerateQueryableFunctions(type);
                        O();
                    }
                });
            }
        }

        void GenerateQueryableFunctions(MojType type)
        {
            O("import { AbstractODataQueryBuilder } from \"@lib/data-utils\";");
            O();

            OB($"export function selectOwnPropsFrom{type.Name}(q: AbstractODataQueryBuilder<any>)");
            Oo("q.select(\"");
            var localProps = type.GetLocalProps().ToArray();
            foreach (var (prop, index) in localProps.WithIndex())
            {
                if (prop.Type.IsMojType)
                {
                    continue;
                }

                o(prop.Name);

                if (localProps.HasNext(index))
                {
                    o(",");
                }
            }
            oO("\");");
            End();
        }
    }
}
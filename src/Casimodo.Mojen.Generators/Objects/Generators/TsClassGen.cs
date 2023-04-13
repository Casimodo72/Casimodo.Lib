using System.IO;

namespace Casimodo.Mojen
{
    public class TsClassGen : TsGenBase
    {
        public TsClassGen()
        {
            Scope = "App";
        }

        public WebDataLayerConfig WebConfig { get; set; }

        class MyTypeHierarchyComparer : IComparer<MojType>
        {
            public int Compare(MojType x, MojType y)
            {
                if (x.BaseClass?.Name == y.BaseClass?.Name)
                {
                    var comp = x.Name.CompareTo(y.Name);
                    return comp;
                }

                var baseClass = x.BaseClass;
                while (baseClass != null)
                {
                    if (baseClass.Name == y.Name)
                        return 1;

                    baseClass = baseClass.BaseClass;
                }

                return x.Name.CompareTo(y.Name);
            }
        }

        static readonly MyTypeHierarchyComparer HierarchyComparer = new();

        protected override void GenerateCore()
        {
            WebConfig = App.Get<WebDataLayerConfig>();
            var outputDirPath = WebConfig.TypeScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
                .Where(x => !x.WasGenerated)
                .Where(x => !x.IsTenant)
                .OrderBy(x => x, HierarchyComparer)
                .ToArray();

            PerformWrite(Path.Combine(outputDirPath, "DataTypes.generated.ts"), () =>
            {
                OTsNamespace(WebConfig.ScriptNamespace, () =>
                {
                    var gen = new TsXClassGen();
                    gen.Options.GenerateInterfaces = true;
                    gen.Options.PrefixInterfaces = true;
                    gen.Options.UseDefaultValues = true;
                    gen.WebConfig = WebConfig;
                    gen.Use(Writer);
                    gen.Push();
                    foreach (var item in items)
                    {
                        gen.Generate(item, false);
                        O();
                    }
                    gen.Pop();
                });
            });
        }
    }
}
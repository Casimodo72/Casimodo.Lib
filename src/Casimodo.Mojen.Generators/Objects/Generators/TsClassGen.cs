﻿using System.IO;

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
                    return 0;

                var t = x.BaseClass;
                while (t != null)
                {
                    if (t.Name == y.Name)
                        return 1;

                    t = t.BaseClass;
                }

                return -1;
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
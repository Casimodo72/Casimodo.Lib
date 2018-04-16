using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class TsClassGen : DataLayerGenerator
    {
        public TsClassGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();
            var outputDirPath = webConfig.TypeScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
                .Where(x => !x.WasGenerated)
                .Where(x => !x.IsAbstract && !x.IsTenant).ToArray();

            PerformWrite(Path.Combine(outputDirPath, "data.generated.ts"), () =>
            {
                OB("module {0}", webConfig.ScriptNamespace);
                O("\"use strict\";");
                O();

                foreach (var item in items)
                {
                    Generate(item);
                    O();
                }

                End();
            });
        }

        public void Generate(MojType item)
        {
            var tenantKey = item.FindTenantKey();

            // NOTE: We use the Name not the ClassName here. Otherwise
            //   we would create lots of TS classes ending with "Entity",
            //   which would be ugly.
            OB("export class {0}", item.Name);
            O();

            // Default constructor.
            O("constructor() { }");

            // Properties
            O();
            MojProp prop;
            var props = item.GetProps(custom: false)
                // Exclude hidden EF navigation collection props.
                .Where(x => !x.IsHiddenCollectionNavigationProp)
                .ToList();
            for (int i = 0; i < props.Count; i++)
            {
                prop = props[i];

                if (prop == tenantKey)
                    // Don't expose tenant information.
                    continue;

                if (i > 0)
                    O();

                if (prop.IsKey)
                    O("// [Key]");

                if (prop.IsExcludedFromDb)
                    O("// [NotMapped]");

                if (prop.DisplayLabel != prop.Name)
                    O("// Display: '" + prop.DisplayLabel + "'");

                foreach (var description in prop.Summary.Descriptions)
                    O("// Description: " + description);

                if (prop.Type.IsCollection)
                {
                    O("{0} = [];", prop.Name);
                }
                else
                {
                    string defaultValue = GetJsDefaultValue(prop);
                    O($"{prop.Name} = {defaultValue};");
                }
            }

            End();
        }
    }
}
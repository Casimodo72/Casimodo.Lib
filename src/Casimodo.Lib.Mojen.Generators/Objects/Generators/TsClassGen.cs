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

        public WebDataLayerConfig WebConfig { get; set; }

        protected override void GenerateCore()
        {
            WebConfig = App.Get<WebDataLayerConfig>();
            var outputDirPath = WebConfig.TypeScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
                .Where(x => !x.WasGenerated)
                .Where(x => !x.IsAbstract && !x.IsTenant).ToArray();

            PerformWrite(Path.Combine(outputDirPath, "DataTypes.generated.ts"), () =>
            {
                OTsNamespace(WebConfig.ScriptNamespace, () =>
                {
                    foreach (var item in items)
                    {
                        Generate(item);
                        O();
                    }
                });
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
            OB("constructor()");
            // TODO: Find a way to emit this only when used in the context of OData.
            O($"this['@odata.type'] = '#{WebConfig.ODataNamespace}.{item.ClassName}';");
            End();

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
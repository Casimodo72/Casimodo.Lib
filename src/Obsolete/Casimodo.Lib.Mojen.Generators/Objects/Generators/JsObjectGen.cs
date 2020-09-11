using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class JsObjectGen : DataLayerGenerator
    {
        static readonly List<string> _commonProps = new List<string>(new string[]
        {
            "Id", "Created", "Modified", "Deleted", "IsReadOnly", "IsNotDeletable"
        });

        public JsObjectGen()
        {
            Scope = "App";
        }

        WebDataLayerConfig WebConfig;

        public bool AreCommentsEnabled { get; set; }

        protected override void GenerateCore()
        {
            WebConfig = App.Get<WebDataLayerConfig>();
            var moduleName = WebConfig.ScriptNamespace;
            var outputDirPath = WebConfig.JavaScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
                .Where(x => !x.WasGenerated)
                .Where(x => !x.IsAbstract && !x.IsTenant).ToArray();

            PerformWrite(Path.Combine(outputDirPath, "data.generated.js"), () =>
            {
                OJsNamespace(moduleName, () =>
                {
                    foreach (var item in items)
                    {
                        Generate(moduleName, item);
                    }
                });
            });
        }

        public void Generate(string moduleName, MojType item)
        {
            // NOTE: We use the Name not the ClassName here. Otherwise
            //   we would create lots of TS classes ending with "Entity",
            //   which would be ugly.
            O();
            OJsClass(ns: moduleName, name: item.Name,
                 constructor: () =>
                 {
                     // Properties

                     // OData type property.
                     O("this['@odata.type'] = '#{0}.{1}';", WebConfig.ODataNamespace, item.ClassName);

                     MojProp prop;
                     var props = item.GetProps(custom: false)
                         // Exclude hidden EF navigation collection props.
                         .Where(x => !x.IsHiddenCollectionNavigationProp)
                         .ToList();
                     for (int i = 0; i < props.Count; i++)
                     {
                         prop = props[i];

                         if (prop.IsTenantKey)
                             // Don't expose tenant information.
                             continue;

                         if (prop.IsODataDynamicPropsContainer)
                             // OData open type: skip dynamic properties container property.
                             continue;

                         //if (i > 0) O();

                         if (AreCommentsEnabled && !IsCommonProp(prop))
                         {
                             if (prop.DisplayLabel != prop.Name)
                                 O("// Display: '" + prop.DisplayLabel + "'");

                             foreach (var description in prop.Summary.Descriptions)
                                 O("// Description: " + description);

                             O("// Type: {0}", prop.Type.Name);
                         }

                         if (prop.Type.IsDictionary)
                         {
                             O("this.{0} = {{}};", prop.Name);
                         }
                         else if (prop.Type.IsCollection && !prop.Type.IsPrimitiveArray)
                         {
                             O("this.{0} = [];", prop.Name);
                         }
                         else
                         {
                             O("this.{0} = {1};", prop.Name, GetJsDefaultValue(prop));
                         }
                     }
                 });
        }

        bool IsCommonProp(MojProp prop)
        {
            for (int i = 0; i < _commonProps.Count; i++)
                if (prop.Name.StartsWith(_commonProps[i]))
                    return true;

            return false;
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class ComplexTypeGen : ClassGen
    {
        public ComplexTypeGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            string outputDirPath = App.Get<DataLayerConfig>().ComplexTypeDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            foreach (var type in App.GetTypes(MojTypeKind.Complex).Where(x => !x.WasGenerated))
            {
                PerformWrite(Path.Combine(outputDirPath, type.ClassName + ".generated.cs"),
                    () => Generate(type));
            }
        }

        public void Generate(MojType type)
        {
            OUsing(BuildNamespaces(type));
            ONamespace(type.Namespace);

            // Class declaration
            GenerateClassHead(type);

            // Static constructor
#if (false)
            O("static {0}()", type.ClassName);
            B();
            E();
            O();
#endif
            // Constructor
            if (type.HasCreateOnInitProps)
            {
                O("public {0}()", type.ClassName);
                Begin();
                foreach (var prop in type.GetCreateOnInitProps())
                {
                    O("{0} = new {1};", prop.Name, MojenUtils.GetDefaultConstructor(prop.Type));
                }
                End();
            }

            // Properties
            O();
            {
                MojProp prop;
                var props = type.GetLocalProps(custom: false).ToList();
                for (int i = 0; i < props.Count; i++)
                {
                    prop = props[i];

                    if (prop.IsODataDynamicPropsContainer)
                        // OData dynamic properties container is handled at a later stage.
                        continue;

                    if (i > 0)
                        O();

                    OSummary(prop.Summary);

                    if (!type.NoDataContract)
                        O("[DataMember]");

                    // Attributes
                    foreach (var attr in prop.Attrs
                        .OrderBy(x => x.Position)
                        .ThenBy(x => x.Name))
                    {
                        O(BuildAttr(attr));
                    }

                    ORequiredAttribute(prop);
                    ODefaultValueAttribute(prop, "OnEdit", null);

                    // Declare property
                    GenerateProp(type, prop);
                }
            }

            GenerateODataOpenTypePropsContainer(type);

            GenerateTypeComparisons(type);

            GenerateAssignFromMethod(type);

            GenerateNamedAssignFromMethods(type);

            End();
            End();
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class ODataConfigGen : WebPartGenerator
    {
        public ODataConfigGen()
        {
            Scope = "App";
        }

        public WebODataBuildConfig ODataConfig { get; set; }

        protected override void GenerateCore()
        {
            var types = App.GetTypes().Where(x => x.Uses(this)).ToArray();
            if (!types.Any())
                return;

            ODataConfig = App.Get<WebODataBuildConfig>();

            string filePath = Path.Combine(WebConfig.WebStartupDirPath, "ODataConfig.generated.cs");
            PerformWrite(filePath, () => GenerateODataConfig(types));
        }

        void GenerateODataConfig(IEnumerable<MojType> types)
        {
            OUsing(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Web",
                "System.Web.Http",
                "System.Web.OData.Builder",
                GetAllDataNamespaces());

            ONamespace(WebConfig.WebAppConfigNamespace);

            O("public static partial class ODataConfig");
            Begin();

            // Main OData.
            O("public static void ConfigureMain(HttpConfiguration config, ODataModelBuilder builder)");
            Begin();

            O("FunctionConfiguration func;");
            // Disable warning in case no actions are used.
            O("#pragma warning disable CS0168 // Variable is declared but never used");
            O("ActionConfiguration action;");
            O("#pragma warning restore CS0168 // Variable is declared but never used");
            O();

            var names = new HashSet<string>();
            foreach (MojType type in types)
            {
                // Skip duplicates.
                if (names.Contains(type.PluralName))
                    continue;

                names.Add(type.PluralName);

                string item = type.VName;

                // NOTE: We are defining OData metadata not for models, but for entities.
                //   BUT we use the model's properties for definition of property constraints.
                //   This means if a property is required at DB level, it could be optional
                //   at OData level. This is intended.
                string typeName = type.IsModel() ? type.RequiredStore.ClassName : type.ClassName;

                O($"// {typeName}");

                if (type.Kind == MojTypeKind.Complex)
                {
                    // Add ComplexType
                    O($"builder.ComplexType<{typeName}>();");
                    OPropertyConstraints(type);
                }
                else if (type.Kind == MojTypeKind.Enum)
                {
                    // Add EnumType
                    O($"builder.EnumType<{typeName}>();");
                }
                else
                {
                    // Add EntitySet. NOTE: this can be either an entity type or a model type.                    

                    O("{");
                    Push();

                    O($"builder.EntitySet<{typeName}>(\"{type.PluralName}\");");

                    O($"var {item} = builder.EntityType<{typeName}>();");

                    OPropertyConstraints(type);

                    // Entity properties which are not in the DB and are
                    // explicitely configured to be added to OData.
                    foreach (var prop in type.GetProps())
                    {
                        if (prop.IsExcludedFromDb && prop.IsExplicitelyIncludedInOData)
                        {
                            O("builder.StructuralTypes.First(t => t.ClrType == typeof({0})).AddProperty(typeof({0}).GetProperty(\"{1}\"));",
                                typeName, prop.Name);
                        }
                    }

                    // Default query function
                    O($"{item}.Collection");
                    O($"    .Function(\"{ODataConfig.Query}\")");
                    O($"    .ReturnsCollectionFromEntitySet<{typeName}>(\"{type.PluralName}\");");

                    // Distinct by property query function
                    O();
                    O($"func = {item}.Collection.Function(\"{ODataConfig.QueryDistinct}\");");
                    O($"func.Parameter<string>(\"On\");");
                    O($"func.ReturnsCollectionFromEntitySet<{typeName}>(\"{type.PluralName}\");");

                    ONextSequenceValueFunctions(type);

                    foreach (var editorOfViewGroup in App.GetItems<MojViewConfig>()
                        .Where(x =>
                            x.Group != null &&
                            x.Kind.Roles.HasFlag(MojViewRole.Editor) &&
                            x.TypeConfig.StoreOrSelf == type.StoreOrSelf))
                    {
                        // NOTE: For now all view groups will share the default OData creation action.

                        // Add OData Update action
                        O();
                        O($"builder.EntityType<{typeName}>().Action(\"{editorOfViewGroup.GetODataUpdateActionName()}\")");
                        O($"    .Parameter<{typeName}>(\"model\");");
                    }

                    Pop();
                    O("}");
                }

                O();
            }
            End();

#if (false)
            // OData for entity lookup.
            O();
            O("public static void ConfigureLookup(HttpConfiguration config, ODataModelBuilder builder)");
            B();
            foreach (MojType entity in App.AllEntities.Where(x => !x.IsAbstract))
            {
                O("builder.EntitySet<{0}>(\"{1}\");", entity.ClassName, entity.PluralName);
                //O("builder.Action(\"{0}\");", entity.PluralName);
            }
            E();
#endif
            End();
            End();
        }

        void OPropertyConstraints(MojType type)
        {
            var item = type.VName;
            foreach (var prop in type.GetProps())
            {
                if (prop.IsNavigation)
                    // Skip reference navigation properties.
                    continue;

                if (prop.IsTenantKey)
                {
                    // Do not expose the Tenant information to the outside.
                    O($"{item}.Ignore(x => x.{prop.Name});");
                    continue;
                }

                // Skip non-stored model properties.
                if (prop.IsModel() && prop.Store == null)
                    continue;

                if (prop.Rules.IsRequired)
                    O($"{item}.Property(x => x.{prop.Name}).IsRequired();");
            }
        }

        void ONextSequenceValueFunctions(MojType type)
        {
            // DB annotations are only defined on store types.
            type = type.RequiredStore;

            string item = type.VName;

            foreach (var prop in type.GetProps().Where(x =>
                x.DbAnno.Sequence.Is &&
                x.DbAnno.Unique.HasParams))
            {
                O();
                O($"func = {item}.Collection.Function(\"{prop.GetNextSequenceValueMethodName()}\");");

                // Parameters
                foreach (var per in prop.DbAnno.Unique.GetParams())
                    O($"func.Parameter<{per.Prop.Type.NameNormalized}>(\"{per.Prop.Name}\");");

                // Return next sequence value.
                O($"func.Returns<{prop.Type.NameNormalized}>();");
            }
        }
    }
}
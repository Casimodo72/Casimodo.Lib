using System.IO;

namespace Casimodo.Lib.Mojen
{
    // TODO: Generate lower-case class methods.

    public class TsObjectInitializerGen : TsGenBase
    {
        public TsObjectInitializerGen()
        {
            Scope = "App";
        }

        public string ModuleName { get; set; }

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();
            ModuleName = webConfig.ScriptNamespace;
            var outputDirPath = webConfig.TypeScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.GetTypes(MojTypeKind.Entity)
                // TODO: REMOVE: , MojTypeKind.Complex)
                // TODO: REMOVE: .Where(x => !x.WasGenerated)
                .Where(x => !x.IsAbstract && !x.IsTenant)
                .ToArray();

            PerformWrite(Path.Combine(outputDirPath, "DataInitializers.generated.ts"), () =>
            {
                OTsNamespace(webConfig.ScriptNamespace, () =>
                {
                    O();
                    O("// Init...OnEditing: Creates nested objects if missing.");
                    O();
                    O("// Init...OnSaving: Sets all navigation properties of non-nested objects to null");
                    O("//   because we don't want to send them to the server as they");
                    O("//   might be only partially expanded and because we must not change their values anyway.");
                    O("//   Except for independent associations (collections).");

                    OTsClass("DataInitializer", isstatic: true, hasconstructor: false,
                        content: () =>
                        {
                            foreach (var item in items)
                            {
                                Generate(item);
                            }
                        }
                    );
                });
            });
        }

        public void Generate(MojType type)
        {
            GenerateOnEditing(type);
            GenerateOnSaving(type);
        }

        public void GenerateOnEditing(MojType type)
        {
            O();
            OB($"Init{type.Name}OnEditing (item: Partial<{type.Name}>): Partial<{type.Name}>");

            // Process nested object references.
            var nestedProps = type.GetProps()
                .Where(x =>
                    x.IsNavigation &&
                    x.Reference.IsNested &&
                    x.Reference.IsToOne
                );

            foreach (var prop in nestedProps)
            {
                O($"if (!item.{prop.Name}) item.{prop.Name} = new {ModuleName}.{prop.Reference.ToType.Name}();");
            }

            // TODO: REVISIT: REMOVE?
#if (false)
            // Process independent associations (collections).
            var independentCollectionProps = type.GetProps()
                .Where(x =>
                    x.IsNavigation &&
                    x.Reference.Independent &&
                    x.Reference.IsToMany
                );

            foreach (var prop in independentCollectionProps)
            {
                O($"if (!item.{prop.Name}) item.{prop.Name} = [];");
            }
#endif

            O("return item;");

            End();
        }

        public void GenerateOnSaving(MojType type)
        {
            O();
            OB($"Init{type.Name}OnSaving (item: Partial<{type.Name}>, deleteOwned: boolean = true): Partial<{type.Name}>");

            var referenceProps = type.GetProps().Where(x => x.IsNavigation);

            foreach (var prop in referenceProps)
            {
                if (prop.IsHiddenCollectionNavigationProp)
                    continue;

                if (prop.Reference.IsToMany)
                {
                    // TODO: Currently we only support saving of independent collection props.
                    //   Neither nested or loose collections are saved currently.
                    // TODO: EF Core: we don't use independent collections in EF Core
                    //   because they are not supported (yet?).
                    if (!prop.Reference.Independent)
                    {
                        if (prop.Reference.IsOwned)
                        {    
                            // Delete property or initialize.
                            OB($"if (typeof item.{prop.Name} !== 'undefined')");

                            OB("if (deleteOwned)");
                            O($"delete item.{prop.Name};");
                            End();
                            OB($"else if (item.{prop.Name} !== null)");
                            O($"for (const o of item.{prop.Name}) this.Init{prop.Type.CollectionElementTypeConfig.Name}OnSaving(o, false);");
                            End();

                            End();
                        }
                        else
                        {
                            // Delete property
                            O($"if (typeof item.{prop.Name} !== 'undefined') delete item.{prop.Name};");
                        }
                    }
                }
                else if (prop.Reference.IsToOne)
                {
                    // Set all navigation properties of non-nested references to null,
                    //  because we don't want to send them to the server.
                    //  We need the foreign keys only.

                    if (prop.Reference.IsLoose)
                    {
                        O($"if (item.{prop.Name}) item.{prop.Name} = null;");
                    }
                    else if (prop.Reference.IsNested)
                    {
                        // Call InitOnSaving for the nested referenced object.
                        O("// Preserve nested object.");
                        O($"if (item.{prop.Name}) {ModuleName}.Init{prop.Reference.ToType.Name}OnSaving(item.{prop.Name});");
                    }
                }
            }

            O("return item;");

            End();
        }
    }
}
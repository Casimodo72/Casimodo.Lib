using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class JsObjectInitializerGen : DataLayerGenerator
    {
        public JsObjectInitializerGen()
        {
            Scope = "App";
        }

        public string ModuleName { get; set; }

        protected override void GenerateCore()
        {
            var webConfig = App.Get<WebDataLayerConfig>();
            ModuleName = webConfig.ScriptNamespace;
            var outputDirPath = webConfig.JavaScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
                .Where(x => !x.WasGenerated)
                .Where(x => !x.IsAbstract && !x.IsTenant).ToArray();

            PerformWrite(Path.Combine(outputDirPath, "data.initializers.generated.js"), () =>
            {
                OJsNamespace(webConfig.ScriptNamespace, () =>
                {
                    O();
                    O("// Init...OnEditing: Creates nested objects if missing.");
                    O();
                    O("// Init...OnSaving: Sets all navigation properties of non-nested objects to null");
                    O("//   because we don't want to send them to the server as they");
                    O("//   might be only partially expanded and because we must not change their values anyway.");
                    O("//   Except for independent associations (collections).");

                    foreach (var item in items)
                    {
                        Generate(item);
                    }
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
            // NOTE: We use the Name not the ClassName here. Otherwise
            //   we would create lots of TS classes ending with "Entity",
            //   which would be ugly.
            O();
            OB("{0}.Init{1}OnEditing = function (item)", ModuleName, type.Name);

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

            // Process independent associations (collections).
            var independentCollectionProps = type.GetProps()
                .Where(x =>
                    x.IsNavigation &&
                    x.Reference.IsIdependent &&
                    x.Reference.IsToMany
                );

            foreach (var prop in independentCollectionProps)
            {
                O($"if (!item.{prop.Name}) item.{prop.Name} = [];");
            }

            End(";");
        }

        public void GenerateOnSaving(MojType type)
        {
            O();
            OB("{0}.Init{1}OnSaving = function (item)", ModuleName, type.Name);

            var referenceProps = type.GetProps().Where(x => x.IsNavigation);

            foreach (var prop in referenceProps)
            {
                if (prop.Reference.IsToMany)
                {
                    // KABU TODO: IMPORTANT: REVISIT: Currently we only support saving 
                    //   of independent collection props.
                    //   Neither nested or loose collections are saved currently.
                    if (!prop.Reference.IsIdependent)
                    {
                        // NOTE: We delete the property in this case.
                        O($"if (typeof item.{prop.Name} !== 'undefined') delete item.{prop.Name};");
                    }
                }
                else if (prop.Reference.IsToOne)
                {
                    // Set all navigation properties of non-nested references to null,
                    //  because we don't want to send them to the server as they might be only partially expanded.

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

            End(";");
        }
    }
}
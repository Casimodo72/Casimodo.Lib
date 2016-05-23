﻿using Casimodo.Lib.Data;
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
            Scope = "Context";
        }

        public string ModuleName { get; set; }

        protected override void GenerateCore()
        {
            var context = App.Get<DataLayerConfig>();
            ModuleName = context.ScriptNamespace;
            var outputDirPath = context.JavaScriptDataDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
                .Where(x => !x.WasGenerated)
                .Where(x => !x.IsAbstract && !x.IsTenant).ToArray();

            PerformWrite(Path.Combine(outputDirPath, "data.initializers.generated.js"), () =>
            {
                OJsNamespace(context.ScriptNamespace, () =>
                {
                    O();
                    O("// Init...OnEditing: Creates objects on nested navigation properties if missing.");
                    O();
                    O("// Init...OnSaving: Sets all navigation properties of non-nested objects to null");
                    O("//   because we don't want to send them to the server as they");
                    O("//   might be only partially expanded and because we must not change their values anyway.");
                    O();

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
            var naviProps = type.GetProps()
                .Where(x =>
                    x.Reference.IsNavigation &&
                    x.Reference.Binding.HasFlag(MojReferenceBinding.Nested) &&
                    x.Reference.IsToOne
                );

            foreach (var prop in naviProps)
            {
                O();
                O($"if (!item.{prop.Name}) item.{prop.Name} = new {ModuleName}.{prop.Reference.ToType.Name}();");
            }

            End(";");
        }

        public void GenerateOnSaving(MojType type)
        {
            O();
            OB("{0}.Init{1}OnSaving = function (item)", ModuleName, type.Name);

            // Set all navigation properties of non-nested references to null,
            //  because we don't want to send them to the server as they might be only partially expanded.

            var naviProps = type.GetProps()
                .Where(x =>
                    x.Reference.IsNavigation);

            foreach (var prop in naviProps)
            {
                O();


                if (prop.Reference.IsToMany)
                {
                    // KABU TODO: IMPORTANT: REVISIT: Currently we don't support saving of collection props.
                    // NOTE: We delete the property in this case.
                    O($"if (typeof item.{prop.Name} != 'undefined') delete item.{prop.Name};");
                }
                else if (prop.Reference.Binding.HasFlag(MojReferenceBinding.Loose))
                {
                    O($"if (item.{prop.Name}) item.{prop.Name} = null;");
                }
                else
                {
                    // Call InitOnSaving for the nested referenced object.
                    O("// Preserve nested object.");
                    O($"if (item.{prop.Name}) {ModuleName}.Init{prop.Reference.ToType.Name}OnSaving(item.{prop.Name});");
                }
            }

            End(";");
        }
    }
}
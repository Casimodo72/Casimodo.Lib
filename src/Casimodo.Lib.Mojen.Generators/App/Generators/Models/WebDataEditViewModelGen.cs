using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class WebDataEditViewModelGen : ClassGen
    {
        static readonly ReadOnlyCollection<string> IgnoredModelAttrs = new ReadOnlyCollection<string>(new string[] {
            "ForeignKey",
            "DataMember",
            "DatabaseGenerated",
        });

        public WebDataEditViewModelGen()
        {
            Scope = "App";

            Namespaces = new List<string>
            {
               "System",
               "System.Collections.Generic",
               "System.ComponentModel",
               "System.Collections.ObjectModel",
               "System.ComponentModel.DataAnnotations",
               "System.Runtime.Serialization",
               "Casimodo.Lib",
               "Casimodo.Lib.ComponentModel",
               "Casimodo.Lib.Data"
            };
        }

        public List<string> Namespaces { get; private set; }

        public void GenerateEditViewModel(MojType type, List<MojViewPropInfo> editorViewPropInfos, string viewGroup)
        {
            var propInfos = new List<MojViewPropInfo>();

            // All native props.
            propInfos.AddRange(editorViewPropInfos.RemoveWhere(x => x.IsNative));

            // All native foreign keys.            
            propInfos.AddRange(editorViewPropInfos.RemoveWhere(x => x.IsForeignKey && x.ForeignDepth == 1));

            // Ignore foreign keys of higher depth.
            //editorViewPropInfos.RemoveWhere(x => x.IsForeignKey && x.ForeignDepth > 1);

            // Only one of each foreign entity navigation group.
            propInfos.AddRange(editorViewPropInfos.DistinctBy(x => x.ViewProp.Name));

            propInfos = propInfos.DistinctBy(x => x.ViewProp.Name).ToList();

            var propItems = propInfos.Select(x => new
            {
                Info = x,
                Prop = (x.IsForeignKey && !x.IsForeign) ? x.ViewProp.ForeignKey : x.ViewProp
            })
            .ToList();

            // Expand related file reference properties.
            propItems.AddRange(
                propItems.ToArray()
                    .Where(x => x.Prop.FileRef.Is)
                    .SelectMany(x => x.Prop.AutoRelatedProps)
                    .Select(x => new
                    {
                        Info = (MojViewPropInfo)null,
                        Prop = x
                    }));

            // Ensure key property.
            if (!propItems.Any(x => x.Prop.IsKey))
            {
                propItems.Insert(0, new
                {
                    Info = (MojViewPropInfo)null,
                    Prop = type.Key
                });
            }

            OGeneratedFileComment();

            OUsing(Namespaces,
                "System.ComponentModel.DataAnnotations.Schema",
                type.Namespace);
            ONamespace(App.Get<WebBuildConfig>().WebDataViewModelsNamespace);

            viewGroup = viewGroup ?? "";

            // Class
            O($"public partial class {viewGroup}{type.Name}Model");
            Begin();

            // Properties
            // NOTE: By design we use model props for data annotations, but entity props for
            //   the type of the property.
            MojPropType effectivePropType;
            foreach (var propItem in propItems)
            {
                var prop = propItem.Prop;

                if (!prop.StoreOrSelf.IsEntity())
                    // NOTE: If the model prop has no underlying store property
                    // then we'll skip it becase we operate on entities (and *not* on models) in the web editors.
                    continue;

                // KABU TODO: Maybe add for hidden fields: [System.Web.Mvc.HiddenInput(DisplayValue = false)]
                var info = propItem.Info;
                if (info != null)
                {
                    O($"[Display(Name = \"{info.EffectiveDisplayLabel}\")]");
                }

                // Attributes
                foreach (var attr in prop.Attrs
                    .Where(x => info == null || x.Name != "Display")
                    .Where(x => !IgnoredModelAttrs.Contains(x.Name))
                    .OrderBy(x => x.Position)
                    .ThenBy(x => x.Name))
                {
                    O(BuildViewModelAttr(attr));
                }

                // Data type annotation.
                if (prop.Type.AnnotationDataType != null)
                    O("[DataType(DataType.{0})]", prop.Type.AnnotationDataType.Value.ToString());

                ORequiredAttribute(prop);
                ODefaultValueAttribute(prop, "OnEdit", null);

                // Ensure we use entity props not their models.
                effectivePropType = prop.IsEntity() ? prop.Type : prop.RequiredStore.Type;

                O($"public {effectivePropType.Name} {prop.Name} {{ get; set; }}");

                O();
            }

            End(); // Class
            End(); // Namespace
        }

        string BuildViewModelAttr(MojAttr attr)
        {
            if (attr.Name == "LocallyRequired")
                return "[Required]";
            else
                return BuildAttr(attr);
        }
    }
}
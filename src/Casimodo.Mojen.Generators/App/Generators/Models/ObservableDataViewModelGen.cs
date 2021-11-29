﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Generates entity view models for WPF/Universal Apps (XAML).
    /// </summary>
    public class ObservableDataViewModelGen : ClassGen
    {
        static readonly ReadOnlyCollection<string> IgnoredModelAttrs = new ReadOnlyCollection<string>(new string[] {
            // NOTE: [Required] will be applied via the OData model builder.
            "Required",
            "DataMember",
            "DatabaseGenerated",
            "ForeignKey",
            // For XAML apps we don't need those:
            "UIHint", "DataType"
        });

        public ObservableDataViewModelGen()
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

        public ViewModelLayerConfig ViewModelConfig { get; set; }

        public List<string> Namespaces { get; private set; }

        protected override void GenerateCore()
        {
            ViewModelConfig = App.Get<ViewModelLayerConfig>();

            if (string.IsNullOrEmpty(ViewModelConfig.ModelsDirPath))
                return;

            var models = App.AllModels.ToArray();
            foreach (MojType model in models)
            {
                string outputDirPath = model.OutputDirPath != null
                    ? Path.Combine(ViewModelConfig.ModelsDirPath, model.OutputDirPath)
                    : ViewModelConfig.ModelsDirPath;

                string outputFilePath = Path.Combine(outputDirPath, model.ClassName + ".generated.cs");

                PerformWrite(outputFilePath, () => GenerateModel(model));
            }
        }

        public string Sep(int i, int count, string sep = ",")
        {
            return i < count - 1 ? sep : "";
        }

        public void GenerateModel(MojType type)
        {
            OUsing(Namespaces,
                "System.ComponentModel.DataAnnotations.Schema",
                "System.Text.Json.Serialization",
                (type.StoreOrSelf.Namespace != type.Namespace ? type.StoreOrSelf.Namespace : null),
                GetFriendNamespaces(type));

            ONamespace(ViewModelConfig.Namespace);

            // ONamespace(type.Namespace);

            GenerateClassHead(type, interfaces: type.Interfaces.Where(x => x.AddToViewModel).ToList());

            // Static constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            O("static {0}()", type.ClassName);
            Begin();
            if (!type.IsAbstract && type.HasAncestorType("ValidatingObservableObject"))
            {
                // Validation
                if (!type.NoValidation)
                    O("ValidationRules.Add(typeof({0}));", type.ClassName);

                // Change tracking
                if (!type.NoChangeTracking)
                {
                    if (type.ChangeTrackingProps.Count != 0)
                    {
                        O("MoDataSnapshot.Add(typeof({0}),", type.ClassName);
                        int i = 0;
                        foreach (var prop in type.ChangeTrackingProps)
                            O("    nameof({0}){1}", prop.Name, Sep(i++, type.ChangeTrackingProps.Count));

                        O(");");
                    }
                }
            }
            O("CreateTypeExtended();");
            End();
            O();
            O("static partial void CreateTypeExtended();");
            O();
            // End of static constructor.

            // Parameterless constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            O("public {0}()", type.ClassName);
            Begin();

            bool isStoreWrapper = type.Store != null && type.IsStoreWrapper;

            if (isStoreWrapper && !type.IsAbstract)
            {
                O("SetStore(new {0}());", type.Store.ClassName);
            }

            foreach (var collectionProp in type.NonArrayCollectionProps)
            {
                O("{0} = new ObservableCollection<{1}>();", collectionProp.Name, collectionProp.Type.GenericTypeArguments.First().Name);
            }

            O("CreateExtended();");
            End();

            O();
            O("partial void CreateExtended();");

            if (isStoreWrapper)
            {
                // Constructor with store entity parameter.
                O();
                O("public {0}({1} store)", type.ClassName, type.Store.ClassName);
                Begin();
                O("SetStore(store);");
                O("CreateExtended();");
                End();

                O();
                O("public {0}{1} Store", (type.IsDerivedFromStoreWrapper ? "new " : ""), type.Store.ClassName);
                Begin();
                O("get => _store;");
                O("set => SetStore(value);");
                End();
                O("{0} _store;", type.Store.ClassName);

                O();
                O("protected {0} SetStore({1} store)", type.ClassName, type.Store.ClassName);
                Begin();
                if (type.IsDerivedFromStoreWrapper)
                    O("base.SetStore(store);");
                O("_store = store;");
                O();
                O("return this;");
                End();
            }

            var allProps = type.GetProps(custom: false)
                .Where(x => !x.IsODataDynamicPropsContainer)
                .ToList();

            // Constructor for JSON deserialization.
            O();
            O("[JsonConstructor]");
            Oo($"public {type.ClassName}(");
            o(allProps
                .Select(prop => $"{prop.Type.Name} {prop.Name}")
                .Join(", "));
            oO(")");
            Begin();
            O(allProps
                .Select(prop =>
                {
                    var name = prop.IsObservable && prop.ProxyOfInheritedProp == null
                        ? $"_{Moj.FirstCharToLower(prop.Name)}"
                        : prop.Name;
                    return $"this.{name} = {prop.Name}; ";
                })
                .Join(""));
            End();

            // Local properties.
            foreach (MojProp prop in type.GetLocalProps(custom: false))
            {
                if (prop.IsODataDynamicPropsContainer)
                    continue;

                O();
                OSummary(prop.Summary);

                if (prop.IsKey)
                    O("[Key]");

                // Attributes
                foreach (var attr in prop.Attrs
                    .Where(x => !IgnoredModelAttrs.Contains(x.Name))
                    .OrderBy(x => x.Position)
                    .ThenBy(x => x.Name))
                {
                    O(BuildAttr(attr));
                }

                ORequiredAttribute(prop);
                ODefaultValueAttribute(prop, "OnEdit", null);

                // For XAML apps we don't need data type annotations.
                // KABU TODO: REMOVE
                //// Data type annotation.
                //if (prop.Type.AnnotationDataType != null)
                //    O("[DataType(DataType.{0})]", prop.Type.AnnotationDataType.Value.ToString());

                // Mapping
                if (prop.Store == null /* || prop.IsMappedToStore == false */)
                {
                    // Model-property is NOT mapped at all.
                }
                else if (prop.IsEditable != true)
                    // Model-property is NOT editable and will NOT be assigned to the entity.
                    O("[StoreMapping(to: false)]");
                else
                    // Model-property is editable and will be assigned to the entity.
                    O("[StoreMapping]");

                // Declare property
                GenerateProp(type, prop, store: isStoreWrapper && prop.Store != null);
            }

            GenerateIKeyAccessorImpl(type);

            GenerateIGuidGenerateableImpl(type);

            GenerateTypeComparisons(type);

            GenerateAssignFromMethod(type);

            GenerateNamedAssignFromMethods(type);

            End();
            End();
        }
    }
}
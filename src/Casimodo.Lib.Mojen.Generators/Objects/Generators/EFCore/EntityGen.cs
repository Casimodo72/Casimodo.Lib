using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class CoreEntityGen : ClassGen
    {
        static readonly ReadOnlyCollection<string> ValidEntityAttrs = new ReadOnlyCollection<string>(new string[] {
            // NOTE: [Required], [Index] will be applied via the EF model builder.            
            "DataMember",
            // [IgnoreDataMember] is used currently only used for the Tenant ID, which must not be exposed.
            "IgnoreDataMember",
            // Value constraints
            "StringLength", "MinLength", "MaxLength", "Range", "Precision", "RegularExpression",
            "DefaultValue",            
            // [DatabaseGenerated] is currently not used, but may be used if we use database-generated identity columns (e.g. integers for IDs).
            "DatabaseGenerated",
            "ForeignKey",
            "Display"
        });

        public CoreEntityGen()
        {
            Scope = "Context";
        }

       

        protected override void GenerateCore()
        {
            DataConfig = App.Get<DataLayerConfig>();

            if (string.IsNullOrEmpty(DataConfig.EntityDirPath))
                return;

            foreach (var entity in App.AllEntities.Where(x => !x.WasGenerated))
            {
                PerformWrite(Path.Combine(DataConfig.EntityDirPath, entity.ClassName + ".generated.cs"),
                    () => Generate(entity));
            }
        }

        public void Generate(MojType type)
        {
            OUsing(BuildNamespaces(type), "System.Linq");

            ONamespace(type.Namespace);

            // Class declaration
            GenerateClassHead(type);

            // Static constructor
#if (false)
            O("static {0}()", entity.ClassName);
            B();
            E();
            O();
#endif
            // Constructor
#if (false)
            O("public {0}()", entity.ClassName);
            B();
            E();
#endif

            // Properties
            O();
            MojProp prop;
            var props = type.GetLocalProps(custom: false).Where(x => !x.IsHiddenCollectionNavigationProp).ToList();
            for (int i = 0; i < props.Count; i++)
            {
                prop = props[i];

                if (prop.IsODataDynamicPropsContainer)
                    // OData dynamic properties container is handled at a later stage.
                    continue;

                if (i > 0)
                    O();

                OSummary(prop.Summary);

                if (prop.IsKey)
                    O("[Key]");

                if (prop.IsExcludedFromDb)
                    O("[NotMapped]");

                //var dbAnnotations = prop.ContainingType.GetIndexesWhereIsMember(prop).ToArray();
                //foreach (var anno in dbAnnotations)
                //{
                //    O($"[Index(\"{anno.GetIndexName()}\", {anno.GetIndexMemberIndex(prop)}, IsUnique = {MojenUtils.TOValue(anno.Unique.Is)})]");
                //}

                // Ignore tenant ID.
                //if (prop.IsTenantKey) O("[IgnoreDataMember]");

                if (!type.NoDataContract && !prop.IsTenantKey)
                    O("[DataMember]");

                // Attributes
                foreach (var attr in prop.Attrs
                    .Where(x => ValidEntityAttrs.Contains(x.Name))
                    .OrderBy(x => x.Position)
                    .ThenBy(x => x.Name))
                {
                    O(BuildAttr(attr));
                }

                // NOTE: We don't specify the [Required] attribute by design.
                // The "required" constraint is established using the EF model generator only.
                // ORequiredAttribute(prop);

                ODefaultValueAttribute(prop, null);

                // Make navigation properties virtual.
                string accessor = prop.IsNavigation ? " virtual" : "";

                O($"public{accessor} {prop.Type.Name} {prop.Name} {{ get; set; }}");
            }

            GenerateInterfaceImpl(type);

            GenerateIKeyAccessorImpl(type);

            GenerateIGuidGenerateableImpl(type);

            GenerateIMultitenantImpl(type);

            GenerateODataOpenTypePropsContainer(type);

            GenerateTypeComparisons(type);

            GenerateAssignFromMethod(type);

            GenerateNamedAssignFromMethods(type);

            // GenSelfAssign(type);

            End();
            End();
        }

        void GenSelfAssign(MojType type)
        {
            OB($"public static void AssignSimpleProps({type.Name} source, {type.Name} target)");

            var props = type.GetProps().ToList();
            MojProp prop;
            for (int i = 0; i < props.Count; i++)
            {
                prop = props[i];
                if (prop.Type.IsMojType)
                    continue;

                var ptype = prop.Type.TypeNormalized;

                if (prop.Type.IsCollection || !IsSimpleType(ptype))
                    continue;

                O($"target.{prop.Name} = source.{prop.Name};");
            }

            End();
        }

        bool IsSimpleType(Type type)
        {
            return type == typeof(string) ||
                (type.FullName.StartsWith("System") &&
                !typeof(System.Collections.IEnumerable).IsAssignableFrom(type));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class EntityGen : ClassGen
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

        public EntityGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            if (string.IsNullOrEmpty(App.Get<DataLayerConfig>().EntityDirPath)) return;

            foreach (var entity in App.AllEntities.Where(x => !x.WasGenerated))
            {
                PerformWrite(Path.Combine(App.Get<DataLayerConfig>().EntityDirPath, entity.ClassName + ".generated.cs"),
                    () => Generate(entity));
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
            var props = type.GetLocalProps(custom: false).Where(x => !x.IsHiddenOneToManyEntityNavigationProp).ToList();
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
                string accessor = prop.Reference.IsNavigation ? " virtual" : "";

                O($"public{accessor} {prop.Type.Name} {prop.Name} {{ get; set; }}");
            }

            GenerateIKeyAccessorImpl(type);

            GenerateIGuidGenerateableImpl(type);

            GenerateIMultitenantImpl(type);

            GenerateODataOpenTypePropsContainer(type);

            GenerateTypeComparisons(type);

            GenerateAssignFromMethod(type);

            End();
            End();
        }
    }
}
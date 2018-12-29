using Casimodo.Lib.Data;
using Casimodo.Lib;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // EF Core data annotations:
    //   See https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes

    public class CoreDbContextModelGen : MojenGenerator
    {
        public CoreDbContextModelGen()
        {
            Scope = "DataContext";
        }

        public DataLayerConfig DataConfig { get; set; }

        protected override void GenerateCore()
        {
            DataConfig = App.Get<DataLayerConfig>();

            if (!DataConfig.IsDbContextModelEnabled) return;
            if (string.IsNullOrEmpty(DataConfig.DbContextDirPath)) return;
            if (string.IsNullOrEmpty(DataConfig.DbContextName)) return;

            PerformWrite(Path.Combine(DataConfig.DbContextDirPath, DataConfig.DbContextName + ".Model.generated.cs"),
                GenerateDbContextModel);
        }

        string GetIndexName(MojProp prop)
        {
            return (prop.DbAnno.Unique.Is ? "U" : "") + "IX_" + prop.Name;
        }

        string GetManyToManyClassName(MojProp prop)
        {
            return prop.DeclaringType.PluralName + "2" + prop.Reference.ToType.PluralName;
        }

        void GenerateDbContextModel()
        {
            var types = App.AllConcreteEntities.ToArray();

            OUsing("System", "Microsoft.EntityFrameworkCore");

            ONamespace(DataConfig.DataNamespace);

            // DbContext class
            OB($"public partial class {DataConfig.DbContextName}");

            // Build model with ModelBuilder
            OB($"void CreateModel(ModelBuilder builder)");

            foreach (var type in types)
            {
                var item = type.VName;

                OCommentSection(type.Name);

                OB($"builder.Entity<{type.ClassName}>(b => ");
                // Specify Db table name.
                O($"b.ToTable(\"{type.TableName}\");");

                // KABU TODO: IMPORTANT: REVISIT: EF Core does not support TPC (yet?.
                //   Currently only TPH is implemented.
#if (false)
                OB($"{item}.Map(m =>");
                // Using EF inheritance strategy: TPC (table per concrete type)
                O("m.MapInheritedProperties();");
                End(");");
#endif

                var properties = type.GetProps()
                    // Exclude hidden collection props.
                    .Where(x => !x.IsHiddenCollectionNavigationProp)
                    .ToArray();

                // Index ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                // See https://www.learnentityframeworkcore.com/configuration/fluent-api/hasindex-method

                var indexes = type.GetProps().Where(x => x.DbAnno.Index.Is)
                    .Select(x => new
                    {
                        IndexName = GetIndexName(x),
                        Prop = x
                    });

                if (indexes.HasDuplicates(x => x.IndexName))
                    throw new MojenException("Duplicate index names.");


                foreach (var dbindex in indexes)
                {
                    // Index: entity.HasIndex("TenantId", "MySomeProp", "MyContextProp").HasName("UIX_MyContextProp").IsUnique();
                    var propNames = dbindex.Prop.DbAnno.Index.Participants.Select(x => "\"" + x.Prop.Name + "\"").Join(", ");
                    Oo($"b.HasIndex({propNames}).HasName(\"{dbindex.IndexName}\")");

                    if (dbindex.Prop.DbAnno.Index.IsUnique)
                        o(".IsUnique()");

                    oO(";");
                }

                foreach (var prop in properties)
                {
                    // Mark as required.
                    // We handle only explicitely required properties here.
                    //   (If the property's type is not nullable then EF will make
                    //    a non-nullable DB field (obviously)).
                    if (prop.Rules.IsRequired && !prop.IsNavigation)
                    {
                        O($"b.Property(\"{prop.Name}\").IsRequired();");
                    }
                }

                if (type.IsManyToManyLink)
                {
                    // See https://stackoverflow.com/questions/49214748/many-to-many-self-referencing-relationship

                    foreach (var prop in type.GetLocalProps().Where(x => x.IsNavigation && x.Reference.Is && x.Reference.IsToOne))
                    {
                        O($"b.HasOne(ab => ab.{prop.Name})");
                        Push();
                        Oo(".WithMany(");
                        if (!prop.Reference.ForeignCollectionProp.IsHiddenCollectionNavigationProp)
                            o($"a => a.{prop.Reference.ForeignCollectionProp.Name}");
                        oO(")");
                        O($".HasForeignKey(ab => ab.{prop.ForeignKey.Name}).OnDelete(DeleteBehavior.Restrict);");
                        Pop();
                    }
                }
                else
                {
                    // ToMany ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                    // ToMany navigation properties.
                    var props = properties
                        .Where(x =>
                            x.IsNavigation &&
                            x.Reference.ToType.IsEntity() &&
                            x.Reference.IsToMany &&
                            // Ignore many-to-many because those will be modelled elsewhere.
                            !x.Reference.ToType.IsManyToManyLink);

                    foreach (var prop in props)
                    {
                        O();
                        var propName = prop.Name;

                        // EF Core does not support independent associations.
                        // We now always create link many-to-many types explicitely.
                        if (prop.Reference.Binding.HasFlag(MojReferenceBinding.Independent))
                        {
                            throw new MojenException("Independent (many-to-many) associations are not supported.");
                        }
                        else
                        {
                            var itemToContainerBackProp = prop.Reference.ForeignItemToCollectionProp;
                            var backrefCount = prop.Reference.ToType.GetOwnedByRefProps().Count();

                            if (prop.Reference.ToType.HasManyParents == true && itemToContainerBackProp?.Rules.IsRequired == true)
                                throw new MojenException("Required child to parent reference mismatch.");

                            if (backrefCount > 1 && itemToContainerBackProp?.Rules.IsRequired == true)
                                throw new MojenException("Required child to parent reference mismatch.");

                            O($"b.HasMany(x => x.{prop.Name})");
                            Push();

                            Oo($".WithOne(");
                            // Specify navigation backref prop if it exists.
                            var itemToContainerNaviBackProp = itemToContainerBackProp?.Navigation;
                            if (itemToContainerNaviBackProp != null)
                                o($@"x => x.{itemToContainerNaviBackProp.Name}");
                            oO(")");

                            if (itemToContainerBackProp?.Rules.IsRequired != true &&

                            (itemToContainerBackProp?.Rules.IsNotRequired == true ||
                             prop.Reference.ToType.HasManyParents == true ||
                             // Case 1:
                             backrefCount > 1 ||
                             (backrefCount == 1 && prop.Reference.ToType.GetOwnedByRefProps().First().Rules.IsNotRequired)))
                            {
                                // Case 1: If the target type has multiple back reference foreign keys then
                                //   those foreign keys must be optional, because mostly only
                                //   one of those foreign keys applies and is set.
                                //   E.g. Job has many WorkTimes and BreakTimes.
                                //     The target JobTimeRange has *two* back references - foreign keys - back to Job:
                                //     JobTimeRange.WorkTimeOfJobId and JobTimeRange.BreakTimeOfJobId
                                //     Only one of those foreign keys can be set. Either the JobTimeRange
                                //     represents the work-time of a Job or the break-time of a Job.

                                O($".IsRequired(false)");
                            }
                            else
                                O($".IsRequired()");

                            // Specify the back reference property.
                            O($".HasForeignKey(y => y.{prop.Reference.ForeignItemToCollectionProp.ForeignKey.Name})");

                            // KABU TODO: REMOVE? This was intended for polymorphic associations, which do not work the way we want them anyway.
                            //else O($".HasForeignKey(y => y.{prop.Reference.ChildToParentReferenceProp.Name})");

                            //  KABU TODO: REVISIT: Currently we will hande cascading deletion ourselves, so turn it off.
                            O(".OnDelete(DeleteBehavior.Restrict);");

                            Pop();
                        }
                    }
                }

                End(");"); // entity builder
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                O();
            }

            O();
            O("OnModelCreatingApplyDecimalPrecision(builder);");

            End(); // CreateModel method

            End(); // DbContext class

            End(); // ns
        }
    }
}
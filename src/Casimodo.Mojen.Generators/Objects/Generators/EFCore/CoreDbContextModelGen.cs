using Casimodo.Lib.Data;
using System.IO;

namespace Casimodo.Mojen
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

                // DB table name.
                O($"b.ToTable(\"{type.TableName}\");");

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
                    })
                    .ToList();

                if (indexes.HasDuplicates(x => x.IndexName))
                    throw new MojenException("Duplicate index names.");

                foreach (var dbindex in indexes)
                {
                    // Index: entity.HasIndex("TenantId", "MySomeProp", "MyContextProp").HasDatabaseName("UIX_MyContextProp").IsUnique();
                    var propNames = dbindex.Prop.DbAnno.Index.Members.Select(x => "\"" + x.Prop.Name + "\"").Join(", ");
                    Oo($"b.HasIndex({propNames}).HasDatabaseName(\"{dbindex.IndexName}\")");

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
                        O($"b.Property(x => x.{prop.Name}).IsRequired();");
                    }

                    var defaultValue = prop.DefaultValues.ForScenario("database").FirstOrDefault();
                    if (defaultValue != null)
                    {
                        O($"b.Property(x => x.{prop.Name}).HasDefaultValue({Moj.CS(defaultValue.Value)});");
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
                    // One to one ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                    // https://docs.microsoft.com/en-us/ef/core/modeling/relationships
                    var toOneNavigationProps = properties
                        .Where(x =>
                            x.IsForeignKey &&
                            x.Reference.IsToOne &&
                            x.Reference.ToType.IsEntity() &&
                            // Exclude ToParent because those will be modelled via the ToChild side.
                            x.Reference.Axis != MojReferenceAxis.ToParent);

                    foreach (var prop in toOneNavigationProps)
                    {
                        // TODO: This produces a warning:
                        //   "Navigations 'x' and 'y' were separated into two relationships
                        //    as ForeignKeyAttribute was specified on navigations on both sides.".
                        //    Because in the entity type we also use [ForeignKey] on both sides.
                        //    See: https://github.com/aspnet/EntityFrameworkCore/issues/11756
                        // TODO: Will this warning go away if we explicitely define the relationship
                        //   from the other side as well here?
                        O();

                        if (prop.Navigation != null)
                            O($"b.HasOne(x => x.{prop.Navigation.Name})");
                        else
                            O($"b.HasOne<{prop.Reference.ToType.Name}>()");

                        Push();

                        if (prop.Reference.Axis == MojReferenceAxis.ToChild)
                        {
                            var backrefProp = prop.Reference.ForeignBackrefProp;
                            if (backrefProp?.Navigation != null)
                                O($".WithOne(x => x.{backrefProp.Navigation.Name})");
                            else
                                O($".WithOne()");

                            if (prop.Reference.ForeignKey != null)
                            {
                                O($".HasForeignKey<{type.Name}>(x => x.{prop.Reference.ForeignKey.Name})");
                                O($".IsRequired({Moj.CS(prop.Reference.ForeignKey.Rules.IsRequired)})");
                            }
                        }
                        else
                        {
                            // ToAncestor, Value, etc.
                            O(".WithMany()");

                            if (prop.Reference.ForeignKey == null)
                                throw new MojenException("Missing foreign key");

                            O($".HasForeignKey(x => x.{prop.Reference.ForeignKey.Name})");
                        }

                        // TODO: Can't make the other side to have a foreign key with EF Core :-(
                        //if (backrefProp?.ForeignKey != null)
                        //    O($".HasForeignKey<{prop.Reference.ToType.Name}>(x => x.{backrefProp.ForeignKey.Name});");

                        O(".OnDelete(DeleteBehavior.Restrict);");

                        Pop();
                    }

                    // ToMany ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                    // ToMany navigation properties.
                    var toManyNavigationProps = properties
                        .Where(x =>
                            x.IsNavigation &&
                            x.Reference.IsToMany &&
                            x.Reference.ToType.IsEntity() &&
                            // Ignore many-to-many because those will be modelled elsewhere.
                            !x.Reference.ToType.IsManyToManyLink);

                    foreach (var prop in toManyNavigationProps)
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
                            var itemType = prop.Reference.ToType;
                            var backrefProp = prop.Reference.ForeignBackrefProp;
                            var backrefCount = itemType.GetOwnedByRefProps().Count();

                            // TODO: Move to dedicated model validation.
                            if (backrefProp?.Rules.IsRequired == true)
                            {
                                // If the collection item type expects to have (or if there effectively are)
                                //   many parents then none of its back-references must be required.
                                // I.e. if the collection item type has multiple back-reference foreign keys then
                                //   those foreign keys must be optional, because only
                                //   one of those foreign keys is effective (at least in our limited set of
                                //   supported scenarios).
                                if (itemType.HasManyParents == true || backrefCount > 1)
                                    throw new MojenException("Backref mismatch: the type will have multiple parents " +
                                        "but at least one back-reference is incorrectly configured to be required.");
                            }

                            O($"b.HasMany(x => x.{prop.Name})");
                            Push();

                            Oo($".WithOne(");
                            // Specify navigation backref prop if it exists.
                            var backrefNaviProp = backrefProp?.Navigation;
                            if (backrefNaviProp != null)
                                o($@"x => x.{backrefNaviProp.Name}");
                            oO(")");

                            if (backrefProp?.Rules.IsRequired != true &&
                                (backrefProp?.Rules.IsNotRequired == true ||
                                 itemType.HasManyParents == true ||
                                 // Case 1:
                                 backrefCount > 1 ||
                                 (backrefCount == 1 && itemType.GetOwnedByRefProps().First().Rules.IsNotRequired)))
                            {
                                // I.e. if the collection item type has multiple back-reference foreign keys then
                                //   those foreign keys must be optional, because only
                                //   one of those foreign keys is effective (at least in our limited set of
                                //   supported scenarios).
                                // E.g. Job has many WorkTimes and BreakTimes.
                                //   The target JobTimeRange has *two* back-references - foreign keys - back to Job:
                                //   JobTimeRange.WorkTimeOfJobId and JobTimeRange.BreakTimeOfJobId
                                //   Only one of those foreign keys can be set. Either the JobTimeRange
                                //   represents the work-time of a Job or the break-time of a Job.

                                O($".IsRequired(false)");
                            }
                            else
                                O($".IsRequired()");

                            // Specify the back reference property.
                            O($".HasForeignKey(y => y.{backrefProp.ForeignKey.Name})");

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
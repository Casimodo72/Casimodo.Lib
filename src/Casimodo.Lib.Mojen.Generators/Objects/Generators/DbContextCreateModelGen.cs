using Casimodo.Lib.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DbContextCreateModelGen : MojenGenerator
    {
        public DbContextCreateModelGen()
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


            PerformWrite(Path.Combine(DataConfig.DbContextDirPath, DataConfig.DbContextName + ".CreateModel.generated.cs"),
                GenerateDbContextModel);
        }

        void GenerateDbContextModel()
        {
            var types = App.AllConcreteEntities.ToArray();

            OUsing(
                "Casimodo.Lib.Data.Builder",
                "System",
                "System.Collections.Generic",
                "System.ComponentModel.DataAnnotations.Schema",
                "System.Data.Entity.Infrastructure.Annotations",
                "System.Data.Entity",
                "System.Linq");

            ONamespace(DataConfig.DataNamespace);

            O($"public partial class {DataConfig.DbContextName}");
            Begin();

            O($"void CreateModel(DbModelBuilder builder)");
            Begin();

            foreach (var type in types)
            {
                var item = type.VName;

                OCommentSection(type.Name);

                O($"var {item} = builder.Entity<{type.ClassName}>();");

                O($"{item}.Map(m =>");
                Begin();

                // Specify Db table name.
                O($"m.ToTable(\"{type.TableName}\");");

                // Using EF inheritance strategy: TPC (table per concrete type)
                O("m.MapInheritedProperties();");
                End(");");

                var properties = type.GetProps()
                    // Exclude hidden collection props.
                    .Where(x => !x.IsHiddenCollectionNavigationProp)
                    .ToArray();

                // Index ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                foreach (var prop in properties)
                {
                    if (prop.Rules.IsRequired && !prop.IsNavigation)
                    {
                        O($"{item}.Property(x => x.{prop.Name}).IsRequired();");
                    }

                    var dbAnnotations = type.GetIndexesWhereIsMember(prop).ToArray();
                    if (dbAnnotations.Any())
                    {
                        if (dbAnnotations.Length == 1)
                        {
                            var anno = dbAnnotations.First();
                            O($"{item}.Property(x => x.{prop.Name}).Index(\"{anno.GetIndexName()}\", {anno.GetIndexMemberIndex(prop)}, {Moj.CS(anno.Unique.Is)});");
                        }
                        else
                        {
                            O($"{item}.Property(x => x.{prop.Name}).Index(");
                            Push();
                            int i = 0;
                            foreach (var anno in dbAnnotations)
                            {
                                Oo($"new IndexAttribute(\"{anno.GetIndexName()}\", {anno.GetIndexMemberIndex(prop)}) {{ IsUnique = {Moj.CS(anno.Unique.Is)} }}");
                                if (++i < dbAnnotations.Length)
                                    oO(",");
                                else
                                    Br();
                            }
                            Pop();
                            O(");");
                        }
                    }
                }

                // OneToMany ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                // OneToMany navigation properties.
                var props = properties
                    .Where(x =>
                        x.IsNavigation &&
                        x.Reference.ToType.IsEntity() &&
                        x.Reference.IsToMany);

                foreach (var prop in props)
                {
                    O();
                    var propName = prop.Name;
                    O($"{item}.HasMany(x => x.{prop.Name})");
                    Push();

                    if (prop.Reference.Binding.HasFlag(MojReferenceBinding.Independent))
                    {
                        O($".WithMany()");

                        O(".Map(x =>");
                        Begin();
                        O($"x.MapLeftKey(\"{type.Name + type.Key.Name}\");");
                        O($"x.MapRightKey(\"{prop.Reference.ToType.Name + prop.Reference.ToType.Key.Name}\");");
                        O($"x.ToTable(\"{type.PluralName}2{prop.Reference.ToType.PluralName}\");");
                        End(");");
                    }
                    else
                    {
                        var itemToContainerBackProp = prop.Reference.ForeignItemToCollectionProp;
                        var backrefCount = prop.Reference.ToType.GetOwnedByRefProps().Count();

                        if (prop.Reference.ToType.HasManyParents == true && itemToContainerBackProp?.Rules.IsRequired == true)
                            throw new MojenException("Required child to parent reference mismatch.");

                        if (backrefCount > 1 && itemToContainerBackProp?.Rules.IsRequired == true)
                            throw new MojenException("Required child to parent reference mismatch.");

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

                            O($".WithOptional()");
                        }
                        else
                            O($".WithRequired()");

                        // Specify the back reference property.
                        O($".HasForeignKey(y => y.{prop.Reference.ForeignItemToCollectionProp.ForeignKey.Name})");

                        // KABU TODO: REMOVE? This was intended for polymorphic associations, which do not work the way we want them anyway.
                        //else O($".HasForeignKey(y => y.{prop.Reference.ChildToParentReferenceProp.Name})");

                        // KABU TODO: REVISIT: When we move to EF7, we need to evaluate
                        //   how no-sql databases handle deletion. Only then will we decide whether
                        //   to hand over cascades to the DB.
                        // We will hande cascading deletion ourselves, so turn it of at DB level.
                        O(".WillCascadeOnDelete(false);");
                    }

                    Pop();
                }

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                O();
            }
            End();

            End();

            End();
        }
    }
}
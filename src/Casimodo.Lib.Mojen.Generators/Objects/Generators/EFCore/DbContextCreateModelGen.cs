using Casimodo.Lib.Data;
using Casimodo.Lib;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // EF Core data annotations:
    //   See https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes

    public class CoreDbContextCreateModelGen : MojenGenerator
    {
        public CoreDbContextCreateModelGen()
        {
            Scope = "DataContext";
        }

        public DataLayerConfig DataConfig { get; set; }

        protected override void GenerateCore()
        {
            DataConfig = App.Get<DataLayerConfig>();

            if (!DataConfig.DbContextUseModelBuilder) return;
            if (string.IsNullOrEmpty(DataConfig.DbContextDirPath)) return;
            if (string.IsNullOrEmpty(DataConfig.DbContextName)) return;


            PerformWrite(Path.Combine(DataConfig.DbContextDirPath, DataConfig.DbContextName + ".CreateModel.generated.cs"),
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

            OUsing(
                "Casimodo.Lib.Data.Builder",
                "System",
                "System.Collections.Generic",
                "System.ComponentModel.DataAnnotations.Schema",
                "Microsoft.EntityFrameworkCore",
                "System.Linq");

            ONamespace(DataConfig.DataNamespace);

            OB($"public partial class {DataConfig.DbContextName}");

            OB($"void CreateModel(ModelBuilder builder)");

            var unidirectionalManyToManyProps = new List<MojProp>();

            foreach (var type in types)
            {
                var item = type.VName;

                OCommentSection(type.Name);

                O($"var {item} = builder.Entity<{type.ClassName}>();");
                // Specify Db table name.
                O($"{item}.ToTable(\"{type.TableName}\");");

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
                    Oo($"{item}.HasIndex({propNames}).HasName(\"{dbindex.IndexName}\")");

                    if (dbindex.Prop.DbAnno.Index.IsUnique)
                        o(".IsUnique()");

                    oO(";");
                }

                foreach (var prop in properties)
                {
                    if (prop.Rules.IsRequired && !prop.IsNavigation)
                    {
                        O($"{item}.Property(\"{prop.Name}\").IsRequired();");
                    }
#if (false)
                    var dbAnnotations = type.GetIndexesWhereIsMember(prop).ToArray();
                    if (dbAnnotations.Any())
                    {
                        if (dbAnnotations.Length == 1)
                        {
                            var anno = dbAnnotations.First();
                            O($"{item}.Property(x => x.{prop.Name}).Index(\"{anno.GetIndexName()}\", {anno.GetIndexMemberIndex(prop)}, {MojenUtils.ToCsValue(anno.Unique.Is)});");
                        }
                        else
                        {
                            O($"{item}.Property(x => x.{prop.Name}).Index(");
                            Push();
                            int i = 0;
                            foreach (var anno in dbAnnotations)
                            {
                                Oo($"new IndexAttribute(\"{anno.GetIndexName()}\", {anno.GetIndexMemberIndex(prop)}) {{ IsUnique = {MojenUtils.ToCsValue(anno.Unique.Is)} }}");
                                if (++i < dbAnnotations.Length)
                                    oO(",");
                                else
                                    Br();
                            }
                            Pop();
                            O(");");
                        }
                    }
#endif
                }

                // OneToMany ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                // OneToMany/ManyToMany navigation properties.
                var props = properties
                    .Where(x =>
                        x.IsNavigation &&
                        x.Reference.ToType.IsEntity() &&
                        x.Reference.IsToMany);

                foreach (var prop in props)
                {
                    O();
                    var propName = prop.Name;

#if (false)
                    public class A2B
                    {
                        public Guid AID { get; set; }
                        public A A { get; set; }

                        public Guid BID { get; set; }
                        public B B { get; set; }
                    }

                    builder.Entity<A2B>()
                        .HasKey(a2b => new { a2b.AID, A2B.BID });

                    builder.Entity<A2B>()
                        .HasOne(a2b => a2b.A)
                        .WithMany(a => a.A2B)
                        .HasForeignKey(a2b => a2b.AID);

                    builder.Entity<A2B>()
                        .HasOne(a2b => a2b.B)
                        .WithMany(b => b.A2B)
                        .HasForeignKey(a2b => a2b.BID);
#endif
                    // Handle unidirectional many-to-many relationship.
                    // KABU TODO: IMPORTANT: REVISIT: EF Core does not support independent associations (yet?).
                    //   This means we have to define an explicit join class/table.
                    //   But sometimes we would like to have an additional "Index" property on the join table.
                    //   Thus maybe we should always use an explicit join class/table.
                    if (prop.Reference.Binding.HasFlag(MojReferenceBinding.Independent))
                    {
                        unidirectionalManyToManyProps.Add(prop);
                        O("// Unidirectional many-to-many prop: " + prop.Name + " : " + GetManyToManyClassName(prop));

                        //O($"{item}.HasMany(x => x.{prop.Name})");
                        //Push();
                        //O($".WithMany()");

                        //O(".Map(x =>");
                        //Begin();
                        //O($"x.MapLeftKey(\"{type.Name + type.Key.Name}\");");
                        //O($"x.MapRightKey(\"{prop.Reference.ToType.Name + prop.Reference.ToType.Key.Name}\");");
                        //O($"x.ToTable(\"{type.PluralName}2{prop.Reference.ToType.PluralName}\");");
                        //End(");");
                    }
                    else
                    {
                        O($"{item}.HasMany(x => x.{prop.Name})");
                        Push();

                        O($".WithOne()");

                        var itemToContainerBackProp = prop.Reference.ItemToCollectionProp;
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

                            O($".IsRequired(false)");
                        }
                        else
                            O($".IsRequired()");

                        // Specify the back reference property.
                        O($".HasForeignKey(y => y.{prop.Reference.ItemToCollectionProp.ForeignKey.Name})");

                        // KABU TODO: REMOVE? This was intended for polymorphic associations, which do not work the way we want them anyway.
                        //else O($".HasForeignKey(y => y.{prop.Reference.ChildToParentReferenceProp.Name})");

                        //  KABU TODO: REVISIT: Currently we will hande cascading deletion ourselves, so turn it off.
                        O(".OnDelete(DeleteBehavior.Restrict);");

                        Pop();
                    }
                }

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                O();
            }

            foreach (var prop in unidirectionalManyToManyProps)
            {
                var type = GetManyToManyClassName(prop);
                var ltype = type.FirstLetterToLower();
                var atype = prop.DeclaringType.Name;
                var aToBList = "To" + prop.Name;

                var aid = atype + "Id";
                var btype = prop.SingleName;
                var bToAList = "To" + prop.DeclaringType.PluralName;

                var bid = btype + "Id";

                O($"// Many-to-many: {type}");
                O($"var {ltype} = builder.Entity<{type}>();");
                O($"{ltype}.HasKey(\"{aid}\", \"{bid}\");");
                O($"{ltype}.HasOne(ab => ab.{atype}).WithMany(a => a.{aToBList}).HasForeignKey(ab => ab.{aid});");
                O($"{ltype}.HasOne(ab => ab.{btype}).WithMany().HasForeignKey(ab => ab.{bid});");
                // TODO: This would be for bidirectional many-to-many: 
                // O($"{ltype}.HasOne(ab => ab.{btype}).WithMany(b => b.{aToList}).HasForeignKey(ab => ab.{bid});");
                O();
#if (false)
                var a2b = builder.Entity<A2B>();
                a2b.HasKey("AID", "BID");

                a2b.HasOne(ab => ab.A)
                    .WithMany(a => a.A2B)
                    .HasForeignKey(ab => ab.AID);

                a2b.HasOne(ab => ab.B)
                    .WithMany(b => b.A2B)
                    .HasForeignKey(ab => ab.BID);
#endif

            }

            O();
            O("OnModelCreatingApplyDecimalPrecision(builder);");

            End(); // CreateModel method

            End(); // DbContext class

            // Generate many-to-many joining classes.
            foreach (var prop in unidirectionalManyToManyProps)
            {
                var atype = prop.DeclaringType.Name;
                var aid = atype + "Id";
                var btype = prop.Reference.ToType.Name;
                var btypeProp = prop.SingleName;
                var btypePlural = prop.Reference.ToType.PluralName;
                var bid = btype + "Id";

                OSummary($"Unidirectional many-to-many join class ({atype}.To{prop.Name} -> {atype}.{btype}).");

                OB($"public class {GetManyToManyClassName(prop)}");
                O($"public Guid {aid} {{ get; set; }}");
                O($"public virtual {atype} {atype} {{ get; set; }}");
                O();
                O($"public Guid {bid} {{ get; set; }}");
                O($"public virtual {btype} {btypeProp} {{ get; set; }}");
                End();

                // public class A2B
                // {
                //    public Guid AID { get; set; }
                //    public A A { get; set; }
                //    public Guid BID { get; set; }
                //    public B B { get; set; }
                // }
            }

            End(); // ns
        }
    }
}
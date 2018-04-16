using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DbRepoCoreOnIsDeletedCascadeGen : DbRepoCoreGenBase
    {
        public DbRepoCoreOnIsDeletedCascadeGen()
        {
            Scope = "DataContext";
            Name = "OnIsDeleted.Cascade";

            AnyTypeMethodCall = (o) => $"void OnCascadeAny({o.DataConfig.DbRepoOperationContextName} ctx)";
            AnyTypeMethodFilter = () => "if (!GetProp(ctx.Item, \"IsDeleted\", false)) return;";

            TypeMethodCall = (o, type) => $"OnCascade(ctx.Item as {type.ClassName}, ctx);";
            TypeMethod = (o, type, item) => $"bool OnCascade({type.ClassName} {item}, {o.DataConfig.DbRepoOperationContextName} ctx)";
            UseRepositoriesContext = false;

            SelectTypes = TypeSelector;
        }

        static readonly MojPropDeletedMarker[] DeleteMarkers = new[] {
            MojPropDeletedMarker.Cascade,
            MojPropDeletedMarker.Self,
            MojPropDeletedMarker.Effective
        };

        IEnumerable<DbRepoCoreGenItem> TypeSelector(IEnumerable<MojType> types)
        {
            foreach (var type in types)
            {
                var item = new DbRepoCoreGenItem(type)
                {
                    Props = type.GetReferenceProps()
                        .Where(ObjectTreeHelper.IsReferenceToChild)
                        // Ignore if soft delete cascade was disabled for this reference property.
                        .Where(x => x.Reference.IsSoftDeleteCascadeDisabled == false)
                        // KABU TODO: IMPORTANT: Disabled for now because I want to see if we have
                        //   scenarios where there's no delete-info on children.
                        //.Where(x => x.Reference.ToType.FindDeletedMarker(DeleteMarkers) != null)
                        .OrderBy(x => x.Reference.Binding)
                        .ThenBy(x => x.Reference.Multiplicity)
                        .ToArray(),

                    SoftReferences = types
                        //.Where(t => t.SoftReferences.Any(r => ObjectTreeHelper.IsReferenceToParent(type, r)))
                        .Select(t => new DbRepoCoreGenSoftRefItem
                        {
                            ChildType = t,
                            References = t.SoftReferences.Where(r => ObjectTreeHelper.IsReferenceToParent(type, r)).ToArray()
                        })
                        .Where(x => x.References.Any())
                        .ToArray()
                };

                if (item.Props.Any() || item.SoftReferences.Any())
                    yield return item;
            }
        }

        public override void OProp()
        {
            var item = Current.Item;
            var type = Current.Type;
            var prop = Current.Prop;
            var targetType = prop.Reference.ToType;
            var targetKey = targetType.Key.Name;
            var target = targetType.VName;
            if (item == target)
                target += "2";
            var targetRepo = targetType.PluralName;
            var targetDeletedMarkerProp = targetType.FindDeletedMarker(DeleteMarkers);

            if (prop.Reference.IsToOne)
            {
                prop = prop.ForeignKey;

                if (targetDeletedMarkerProp != null)
                {
                    O($"if ({item}.{prop.Name} != null)");
                    Begin();

                    O($"var {target} = ctx.Repos.{targetRepo}.Find({item}.{prop.Name}.Value);");

                    O($"if (ProcessCascadeItem({target}, ctx))");
                    O($"    ctx.Repos.{targetRepo}.Update(ctx.CreateSubUpdateOperation({target}));");

                    End();
                    O();
                }
                else
                {
                    O($"// ## Missing deleted marker property for child type '{targetType.ClassName}'.");
                }
            }
            else if (prop.Reference.IsToMany)
            {
                O($"foreach (var {target} in ctx.Repos.{targetRepo}.LocalAndQuery(x => x.{prop.Reference.ItemToCollectionProp.ForeignKey.Name} == {item}.{type.Key.Name}))");
                O($"    if (ProcessCascadeItem({target}, ctx))");
                O($"        ctx.Repos.{targetRepo}.Update(ctx.CreateSubUpdateOperation({target}));");

                O();
            }
            else throw new MojenException($"Unexpected multiplicity '{prop.Reference.Multiplicity}'.");
        }

        public override void OSoftReference()
        {
            var item = Current.ReferenceItem;
            var targetType = item.ChildType;
            //var targetTypeItem = typeItems.First(x => x.Type == targetType);
            var targetRepo = targetType.PluralName;

            var pick = targetType.FindPick();
            var valueDisplayPropName = pick != null ? targetType.GetProp(pick.DisplayProp).Name : null;

            foreach (var reference in item.References)
            {
                if (reference.IsCollection)
                {
                    O($"// Soft child collection of {targetType.ClassName}: {reference.Axis}, {reference.Binding}, {reference.Multiplicity}");

                    O($"foreach (var child in ctx.Repos.{targetRepo}.LocalAndQuery(x => {Mex.ToLinqPredicate(reference.Condition)}))");
                    O($"    if (ProcessCascadeItem(child, ctx))");
                    O($"        ctx.Repos.{targetRepo}.Update(ctx.CreateSubUpdateOperation(child));");
                }
                else
                {
                    O($"// Soft single child {targetType.ClassName}: {reference.Axis}, {reference.Binding}, {reference.Multiplicity}");

                    var target = targetType.VName;

                    O($"var {target} = ctx.Repos.{targetRepo}.LocalAndQuery(x => {Mex.ToLinqPredicate(reference.Condition)}).FirstOrDefault();");
                    O($"if (ProcessCascadeItem({target}, ctx))");
                    O($"    ctx.Repos.{targetRepo}.Update(ctx.CreateSubUpdateOperation({target}));");
                }
            }
        }
    }
}
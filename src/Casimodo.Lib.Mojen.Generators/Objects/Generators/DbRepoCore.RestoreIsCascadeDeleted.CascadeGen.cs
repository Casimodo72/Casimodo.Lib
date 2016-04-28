using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DbRepoCoreOnRestoreIsCascadeDeletedCascadeGen : DbRepoCoreGenBase
    {
        public DbRepoCoreOnRestoreIsCascadeDeletedCascadeGen()
        {
            Scope = "DataContext";
            Name = "RestoreCascadeDeleted.Cascade";

            AnyTypeMethodCall = (o) => $"void OnRestoreCascadeDeletedAny({o.DataConfig.DbRepoOperationContextName} ctx)";            

            TypeMethodCall = (o, type) => $"OnRestoreCascadeDeleted(ctx.Item as {type.ClassName}, ctx);";
            TypeMethod = (o, type, item) => $"bool OnRestoreCascadeDeleted({type.ClassName} {item}, {o.DataConfig.DbRepoOperationContextName} ctx)";
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
                        // KABU TODO: IMPORTANT: Disabled for now because I want to see if we have
                        //   scenarios where there's no delete-info on children.
                        //.Where(x => x.Reference.ToType.FindDeletedMarker(DeleteMarkers) != null)
                        .OrderBy(x => x.Reference.Binding)
                        .ThenBy(x => x.Reference.Cardinality)
                        .ToArray(),

                    SoftReferences = types                        
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

                    O($"if (IsCascadeDeletedByOrigin({target}, ctx))");
                    O($"    RestoreCascadeDeleted(ctx.SubRestoreCascadeDeleted({target}));");

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
                O($"foreach (var {target} in ctx.Repos.{targetRepo}.LocalAndQuery(true, x => x.{prop.Reference.ChildToParentProp.ForeignKey.Name} == {item}.{type.Key.Name}))");
                O($"    if (IsCascadeDeletedByOrigin({target}, ctx))");
                O($"        RestoreCascadeDeleted(ctx.SubRestoreCascadeDeleted({target}));");

                O();
            }
            else throw new MojenException($"Unexpected cardinality '{prop.Reference.Cardinality}'.");
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
                    O($"// Soft child collection of {targetType.ClassName}: {reference.Axis}, {reference.Binding}, {reference.Cardinality}");

                    O($"foreach (var child in ctx.Repos.{targetRepo}.LocalAndQuery(true, x => {Mex.ToLinqPredicate(reference.Condition)}))");
                    O($"    if (IsCascadeDeletedByOrigin(child, ctx))");
                    O($"        RestoreCascadeDeleted(ctx.SubRestoreCascadeDeleted(child));");
                }
                else
                {
                    O($"// Soft single child {targetType.ClassName}: {reference.Axis}, {reference.Binding}, {reference.Cardinality}");

                    var target = targetType.VName;

                    O($"var {target} = ctx.Repos.{targetRepo}.LocalAndQuery(true, x => {Mex.ToLinqPredicate(reference.Condition)}).FirstOrDefault();");
                    O($"if (IsCascadeDeletedByOrigin({target}, ctx))");
                    O($"    RestoreCascadeDeleted(ctx.SubRestoreCascadeDeleted({target}));");
                }
            }
        }
    }
}
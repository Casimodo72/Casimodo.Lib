using Casimodo.Lib.Data;
using System;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// If a parent object is added then also update its nested-referenced objects.
    /// </summary>
    public class DbRepoCoreOnAddedNestedGen : DbRepoCoreGenBase
    {
        public DbRepoCoreOnAddedNestedGen()
        {
            Scope = "DataContext";

            Name = "OnAdded.Nested";

            AnyTypeMethodCall = (o) => $"void OnAddedNestedAny({o.DataConfig.DbRepoOperationContextName} ctx)";

            TypeMethodCall = (o, type) => $"OnAddedNested(ctx.Item as {type.ClassName}, ctx);";
            TypeMethod = (o, type, item) => $"bool OnAddedNested({type.ClassName} {item}, {o.DataConfig.DbRepoOperationContextName} ctx)";
            UseRepositoriesContext = false;

            SelectTypes = (types) => types.Select(t => new DbRepoCoreGenItem(t)
            {
                Props = SelectProps(t).Where(x =>
                    x.IsNavigation &&
                    x.Reference.Binding.HasFlag(MojReferenceBinding.Nested) &&
                    x.Reference.ToType.IsEntityOrModel())
                    .ToArray()
            })
            .Where(t => t.Props.Any());
        }

        public override void OProp()
        {
            var item = Current.Item;
            var type = Current.Type;
            var prop = Current.Prop;
            var targetType = prop.Reference.ToType;
            var targetRepo = $"ctx.Repos.{targetType.PluralName}";
            var target = FirstCharToLower(targetType.Name);
            if (target == item)
                target += "2";

            // If the navigation property is assigned.
            O($"if ({item}.{prop.Name} != null)");
            Begin();

            if (prop.Reference.IsToOne)
            {
                // Add the nested referenced object.
                O($"SetIsNested({item}.{prop.Name});");
                O($"{targetRepo}.Add(ctx.CreateSubAddOperation({item}.{prop.Name}));");
            }
            else if (prop.Reference.IsToMany && !prop.IsHiddenCollectionNavigationProp)
            {
                // Add the collection of nested referenced objects.
                O($"foreach (var obj in {item}.{prop.Name})");
                Begin();
                O("SetIsNested(obj);");
                O($"{targetRepo}.Add(ctx.CreateSubAddOperation(obj));");
                End();
            }

            End();
            O();
        }
    }
}
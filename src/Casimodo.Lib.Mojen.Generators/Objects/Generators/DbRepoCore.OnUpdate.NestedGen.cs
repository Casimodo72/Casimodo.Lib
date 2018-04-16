using Casimodo.Lib.Data;
using System;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// If a parent object is updated then also update its nested-referenced objects.
    /// </summary>
    public class DbRepoCoreOnUpdateNestedGen : DbRepoCoreGenBase
    {
        public DbRepoCoreOnUpdateNestedGen()
        {
            Scope = "DataContext";

            Name = "OnUpdate.Nested";

            AnyTypeMethodCall = (o) => $"void OnUpdateNestedAny({o.DataConfig.DbRepoOperationContextName} ctx)";

            TypeMethodCall = (o, type) => $"OnUpdateNested(ctx.Item as {type.ClassName}, ctx);";
            TypeMethod = (o, type, item) => $"bool OnUpdateNested({type.ClassName} {item}, {o.DataConfig.DbRepoOperationContextName} ctx)";
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

            var single = prop.Reference.IsToOne;
            var many = prop.Reference.IsToMany;

            // If the navigation property is assigned
            // and if the nested entity does not exist yet.
            Oo($"if ({item}.{prop.Name} != null");

            if (single)
                o($" && {targetRepo}.Exists({item}.{prop.Name}.{targetType.Key.Name})");

            oO(")");

            if (single)
            {
                // Update the nested referenced object.
                O($"    {targetRepo}.Update({item}.{prop.Name});");
            }
            else if (many && !prop.IsHiddenCollectionNavigationProp)
            {
                // Update the collection of nested referenced objects.

                // Example:
                // UpdateCollection<Article, Guid>(job.Articles, (x) => x.ArticleOfJobId == job.Id, (x) => x.Id, context.Articles);

                Oo($"    UpdateNestedCollection<{targetType.ClassName}, {targetType.Key.Type.NameNormalized}>(");
                o($"{item}.{prop.Name}, ");
                o($"(x) => x.{prop.Reference.ItemToCollectionProp.ForeignKey.Name} == {item}.{type.Key.Name}, ");
                o($"(x) => x.{targetType.Key.Name}, ");
                oO($"{targetRepo}, ctx);");
            }

            O();
        }
    }
}
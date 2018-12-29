using Casimodo.Lib.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DbRepoCoreOnDeleteCascadeGen : DbRepoCoreGenBase
    {
        public DbRepoCoreOnDeleteCascadeGen()
        {
            Scope = "DataContext";

            Name = "OnDeleted.Cascade";
            OnAnyTypeMethodName = "OnDeletedCascadeAny";
            OnTypeMethodName = "OnDeletedCascade";

            SelectTypes = (types) => types.Select(t => new DbRepoCoreGenItem(t)
            {
                Props = SelectProps(t).Where(x =>
                      x.Reference.Binding.HasFlag(MojReferenceBinding.Owned) &&
                    (!x.IsNavigation || x.Reference.IsToMany))
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
            var target = FirstCharToLower(targetType.Name);
            if (target == item)
                target += "2";

            if (prop.Reference.IsToOne)
            {
                O($"if ({item}.{prop.Name} != null)");
                Begin();

                if (prop.Reference.IsDeletionOptimized)
                {
                    OComment("Optimized deletion");
                    // var blob = new Blob { Id = parent.DataId.Value };
                    O($"var {target} = new {targetType.Name} {{ {targetType.Key.Name} = {item}.{prop.Name}.Value }};");
                    // db.Entry(blob).State = EntityState.Deleted;
                    O($"db.Entry({target}).State = EntityState.Deleted;");
                }
                else
                {
                    O($"var {target} = context.{targetType.PluralName}.Find({item}.{prop.Name}.Value);");
                    O($"if ({target} != null)");
                    O($"    context.{targetType.PluralName}.Delete({target});");
                }

                End();
                O();
            }
            else if (prop.Reference.IsToMany)
            {
                O($"foreach (var {target} in context.{targetType.PluralName}.Query()" +
                    $".Where(x => x.{prop.Reference.ForeignItemToCollectionProp.ForeignKey.Name} == {item}.{type.Key.Name}))");
                O($"    context.{targetType.PluralName}.Delete({target});");
                O();
            }
            else throw new MojenException($"Unexpected multiplicity {prop.Reference.Multiplicity}");
        }
    }
}
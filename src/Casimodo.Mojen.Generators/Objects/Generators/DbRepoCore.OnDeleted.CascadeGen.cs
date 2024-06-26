﻿using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    public class DbRepoCoreOnDeleteCascadeGen : DbRepoCoreGenBase
    {
        public DbRepoCoreOnDeleteCascadeGen()
        {
            Scope = "DataContext";

            Name = "OnDeleted.Cascade";
            OnAnyTypeMethodName = "OnDeletedCascadeAny";
            OnTypeMethodName = "OnDeletedCascade";

            SelectTypes = types => types.Select(t => new DbRepoCoreGenItem(t)
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

                if (prop.Reference.IsOptimizedDeletion)
                {
                    OComment("Optimized deletion");
                    // var blob = new Blob { Id = parent.DataId.Value };
                    O($"var {target} = new {targetType.Name} {{ {targetType.Key.Name} = {item}.{prop.Name}.Value }};");
                    // db.Entry(blob).State = EntityState.Deleted;
                    O($"db.Entry({target}).State = EntityState.Deleted;");
                }
                else if (prop.Reference.IsMarkedForHardDeletion)
                {
                    OComment("Mark for hard deletion");
                    // var blob = new Blob { Id = parent.DataId.Value, IsMakedForHardDeletion = true };
                    O($"var {target} = new {targetType.Name} {{ {targetType.Key.Name} = {item}.{prop.Name}.Value, IsMarkedForHardDeletion = true }};");
                    // db.Entry(blob).Property(nameof(blob.IsMarkedForHardDeletion)).IsModified = true;
                    O($"db.Entry({target}).Property(nameof({target}.IsMarkedForHardDeletion)).IsModified = true;");
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
                    $".Where(x => x.{prop.Reference.ForeignBackrefProp.ForeignKey.Name} == {item}.{type.Key.Name}))");
                O($"    context.{targetType.PluralName}.Delete({target});");
                O();
            }
            else throw new MojenException($"Unexpected multiplicity {prop.Reference.Multiplicity}");
        }
    }
}
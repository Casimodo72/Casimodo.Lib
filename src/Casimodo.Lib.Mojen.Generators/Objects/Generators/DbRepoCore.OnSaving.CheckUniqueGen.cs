using Casimodo.Lib.Data;
using System;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // KABU TODO: RENAME to ValidateUnique
    public class DbRepoCoreOnSavingCheckUniqueGen : DbRepoCoreGenBase
    {
        public DbRepoCoreOnSavingCheckUniqueGen()
        {
            Scope = "DataContext";

            Name = "OnSaving.CheckUnique";
            OnAnyTypeMethodName = "OnSavingCheckUniqueAny";
            OnTypeMethodName = "OnSavingCheckUnique";
            ItemName = "item";

            SelectTypes = (types) => types.Select(t => new DbRepoCoreGenItem(t)
            {
                Props = SelectProps(t).Where(x => x.DbAnno.Unique.Is).ToArray()
            })
            .Where(t => t.Props.Any());
        }

        // Example:
        // bool OnSavingCheckUnique(ProjectEntity project, DbContext db)
        // {
        //    if (project == null) return false;
        //    var context = new GaDbRepositoriesContext((GaDbContext)db);
        //
        //    if (project.RawNumber == null) ThrowUniquePropValueMustNotBeNull("RawNumber");
        //    if (project.RawNumber < 10000) ThrowUniquePropValueMustNotBeLessThan("RawNumber", 10000);
        //    if (context.Db.Projects.Any(x => x.Id != project.Id && x.RawNumber == project.RawNumber && x.CompanyId == project.CompanyId && x.Year == project.Year))
        //        ThrowUniquePropValueExistsCustom("Die Projekt-Nummer '{0}' ist bereits vergeben.", project.RawNumber);
        //    return true;
        // }

        public override void OProp()
        {
            var item = Current.Item;
            var type = Current.Type;
            var prop = Current.Prop;
            var unique = prop.DbAnno.Unique;
            var sequence = prop.DbAnno.Sequence;

            if (prop.Type.CanBeNull)
            {
                O($"if ({item}.{prop.Name} == null) ThrowUniquePropValueMustNotBeNull<{type.ClassName}>(\"{prop.Name}\");");
                O();
            }

            if (sequence.Is)
            {
                if (sequence.Start != null)
                {                
                    O($"if ({item}.{prop.Name} < {sequence.Start}) ThrowUniquePropValueMustNotBeLessThan<{type.ClassName}>(\"{prop.Name}\", {sequence.Start});");
                    O();
                }

                if (sequence.StartSelector != null)
                {                    
                    var method = prop.GetStartSequenceValueMethodName();
                    var step = sequence.StartSelector.Root;
                    var sourceProp = step.SourceProp.ForeignKey;
                    O($"var min = {method}(db, {item}.{sourceProp.Name});");
                    O($"if (min != default({prop.Type.Name}) && {item}.{prop.Name} < min) ThrowUniquePropValueMustNotBeLessThan<{type.ClassName}>(\"{prop.Name}\", min);");
                    O();
                }

                if (sequence.EndSelector != null)
                {
                    var method = prop.GetEndSequenceValueMethodName();
                    var step = sequence.EndSelector.Root;
                    var sourceProp = step.SourceProp.ForeignKey;
                    O($"var max = {method}(db, {item}.{sourceProp.Name});");
                    O($"if (max != default({prop.Type.Name}) && {item}.{prop.Name} > max) ThrowUniquePropValueMustNotBeGreaterThan<{type.ClassName}>(\"{prop.Name}\", max);");
                    O();
                }
            }

            // Check for unique value.
            var key = type.Key.Name;
            Oo($"if (context.Db.{type.PluralName}.Any(x => x.{key} != {item}.{key}");
            o($" && x.{prop.Name} == {item}.{prop.Name}");
            foreach (var per in unique.GetParams(includeTenant: true))
            {
                o($" && x.{per.Prop.Name} == {item}.{per.Prop.Name}");
            }
            oO("))");
            if (!string.IsNullOrEmpty(unique.ErrorMessage))
                O($"    ThrowUniquePropValueExistsCustom<{type.ClassName}>(\"{prop.Name}\", {item}.{prop.Name}, \"{unique.ErrorMessage}\");");
            else
                O($"    ThrowUniquePropValueExists<{type.ClassName}>(\"{prop.Name}\", {item}.{prop.Name});");
        }

        public override void OHelpers()
        { }
    }
}
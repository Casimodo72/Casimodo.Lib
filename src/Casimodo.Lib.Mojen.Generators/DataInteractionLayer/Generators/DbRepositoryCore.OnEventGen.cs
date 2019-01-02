using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// If a parent object is updated then also update its nested referenced objects.
    /// </summary>
    public sealed class DbRepositoryCoreOnEventGen : DbRepoCoreGenBase
    {
        public DbRepositoryCoreOnEventGen()
        {
            Name = "OnEvent";
            OnAnyTypeMethodName = "OnEventAny";
            OnTypeMethodName = "OnEvent";

            SelectTypes = (types) => types
                .Where(t => FilterTriggers(t.Triggers).Count() != 0)
                .Select(t => new DbRepoCoreGenItem(t));
        }

        IEnumerable<MiaTypeTriggerConfig> FilterTriggers(IEnumerable<MiaTypeTriggerConfig> triggers)
        {
            return triggers.Where(x =>
                x.CrudOp != MojCrudOp.None &&
                x.ForScenario.HasFlag(MiaTriggerScenario.Repository));
        }

        public override void OForAllTypes()
        {
            OClassStart();

            O($"void {OnAnyTypeMethodName}(object item, DbContext db, MojCrudOp e)");
            Begin();

            O($"if (item == null) return;");

            var types = GetItems().Select(x => x.Type).ToArray();
            foreach (var type in types)
            {
                O($"// {type.Name}");
                foreach (var operation in FilterTriggers(type.Triggers))
                {
                    // E.g. OnCreateContract_UpdateProject(...)
                    // O($"{GetMethodName(operation)}(item as {type.ClassName}, db, e);");
                    O($"{GetMethodName(operation)}(item, db, e);");
                }
                O();
            }

            End();
            O();

            var ctx = new DbRepoCoreGenContext();

            foreach (var type in types)
            {
                string item = FirstCharToLower(type.Name);

                OCommentSection(type.Name);
                O();

                foreach (var operation in FilterTriggers(type.Triggers))
                {
                    O($"bool {GetMethodName(operation)}(object item, DbContext db, MojCrudOp e)");
                    Begin();
                    O($"if (e != MojCrudOp.{operation.Event} || !(item is {type.ClassName})) return false;");
                    O($"var {item} = ({type.ClassName})item;");
                    O($"var context = new {DataConfig.DbRepoContainerName}(({DataConfig.DbContextName})db);");
                    O();

                    Generate(operation);

                    O("return true;");
                    End();
                    O();
                }
            }

            OClassEnd();
        }

        /// <summary>
        /// Returns e.g. OnCreateContract_UpdateProject(...)
        /// </summary>        
        string GetMethodName(MiaTypeTriggerConfig operation)
        {
            return "On" +
                operation.Event + operation.ContextType.Name +
                "_" +
                operation.CrudOp + operation.TargetType.Name +
                (operation.Name ?? "");
        }

        void Generate(MiaTypeTriggerConfig trigger)
        {
            MojType type = trigger.ContextType;
            string item = type.VName;
            var targetType = trigger.TargetType;
            var target = targetType.VName;
            string repository = $"context.{targetType.PluralName}";

            GenerateCore(trigger, (toRefProp, fromRefProp) =>
            {
                if (trigger.CrudOp == MojCrudOp.Delete)
                {
                    O($"{repository}.Delete({target});");
                }
                else if (trigger.CrudOp == MojCrudOp.Create)
                {
                    if (trigger.Multiplicity != MojMultiplicity.One)
                        throw new MojenException($"Invalid multiplicity '{trigger.Multiplicity}' for this operation.");

                    if (trigger.Operations.FactoryFunctionCall != null)
                    {
                        var func = trigger.Operations.FactoryFunctionCall;
                        if (!func.EndsWith(")"))
                            func += "()";
                        O($"var {target} = {func};");
                    }
                    else
                    {
                        O($"var {target} = new {targetType.ClassName}();");
                        // Generate ID
                        O($"({target} as IGuidGenerateable).GenerateGuid();");
                    }

                    // Create entity
                    if (trigger.Operations.MappingFunctionName != null)
                    {
                        // Map using the given function.
                        O($"{trigger.Operations.MappingFunctionName}({item}, {target}, context);");
                    }
                    else
                    {
                        // Map properties
                        foreach (var map in trigger.Operations.Items.OfType<MiaPropSetterConfig>())
                        {
                            O($"{target}.{map.Target.FormedTargetPath} = {item}.{map.Source.FormedTargetPath};");
                        }

                        // Apply DB sequences
                        foreach (var sequenceProp in targetType.GetProps().Where(x => x.DbAnno.Sequence.IsDbSequence))
                        {
                            O("{0}.{1} = {2}.{3};", target, sequenceProp.Name, repository, GetDbSequenceFunction(sequenceProp));
                        }

                        // Set foreign keys
                        if (toRefProp != null)
                            O("{0}.{1} = {2}.{3};", item, toRefProp.Reference.ForeignKey.Name, target, targetType.Key.Name);

                        if (fromRefProp != null)
                            O("{0}.{1} = {2}.{3};", target, fromRefProp.Reference.ForeignKey.Name, item, type.Key.Name);
                    }

                    // Add to DB
                    O();
                    O($"{repository}.Add({target});");
                }
                else if (trigger.CrudOp == MojCrudOp.Update)
                {
                    // Map properties
                    foreach (var map in trigger.Operations.Items.OfType<MiaPropSetterConfig>())
                    {
                        O($"{target}.{map.Target.FormedTargetPath} = {item}.{map.Source.FormedTargetPath};");
                    }

                    // Update
                    O();
                    O($"{repository}.Update({target});");
                }
            });
        }

        void GenerateCore(MiaTypeTriggerConfig trigger, Action<MojProp, MojProp> build)
        {
            CheckMultiplicityNotEmpty(trigger.Multiplicity);

            MojType type = trigger.ContextType;
            string item = FirstCharToLower(type.Name);
            var targetType = trigger.TargetType;
            var target = FirstCharToLower(targetType.Name);
            string repository = $"context.{targetType.PluralName}";

            // Get the reference/navigation properties

            // KABU TODO: FUTURE: This will become more complex when we will try to handle
            //   types with multiple reference properties to the same target type.

            // Scenarios:
            // 1) There is only a foreign key from target to source.
            // 2) There is only a foreign key from source to target.
            // 3) There is a navigation property from source to target.
            // 3.a) Navigation property has multiplicity of One (or zero)
            // 3.b) Navigation property has multiplicity of Many (or zero)
            // 3.b.1) There must have a back-reference property on the target type.
            // 4) There is a navigation property from target to source.
            // 4.a) Here we only allow a multiplicity of One (or zero) to the source type.

            var to = type.FindReferenceWithForeignKey(targetType);
            var from = targetType.FindReferenceWithForeignKey(type);

            // KABU TODO: IMPORTANT: What to do if the foreign key itself changes?
            // KABU TODO: IMPORTANT: What to do if the related entity does not exist,
            //   no relationship was established yet? This can be the case
            //   if the relationship is optional.

            if (to == null && from == null &&
                trigger.Operations.FactoryFunctionCall == null &&
                trigger.Operations.MappingFunctionName == null)
            {
                throw new MojenException($"There is no relationship between the types '{type.ClassName}' and '{targetType.ClassName}'.");
            }

            if (trigger.CrudOp == MojCrudOp.Create)
            {
                build(to, from);
            }
            else if (trigger.Multiplicity == MojMultiplicity.One)
            {
                if (to != null)
                    O($"if ({item}.{to.Reference.ForeignKey.Name} == null) return true;");

                O();
                if (to != null)
                    O($"var {target} = {repository}.Find({item}.{to.Reference.ForeignKey.Name}.Value);");
                else
                    O($"var {target} = {repository}.Query(true).SingleOrDefault(x => x.{from.Reference.ForeignKey.Name} == {item}.{type.Key.Name});");

                O($"if ({target} == null) return true;");
                O();

                build(to, from);
            }
            else if (trigger.Multiplicity == MojMultiplicity.Many)
            {
                if (to != null)
                {
                    if (to.Reference.ForeignBackrefProp == null)
                        throw new MojenException($"The expected back-reference property is missing.");

                    O($"foreach (var {target} in {repository}.Query(true).Where(x => x.{to.Reference.ForeignBackrefProp.ForeignKey.Name} == {item}.{type.Key.Name}).ToArray())");
                }
                else
                    O($"foreach (var {target} in {repository}.Query(true).Where(x => x.{from.Reference.ForeignKey.Name} == {item}.{type.Key.Name}).ToArray())");

                Begin();

                build(to, from);

                End();
            }
        }

        void CheckMultiplicityNotEmpty(MojMultiplicity multiplicity)
        {
            if (multiplicity != MojMultiplicity.One &&
                multiplicity != MojMultiplicity.Many)
                throw new MojenException($"Invalid multiplicity '{multiplicity}' for this operation.");
        }
    }
}
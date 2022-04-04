namespace Casimodo.Lib.Mojen
{
    // KABU TODO: EF Core: Use native support for sequences.
    //    See: https://docs.microsoft.com/en-us/ef/core/modeling/relational/sequences

    public class DbRepoCoreGetNextSequenceValueGen : DbRepoCoreGenBase
    {
        public DbRepoCoreGetNextSequenceValueGen()
        {
            Scope = "DataContext";

            Name = "GetNextSequenceValue";
            ItemName = "item";

            // Select types which have sequence props with parameterized constraints or start value selectors.
            SelectTypes = types => types.Select(t => new DbRepoCoreGenItem(t)
            {
                Props = SelectProps(t).Where(prop =>
                    prop.DbAnno.Sequence.Is &&
                    prop.DbAnno.Unique.HasParams)
                    .ToArray()
            })
            .Where(t => t.Props.Any());
        }

        public override void OForAllTypes()
        {
            // Example:
            // public int GetNextSequenceValueForProjectRawNumber(DbContext db, ProjectEntity project)
            // {
            //    return GetNextSequenceValueForProjectRawNumber(db, project.CompanyId, project.Year);
            // }
            //
            // public int GetNextSequenceValueForProjectRawNumber(DbContext db, Guid? companyId, int? year)
            // {
            //    Guard.ArgNotNull(companyId, nameof(companyId));
            //    Guard.ArgNotNull(year, nameof(year));
            //
            //    var value = ((GaDbContext)db).Projects
            //        .Where(x => x.CompanyId == companyId && x.Year == year)
            //        .GroupBy(x => new { x.CompanyId, x.Year })
            //        .Select(grp => grp.Max(x => x.RawNumber))
            //        .FirstOrDefault();
            //
            //    if (value == null)
            //    {
            //        value = GetStartSequenceValueForProjectRawNumber(db, companyId);
            //    }
            //
            //    if (value == null || value.Value < 1) ThrowFailedToGetNextSequenceValue("RawNumber");
            //
            //    return value.Value;
            // }
            //
            // public int? GetStartSequenceValueForProjectRawNumber(DbContext db, Guid? companyId)
            // {
            //    Guard.ArgNotNull(companyId, nameof(companyId));
            //
            //    // Select start value from Company.ProjectNumberSequenceStart.
            //    return ((GaDbContext)db).Companies
            //        .Where(x => x.Id == companyId)
            //        .Select(x => x.ProjectNumberSequenceStart)
            //        .SingleOrDefault();
            //
            // }

            OClassStart();

            var typeItems = GetItems().ToArray();
            foreach (var typeItem in typeItems)
            {
                var type = typeItem.Type;
                foreach (var prop in typeItem.Props)
                {
                    var parameters = prop.DbAnno.Unique.GetParams(includeTenant: true).ToArray();
                    var method = prop.GetNextSequenceValueMethodName();
                    var item = type.VName;

                    // First method ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    OCommentSection($"{type.Name}.{prop.Name}");
                    O($"public {prop.Type.NameNormalized} {method}(DbContext db, {type.ClassName} {item})");
                    Begin();
                    Oo($"return {method}(db, ");
                    o(parameters.Select(p => $"{item}.{p.Prop.Name}").Join(", "));
                    oO(");");
                    End();

                    // Second method ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    O();
                    Oo($"public {prop.Type.NameNormalized} {method}(DbContext db, ");
                    o(parameters.Select(p => $"{p.Prop.Type.Name} {p.Prop.VName}").Join(", "));
                    oO(")");
                    Begin();

                    // Check nulls.
                    var nullables = parameters.Where(x => x.Prop.Type.CanBeNull).ToArray();
                    if (nullables.Any())
                    {
                        foreach (var p in nullables)
                            O("Guard.ArgNotNull({0}, nameof({0})); ", p.Prop.VName);
                        O();
                    }

                    O($"var value = (({DataConfig.DbContextName})db).{type.PluralName}");

                    var uniquePers = prop.DbAnno.Unique.GetMembers().ToArray();

                    Oo($"    .Where(x => ");
                    o(uniquePers.Select(per => $"x.{per.Prop.Name} == {per.Prop.VName}").Join(" && "));
                    oO(")");

                    Oo("    .GroupBy(x => new { ");
                    o(uniquePers.Select(per => $"x.{per.Prop.Name}").Join(", "));
                    oO(" })");

                    O($"    .Select(grp => grp.Max(x => x.{prop.Name}))");

                    O($"    .FirstOrDefault();");

                    O();
                    O($"if (value == default({prop.Type.Name}))");

                    // There is no entity in this group yet. We need to use a start value.
                    // If a start selector was defined then select the start value,
                    // otherwise use the start value integer.
                    if (prop.DbAnno.Sequence.StartSelector != null)
                    {
                        method = prop.GetStartSequenceValueMethodName();
                        var foreignKey = prop.DbAnno.Sequence.StartSelector.Root.SourceProp.ForeignKey;
                        O($"    value = {method}(db, {foreignKey.VName});");
                    }
                    else
                    {                        
                        O($"    value = {prop.DbAnno.Sequence.Start};");
                    }
                    O("else");
                    // Increase the current max number.
                    O($"    value += 1;");

                    O();
                    O($"if (value == default({prop.Type.Name}) || value.Value < {prop.DbAnno.Sequence.Min}) ThrowFailedToGetNextSequenceValue(\"{prop.Name}\");");

                    O();
                    O("return value.Value;");

                    End();

                    // Start/end sequence value query ~~~~~~~~~~~~~~~~~~~~~~~~~

                    var sequence = prop.DbAnno.Sequence;
                    if (sequence.StartSelector != null)
                    {
                        OGetStartEndSequenceValue(prop.GetStartSequenceValueMethodName(), sequence.StartSelector);
                    }

                    if (sequence.EndSelector != null)
                    {
                        OGetStartEndSequenceValue(prop.GetEndSequenceValueMethodName(), sequence.EndSelector);
                    }

                    O();
                }
            }

            OHelpers();

            OClassEnd();
        }

        void OGetStartEndSequenceValue(string method, MojFormedNavigationPath selector)
        {
            var step = selector.Root;
            var sourceProp = step.SourceProp.ForeignKey;
            var targetProp = step.TargetProp;

            O();
            Oo($"public {targetProp.Type.Name} {method}(DbContext db, ");
            o($"{sourceProp.Type.Name} {sourceProp.VName}");
            oO(")");
            Begin();

            if (sourceProp.Type.CanBeNull)
            {
                O("Guard.ArgNotNull({0}, nameof({0}));", sourceProp.VName);
                O();
            }

            O($"// Select value from {step.TargetType.Name}.{targetProp.Name}.");
            O($"return (({DataConfig.DbContextName})db).{step.TargetType.PluralName}");
            O($"    .Where(x => x.{step.TargetType.Key.Name} == {sourceProp.VName})");
            O($"    .Select(x => x.{targetProp.Name})");
            O($"    .SingleOrDefault();");
            O();

            End();
        }

        public override void OHelpers()
        {
            O($"void ThrowFailedToGetNextSequenceValue(string prop)");
            Begin();
            // KABU TODO: LOCALIZE
            OThrowRepoException(() => oQuote("Der nächste Wert für '{prop}' konnte nicht ermittelt werden."));
            End();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // NOTE: Not used anymore. We use the DbMigrationSeedGen instead.
    public class EntitySeedGen : DataLayerGenerator
    {
        public EntitySeedGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            var outputDirPath = App.Get<DataLayerConfig>().DbSeedDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var seederClassName = App.Get<DataLayerConfig>().DbSeederName;
            if (string.IsNullOrWhiteSpace(seederClassName))
                return;

            var types = App.GetTopTypes().Where(x => x.Seedings.Count != 0).ToArray();
            if (!types.Any())
                return;

            // Write seed container.
            PerformWrite(Path.Combine(outputDirPath, string.Format("{0}.generated.cs", seederClassName)),
                () => GenerateSeedContainer(types));

            // Generate seed file for each type.
            foreach (var type in types)
            {
                PerformWrite(Path.Combine(outputDirPath, string.Format("{0}.{1}.generated.cs", seederClassName, type.PluralName)),
                    () => GenerateSeed(type));
            }
        }

        public void GenerateSeedContainer(MojType[] types)
        {
            OUsing("System", "Casimodo.Lib.Data");
            O();
            ONamespace(types.First().Namespace);
            O("partial class {0}", App.Get<DataLayerConfig>().DbSeederName);
            Begin();
            O("void SeedCore()");
            Begin();
            foreach (var type in types)
                O("Seed{0}();", type.PluralName);
            End();
            End();
            End();
        }

        public void GenerateSeed(MojType type)
        {
            if (type.Seedings.Count == 0)
                return;

            MojType entity = type.Kind == MojTypeKind.Entity ? type : type.Store;

            OUsing("System", "Casimodo.Lib.Data");
            O();
            ONamespace(type.Namespace);
            O("partial class {0}", App.Get<DataLayerConfig>().DbSeederName);
            Begin();

            O("void Seed{0}()", type.PluralName);
            Begin();

            var assignments = new List<string>();
            foreach (MojValueSetContainer valueSetContainer in type.Seedings)
            {
                foreach (MojValueSet valueSet in valueSetContainer.Items)
                {
                    assignments.Clear();
                    Oo("Add(new {0} {{ ", entity.ClassName);

                    foreach (MojValueSetProp vprop in valueSet.Values)
                    {
                        var prop = type.FindProp(vprop.Name, custom: true);
                        if (prop == null)
                        {
                            if (valueSetContainer.IgnoredSeedMappings.Contains(vprop.Name))
                                continue;

                            throw new MojenException(
                                string.Format("Value -> property mapping not found for '{0}'.", vprop.Name));
                        }

                        // If seeding a model with an underlying store, then map to the underlying
                        // store's property.
                        prop = type.FindStoreProp(prop.Name, custom: true);
                        if (prop == null)
                            throw new MojenException(
                                string.Format("Value -> store property mapping not found for '{0}'.", vprop.Name));

                        assignments.Add(string.Format("{0} = {1}{2}",
                            prop.Name,
                            MojenUtils.GetValueCast(prop),
                            MojenUtils.TOValue(vprop.Value, parse: true)));
                    }

                    if (assignments.Any())
                        o(assignments.Join(", "));

                    Oo(" });" + Environment.NewLine);
                }
            }
            End();
            O();

            // Generate add method.
            O("void Add({0} item)", entity.ClassName);
            Begin();
            O("Context.{0}.Add(item);", entity.PluralName);
            // Set any non-nullable DateTimes to DateTime.Now, otherwise
            // SQL Server will bark about not being able to convert from datetime to datetime2.
            var fixDateTimeProps = entity.GetProps().Where(x => !x.Type.IsNullableValueType && x.Type.TypeNormalized == typeof(DateTimeOffset)).ToArray();
            if (fixDateTimeProps.Any())
            {
                O();
                foreach (var prop in fixDateTimeProps)
                    O("item.{0} = SeedTime;", prop.Name);
            }
            End();

            End();
            End();
        }
    }
}
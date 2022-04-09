using System.Globalization;
using System.IO;

namespace Casimodo.Lib.Mojen
{
    public class DbMigrationSeedGen : DbSeedGenBase
    {
        public DbMigrationSeedGen()
        {
            Scope = "DataContext";
        }

        protected override void GenerateCore()
        {
            var outputDirPath = DataConfig.DbSeedRegistryDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = GetSeedItems();
            if (!items.Any())
                return;

            // Write seed container.
            PerformWrite(Path.Combine(outputDirPath, "Seed." + DataConfig.DbContextName + ".generated.cs"),
                () => GenerateSeedContainer(items));

            // Generate seed file for each type.
            outputDirPath = DataConfig.DbSeedDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            foreach (var item in items)
            {
                item.Type.CheckRequiredStore();

                PerformWrite(Path.Combine(outputDirPath, string.Format("Seed.{0}.generated.cs", item.Type.PluralName)),
                    () => GenerateSeed(item));
            }

            return;
            // ---

            //var types = App.GetTopTypes().Where(x => x.Seedings.Count != 0).ToArray();
            //if (!types.Any())
            //    return;

            //// Write seed container.
            //PerformWrite(Path.Combine(outputDirPath, "Seed." + DataConfig.DbContextName + ".generated.cs"),
            //    () => GenerateSeedContainer(types));

            //// Generate seed file for each type.
            //outputDirPath = DataConfig.DbSeedDirPath;
            //if (string.IsNullOrWhiteSpace(outputDirPath))
            //    return;

            //foreach (var type in types)
            //{
            //    type.CheckRequiredStore();

            //    PerformWrite(Path.Combine(outputDirPath, string.Format("Seed.{0}.generated.cs", type.PluralName)),
            //        () => GenerateSeed(type));
            //}
        }

        void GenerateSeedContainer(List<SeedGenItem> items)
        {
            OUsing("System", "System.Globalization", "Casimodo.Lib.Data",
                items.First().Type.Namespace);

            ONamespace(DataConfig.DataNamespace + ".Migrations");

            O("partial class DbMigrationSeed : DbSeedBase");
            Begin();
            O($"public {DataConfig.DbContextName} Context {{ get; set; }}");
            O();
            O($"public void Seed({DataConfig.DbContextName} context)");
            Begin();
            O("if (!IsEnabled) return;");
            O("Context = context;");
            O($"SeedTime = DateTimeOffset.Parse(\"{App.Now.ToString(CultureInfo.InvariantCulture)}\", CultureInfo.InvariantCulture);");
            O();
            foreach (var item in items)
            {
                var enabled = item.Seedings.All(x => x.IsDbSeedEnabled);
                O($"{(enabled ? "" : "// DISABLED: ")}Seed{item.Type.PluralName}();");
            }
            End();
            End();
            End();
        }

        void GenerateSeed(SeedGenItem item)
        {
            if (item.Seedings.Count == 0)
                return;

            var type = item.Type;

            MojType storeType = type.Kind == MojTypeKind.Entity ? type : type.GetNearestStore();

            OUsing("System", "System.Linq",
                "System.Data.Entity",
                "System.Data.Entity.Migrations",
                "Casimodo.Lib.Data",
                (DataConfig.DataNamespace != storeType.Namespace ? storeType.Namespace : null));
            ONamespace(DataConfig.DataNamespace + ".Migrations");
            O("partial class DbMigrationSeed");
            Begin();
            O($"void Seed{type.PluralName}()");
            Begin();

            var assignments = new List<string>();
            foreach (MojValueSetContainer valueSetContainer in item.Seedings)
            {
                var userNameProp = type.FindStoreProp("UserName");
                if (valueSetContainer.ClearExistingData)
                {
                    O($"Context.Database.ExecuteSqlCommand(\"delete from [{type.TableName}]\");");
                    O();
                }

                foreach (MojValueSet valueSet in valueSetContainer.Items)
                {
                    assignments.Clear();
                    Oo($"Add(new {storeType.ClassName} {{ ");

                    foreach (MojValueSetProp val in valueSet.Values)
                    {
                        var prop = type.FindProp(val.Name);
                        if (prop == null)
                        {
                            // KABU TODO: REMOVE
                            //if (valueSetContainer.IgnoredSeedMappings.Contains(val.Name))
                            //    continue;

                            throw new MojenException(
                                string.Format("Value -> property mapping not found for '{0}'.", val.Name));
                        }

                        // If seeding a model with an underlying store, then map to the underlying
                        // store's property.
                        prop = type.FindStoreProp(prop.Name);
                        if (prop == null)
                            throw new MojenException(
                                string.Format("Value -> store property mapping not found for '{0}'.", val.Name));

                        if (val.Kind == "FileName")
                        {
                            // Read and set file content.
                            var filePath = DataConfig.DbSeedBinariesDirPath[(DataConfig.DbDirPath.Length + 1)..];
                            filePath += "\\" + val.Value;
                            assignments.Add(string.Format("{0} = ReadFileContent(@\"{1}\")",
                                prop.Name,
                                filePath));
                        }
                        else
                        {
                            assignments.Add(string.Format("{0} = {1}{2}",
                                prop.Name,
                                Moj.GetCsCast(prop),
                                Moj.CS(val.Value, parse: true, verbatim: true)));
                        }
                    }

                    // Property value assignments
                    if (assignments.Any())
                        o(assignments.Join(", "));

                    o(" }");

                    // Roles
                    if (valueSet.AuthRoles.Any())
                        o($", roles: \"{valueSet.AuthRoles.Join(", ")}\"");

                    // Pw
                    if (valueSet.Pw != null)
                        o($", pw: \"{valueSet.Pw}\"");

                    o(");");

                    if (userNameProp != null)
                    {
                        var userNameVal = valueSet.Values.FirstOrDefault(x => x.Name == "UserName");
                        if (userNameVal != null)
                        {
                            o(" // " + userNameVal.ValueToString());
                        }
                    }

                    Br();
                }
            }
            End();
            O();

            // KABU TODO: This is a temporary hack to allow for custom seeding of identity users.
            if (storeType.Name != "User")
            {
                // Generate add method.
                O($"public void Add({storeType.ClassName} item)");
                Begin();
                // Set any non-nullable DateTimes to DateTime.Now, otherwise
                // SQL Server will bark about not being able to convert from datetime to datetime2.
                var fixDateTimeProps = storeType.GetProps().Where(x => !x.Type.IsNullableValueType && x.Type.TypeNormalized == typeof(DateTimeOffset)).ToArray();
                if (fixDateTimeProps.Any())
                {
                    O();
                    foreach (var prop in fixDateTimeProps)
                        O($"item.{prop.Name} = SeedTime;");
                }

                O("SetBasics(item);");
                O("OnAdding(item);");
                O($"Context.{storeType.PluralName}.AddOrUpdate(item);");
                End();

                // Partial OnAdding
                O();
                O($"partial void OnAdding({storeType.ClassName} item);");
            }

            End();
            End();
        }
    }
}
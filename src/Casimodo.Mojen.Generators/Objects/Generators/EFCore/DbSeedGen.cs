using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public abstract class DbSeedGenBase : DataLayerGenerator
    {
        protected class SeedGenItem
        {
            public MojType Type;
            public List<MojValueSetContainer> Seedings;
        }

        protected List<SeedGenItem> GetSeedItems()
        {
            var types = App.GetTopTypes();
            return App.GetItems<MojValueSetContainer>()
                .Where(seedConfig => types.Any(type => type.StoreOrSelf == seedConfig.TypeConfig))
                .GroupBy(x => x.TypeConfig)
                .Select(group => new SeedGenItem
                {
                    Type = group.Key,
                    Seedings = group.ToList()
                })
                .OrderBy(x => x.Type.Name)
                .ToList();
        }
    }

    public class CoreDbSeedGen : DbSeedGenBase
    {
        public CoreDbSeedGen()
        {
            Scope = "DataContext";
            Lang = "C#";
        }

        protected override void GenerateCore()
        {
            var config = App.Get<MojGlobalDataSeedConfig>();
            if (!config.IsDbSeedEnabled)
                return;

            var outputDirPath = DataConfig.DbSeedRegistryDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var items = GetSeedItems();
            if (!items.Any())
                return;

            // Write seed container.
            PerformWrite(Path.Combine(outputDirPath, DataConfig.TypePrefix + "DbSeed" + ".generated.cs"),
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
        }

        void GenerateSeedContainer(List<SeedGenItem> items)
        {
            OUsing("System", "System.Globalization", "System.Threading.Tasks", "Casimodo.Lib.Data",
                items.First().Type.Namespace);

            ONamespace(DataConfig.DataNamespace + ".Seed");

            var className = DataConfig.TypePrefix + "DbSeed";

            // Seed info class
            var infoClassName = DataConfig.TypePrefix + "DbSeedInfo";
            var seedConfigs = App.GetItems<MojSeedConfig>().ToList();
            OB($"public class {infoClassName} : DbSeedInfo");
            OB($"public {infoClassName}()");

            foreach (var section in seedConfigs.GroupBy(x => x.Section))
            {
                OB($@"AddSection(""{section.Key}"", new string[]");
                foreach (var seedConfig in section.OrderBy(x => x.TypeConfig.Name))
                {
                    O($@"""{seedConfig.TypeConfig.PluralName}"",");
                }
                End(");");
            }

            foreach (var item in items)
            {
                var type = item.Type;
                var isseedAsync = item.Seedings.Any(x => x.IsAsync);
                O($@"Items.Add(""{type.PluralName}"", {(isseedAsync ? "async " : "")}seed => {(isseedAsync ? "await " : "")}(seed as {className}).Seed{type.PluralName}());");
            }
            End();
            End();
            O();

            // Seed class with main seed method calling all other seed methods.
            OB($"public partial class {className} : DbSeed<{DataConfig.DbContextName}>");

            var isasync = items.SelectMany(x => x.Seedings).Any(x => x.IsAsync);
            O($"{GetAsyncMethod(isasync)} SeedCore()");
            Begin();
            O("if (!IsEnabled) return;");
            O("SeedTime = DateTimeOffset.Parse(\"{0}\", CultureInfo.InvariantCulture);", App.Now.ToString(CultureInfo.InvariantCulture));
            O();
            foreach (var item in items)
            {
                var type = item.Type;
                var enabled = item.Seedings.All(x => x.IsDbSeedEnabled);
                O("{0}{1}Seed{2}();",
                    (enabled ? "" : "// DISABLED: "),
                    GetAsyncAwait(item.Seedings.Any(x => x.IsAsync)),
                    type.PluralName);
            }
            End();
            End();
            End();
        }

        string GetAsyncMethod(MojValueSetContainer container)
        {
            return GetAsyncMethod(container.IsAsync);
        }

        string GetAsyncMethod(bool isasync)
        {
            return isasync ? "async Task" : "void";
        }

        string GetAsyncAwait(MojValueSetContainer container)
        {
            return container.IsAsync ? "await " : "";
        }

        string GetAsyncAwait(bool isasync)
        {
            return isasync ? "await " : "";
        }

        void GenerateSeed(SeedGenItem item)
        {
            if (item.Seedings.Count == 0)
                return;

            var type = item.Type;

            MojType storeType = type.Kind == MojTypeKind.Entity ? type : type.GetNearestStore();

            OUsing("System", "System.Linq", "System.Threading.Tasks", "Casimodo.Lib.Data",
                (DataConfig.DataNamespace != storeType.Namespace ? storeType.Namespace : null));
            ONamespace(DataConfig.DataNamespace + ".Seed");

            // Seed class
            O($"partial class {DataConfig.TypePrefix}DbSeed");
            Begin();

            // Seed method for this MojType.
            O($"internal {GetAsyncMethod(item.Seedings.Any(x => x.IsAsync))} Seed{type.PluralName}()");
            Begin();

            var enabled = item.Seedings.All(x => x.IsDbSeedEnabled);
            if (enabled)
            {
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
                        Oo($"{GetAsyncAwait(valueSetContainer)}Add(new {storeType.ClassName} {{ ");

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
            }
            End();
            O();

            // KABU TODO: This is a temporary hack to allow for custom seeding of identity users.
            if (storeType.Name != "User")
            {
                // Generate add method.
                O("public void Add({0} item)", storeType.ClassName);
                Begin();
                // Set any non-nullable DateTimes to DateTime.Now, otherwise
                // SQL Server will bark about not being able to convert from datetime to datetime2.
                var fixDateTimeProps = storeType.GetProps().Where(x => !x.Type.IsNullableValueType && x.Type.TypeNormalized == typeof(DateTimeOffset)).ToArray();
                if (fixDateTimeProps.Any())
                {
                    O();
                    foreach (var prop in fixDateTimeProps)
                        O("item.{0} = SeedTime;", prop.Name);
                }

                O("SetBasics(item);");
                O("OnAdding(item);");
                O("AddOrUpdate(item);");
                End();

                // Partial OnAdding
                O();
                O("partial void OnAdding({0} item);", storeType.ClassName);
            }

            End();
            End();
        }
    }
}
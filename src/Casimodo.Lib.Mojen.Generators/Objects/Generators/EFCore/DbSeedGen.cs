using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class CoreDbSeedGen : DataLayerGenerator
    {
        public CoreDbSeedGen()
        {
            Scope = "DataContext";
            Lang = "C#";
        }

        public DataLayerConfig DataConfig { get; set; }

        protected override void GenerateCore()
        {
            var config = App.Get<MojGlobalDataSeedConfig>();
            if (!config.IsDbSeedGeneratorEnabled)
                return;

            DataConfig = App.Get<DataLayerConfig>();

            var outputDirPath = DataConfig.DbSeedRegistryDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var types = App.GetTopTypes().Where(x => x.Seedings.Count != 0).ToArray();
            if (!types.Any())
                return;

            // Write seed container.
            PerformWrite(Path.Combine(outputDirPath, DataConfig.TypePrefix + "DbSeed" + ".generated.cs"),
                () => GenerateSeedContainer(types));

            // Generate seed file for each type.
            outputDirPath = DataConfig.DbSeedDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            foreach (var type in types)
            {
                type.CheckRequiredStore();

                PerformWrite(Path.Combine(outputDirPath, string.Format("Seed.{0}.generated.cs", type.PluralName)),
                    () => GenerateSeed(type));
            }
        }

        public void GenerateSeedContainer(MojType[] types)
        {
            OUsing("System", "System.Globalization", "System.Threading.Tasks", "Casimodo.Lib.Data", types.First().Namespace);

            ONamespace(DataConfig.DataNamespace + ".Seed");

            var className = DataConfig.TypePrefix + "DbSeed";

            // Seed info class
            var infoClassName = DataConfig.TypePrefix + "DbSeedInfo";
            OB($"public class {infoClassName} : DbSeedInfo");
            OB($"public {infoClassName}()");
            foreach (var type in types)
            {
                var isseedAsync = type.Seedings.Any(x => x.IsAsync);
                O($@"Items.Add(""{type.PluralName}"", {(isseedAsync ? "async " : "")}(seed) => {(isseedAsync ? "await " : "")}(seed as {className}).Seed{type.PluralName}());");
            }
            End();
            End();
            O();

            // Seed class with main seed method calling all other seed methods.
            OB($"public partial class {className} : DbSeed<{DataConfig.DbContextName}>");

            var isasync = types.SelectMany(x => x.Seedings).Any(x => x.IsAsync);
            O($"{GetAsyncMethod(isasync)} SeedCore()");
            Begin();
            O("if (!IsEnabled) return;");
            O("SeedTime = DateTimeOffset.Parse(\"{0}\", CultureInfo.InvariantCulture);", App.Now.ToString(CultureInfo.InvariantCulture));
            O();
            foreach (var type in types)
            {
                var enabled = type.Seedings.All(x => x.IsDbSeedEnabled);
                O("{0}{1}Seed{2}();",
                    (enabled ? "" : "// DISABLED: "),
                    GetAsyncAwait(type.Seedings.Any(x => x.IsAsync)),
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

        public void GenerateSeed(MojType type)
        {
            if (type.Seedings.Count == 0)
                return;

            MojType storeType = type.Kind == MojTypeKind.Entity ? type : type.GetNearestStore();

            OUsing("System", "System.Linq", "System.Threading.Tasks", "Casimodo.Lib.Data",
                (DataConfig.DataNamespace != storeType.Namespace ? storeType.Namespace : null));
            ONamespace(DataConfig.DataNamespace + ".Seed");

            // Seed class
            O($"partial class {DataConfig.TypePrefix}DbSeed");
            Begin();

            // Seed method for this MojType.
            O($"internal {GetAsyncMethod(type.Seedings.Any(x => x.IsAsync))} Seed{type.PluralName}()");
            Begin();

            var enabled = type.Seedings.All(x => x.IsDbSeedEnabled);
            if (enabled)
            {
                var assignments = new List<string>();
                foreach (MojValueSetContainer valueSetContainer in type.Seedings)
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
                                var filePath = DataConfig.DbSeedBinariesDirPath.Substring(DataConfig.DbDirPath.Length + 1);
                                filePath += "\\" + val.Value;
                                assignments.Add(string.Format("{0} = ReadFileContent(@\"{1}\")",
                                    prop.Name,
                                    filePath));
                            }
                            else
                            {
                                assignments.Add(string.Format("{0} = {1}{2}",
                                    prop.Name,
                                    MojenUtils.GetCsCast(prop),
                                    MojenUtils.ToCsValue(val.Value, parse: true, verbatim: true)));
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
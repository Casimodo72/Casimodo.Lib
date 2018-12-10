﻿using System;
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
        }

        public DataLayerConfig DataConfig { get; set; }

        protected override void GenerateCore()
        {
            var config = App.Get<GlobalDataSeedConfig>();
            if (!config.IsSeedGeneratorEnabled)
                return;

            DataConfig = App.Get<DataLayerConfig>();

            var outputDirPath = DataConfig.DbSeedRegistryDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            var types = App.GetTopTypes().Where(x => x.Seedings.Count != 0).ToArray();
            if (!types.Any())
                return;

            // Write seed container.
            PerformWrite(Path.Combine(outputDirPath, "Seed" + DataConfig.DbContextName + ".generated.cs"),
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

            O($"partial class Seed{DataConfig.DbContextName}");
            Begin();
            // TODO: REMOVE: O("public {0} Context {{ get; set; }}", DataConfig.DbContextName);
            //O();

            var isasync = types.SelectMany(x => x.Seedings).Any(x => x.IsAsync);
            O($"public {GetAsyncMethod(isasync)} Seed({DataConfig.DbContextName} context)");
            Begin();
            O("if (!IsEnabled) return;");
            O("Context = context;");
            O("SeedTime = DateTimeOffset.Parse(\"{0}\", CultureInfo.InvariantCulture);", App.Now.ToString(CultureInfo.InvariantCulture));
            O();
            foreach (var type in types)
            {
                var enabled = type.Seedings.All(x => x.IsEnabled);
                O("{0}{1}Seed{2}();",
                    GetAsyncAwait(type.Seedings.Any(x => x.IsAsync)),
                    (enabled ? "" : "// DISABLED: "),
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
            O($"partial class Seed{DataConfig.DbContextName}");
            Begin();
            O($"{GetAsyncMethod(type.Seedings.Any(x => x.IsAsync))} Seed{type.PluralName}()");
            Begin();

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
                                MojenUtils.ToCsValue(val.Value, parse: true)));
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
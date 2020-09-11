using Casimodo.Lib.Data;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Reads DB data of an entity and transforms it to a Mojen seed definition.
    /// </summary>
    public class EntityFromDbToSeedGen : EntityFromDbTransformationGenBase
    {
        public override void GenerateExport()
        {
            MainSeedConfig = App.Get<MojGlobalDataSeedConfig>(required: false);

            if (MainSeedConfig == null || !MainSeedConfig.IsDbImportEnabled ||
                string.IsNullOrEmpty(MainSeedConfig.DbImportOutputSeedDirPath) ||
                string.IsNullOrEmpty(MainSeedConfig.DbImportConnectionString))
                return;

            var containers = App.GetItems<MojValueSetContainer>().Where(x => x.Uses(this)).ToArray();

            foreach (var container in containers)
            {
                Options = container.GetGeneratorConfig<EntityFromDbTransformationOptions>();
                if (Options?.IsEnabled == false)
                    continue;

                string outputDirPath = Options?.OutputDirPath ?? MainSeedConfig.DbImportOutputSeedDirPath;

                var filePath = Path.Combine(outputDirPath, container.TypeConfig.Name + ".Seed.generated.cs");

                PerformWrite(filePath, () => GenerateExport(container));
            }
        }

        public void GenerateExport(MojValueSetContainer container)
        {
            var seedType = container.TypeConfig;
            var storeType = seedType.GetNearestStore();

            ONamespace("Casimodo.Lib.Mojen");

            O($"public partial class {storeType.Name}Seed : MojGeneratedDbSeed");
            Begin();

            // Constructor
            O($"public override void PopulateCore(MojValueSetContainerBuilder seed)");
            Begin();

            // TODO: REMOVE: O("seed.ClearSeedProps();");

            var dbprops = container.GetSeedableProps().Select(x => x.StoreOrSelf).ToArray();

            var nonst = dbprops.FirstOrDefault(x => x.Type.Type == null);
            if (nonst != null)
                throw new MojenException("Seed definition must not contain non simple type properties.");

            O("seed.Seed(" + dbprops.Select(x => "\"" + x.Name + "\"").Join(", ") + ");");

            var fields = dbprops.Select(x => "[" + container.GetImportPropName(x.Name) + "]").Join(", ");

            var table = storeType.TableName;

            Type queryType = Moj.CreateType(storeType, dbprops, container.GetImportPropName);

            var query = $"select {fields} from [{table}]";

            // Sort
            query = AddOrderBy(query);

            // bool validate = true;

            using (var db = new DbContext(MainSeedConfig.DbImportConnectionString))
            {
                foreach (var entity in db.Database.SqlQuery(queryType, query))
                {
                    Oo("seed.Add(");

                    int i = 0;
                    foreach (var prop in dbprops)
                    {
                        var sourcePropName = container.GetImportPropName(prop.Name);
                        var value = Casimodo.Lib.TypeHelper.GetTypeProperty(entity, sourcePropName, required: true)
                            .GetValue(entity);

                        if (value == null)
                        {
                            if (!prop.Type.CanBeNull || prop.Rules.IsRequired)
                                throw new MojenException($"Prop '{prop.Name}' is null in import DB, but must not be null in target DB.");
                            o("null");
                        }
                        else if (prop.Type.Type == typeof(string))
                        {
                            o("@\"" + ((string)value).Replace("\"", "\"\"") + "\"");
                        }
                        else if (prop.Type.TypeNormalized == typeof(Guid))
                        {
                            o("\"" + value.ToString() + "\"");
                        }
                        else if (prop.Type.IsEnum)
                        {
                            // KABU TODO: IMPORTANT: Handle enums.
                            o(Moj.CS(value, parse: false));
                        }
                        else
                        {
                            o(Moj.CS(value, parse: false));
                        }

                        if (++i < dbprops.Length)
                            o(", ");
                    }

                    oO(");");
                }
            }

            End();
            End();
            End();
        }
    }
}
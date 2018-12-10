using Casimodo.Lib.Data;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Reads database data of an entity and transforms that data to a Mojen DB seed definition.
    /// </summary>
    public class EntityDbToSeedExporterGen : EntityExporterGenBase
    {
        public override void GenerateExport()
        {
            foreach (var item in App.GetItems<MojValueSetContainer>().Where(x => x.Uses(this)))
            {
                Options = item.GetGeneratorConfig<EntityExporterOptions>();
                if (Options?.IsEnabled == false)
                    continue;

                string outputDirPath = Options?.OutputDirPath ?? ExportConfig.ProductionDataFetchOutputDirPath;

                var filePath = Path.Combine(outputDirPath, item.TargetType.Name + ".Seed.generated.cs");

                PerformWrite(filePath, () => GenerateExport(item));
            }
        }

        public void GenerateExport(MojValueSetContainer container)
        {
            var seedType = container.TargetType;
            var storeType = seedType.GetNearestStore();

            ONamespace("Casimodo.Lib.Mojen");

            O($"public partial class {storeType.Name}Seed");
            Begin();

            // Constructor
            O($"public void Populate(MojValueSetContainerBuilder seed)");
            Begin();

            O("seed.ClearSeedProps();");

            var props = container.GetProps(defaults: false)
                //.Where(x => x.Type.Type != null)
                .Select(x => seedType.FindStoreProp(x.Name))
                .Where(x => !x.Reference.Is || !x.Reference.IsToMany)
                .ToArray();

            if (props.Any(x => x.Type.Type == null))
                throw new MojenException("Seed definition must not contain non simple type properties.");

            O("seed.Seed(" + props.Select(x => "\"" + x.Name + "\"").Join(", ") + ");");

            var fields = props.Select(x => "[" + x.Name + "]").Join(", ");

            var table = storeType.TableName;

            Type queryType = MojenUtils.CreateType(storeType, props);

            var query = $"select {fields} from [{table}]";

            // Sort
            if (!string.IsNullOrWhiteSpace(Options.OrderBy))
            {
                query += $" order by [{Options.OrderBy}]";
            }

            using (var db = new DbContext(ExportConfig.DbConnectionString))
            {
                foreach (var entity in db.Database.SqlQuery(queryType, query))
                {
                    Oo("seed.Add(");

                    int i = 0;
                    foreach (var prop in props)
                    {
                        var value = Casimodo.Lib.TypeHelper.GetTypeProperty(entity, prop.Name, required: true)
                            .GetValue(entity);

                        if (value == null)
                        {
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
                            o(MojenUtils.ToCsValue(value, parse: false));
                        }
                        else
                        {
                            o(MojenUtils.ToCsValue(value, parse: false));
                        }

                        if (++i < props.Length)
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
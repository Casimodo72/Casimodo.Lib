using Dapper;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    ///  Reads DB data of an entity and transforms it to XML.
    /// </summary>
    public class EntityFromDbToXmlGen : EntityFromDbTransformationGenBase
    {
        public EntityFromDbToXmlGen()
        {
            Scope = "Context";
        }

        public override void GenerateExport()
        {
            foreach (var type in App.GetTopTypes().Where(x => x.Uses(this)))
            {
                GenerateExport(type);
            }
        }

        public void GenerateExport(MojType origType)
        {
            Options = origType.GetGeneratorConfig<EntityFromDbTransformationOptions>();
            if (Options?.IsEnabled == false)
                return;

            var type = origType.GetNearestStore();

            var rootElem = XEl("Items", XA("EntityType", type.ClassName));

            var dbprops = type.GetDatabaseProps().ToArray();

            var fields = dbprops.Select(x => "[" + x.Name + "]").Join(", ");

            var table = type.TableName;

            Type queryType = Moj.CreateType(type, dbprops);
            using (var conn = new SqlConnection(MainSeedConfig.DbImportConnectionString))
            {
                foreach (var entity in conn.Query(queryType, $"select {fields} from {table}"))
                {
                    var itemElem = XEl("Item");
                    rootElem.Add(itemElem);

                    foreach (var prop in dbprops)
                    {
                        var value = Casimodo.Lib.TypeHelper.GetTypeProperty(entity, prop.Name, required: true)
                            .GetValue(entity);

                        if (value == null)
                            // NULL values are expressed by leaving out the property.
                            continue;

                        itemElem.Add(XEl("Prop", XA("Name", prop.Name), Moj.XmlValue(value)));
                    }
                }
            }

            // Save to file

            string outputDirPath = Options?.OutputDirPath ?? MainSeedConfig.DbImportOutputXmlDirPath;
            var filePath = Path.Combine(outputDirPath, "Data." + type.ClassName + ".Xml.generated.cs");

            rootElem.Save(filePath);
        }
    }
}
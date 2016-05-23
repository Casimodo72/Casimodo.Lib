﻿using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Xml;

namespace Casimodo.Lib.Mojen
{
    public class AuthUserEntityExporterOptions : EntityExporterOptions
    {
        public string PwSourceFilePath { get; set; }
    }

    public class EntityExporterOptions : MojBase
    {
        public bool IsEnabled { get; set; } = true;
        public string OutputDirPath { get; set; }

        public string OrderBy { get; set; }

        /// <summary>
        /// KABU TODO: Not implemented yet
        /// </summary>
        public string Filter { get; set; }
    }

    public class GlobalDataSeedConfig : MojBase
    {
        public bool IsProductionDataFetchEnabled { get; set; }
        public bool IsProductionDataOverwriteEnabled { get; set; }
        public bool IsInitialSeedEnabled { get; set; }
        public string DbConnectionString { get; set; }
        public string ProductionDataFetchOutputDirPath { get; set; }
    }

    public abstract class EntityExporterGenBase : DataLayerGenerator
    {
        public EntityExporterGenBase()
        {
            Scope = "Context";
        }

        public GlobalDataSeedConfig ExportConfig { get; set; }
        public EntityExporterOptions Options { get; set; }

        protected override void GenerateCore()
        {
            ExportConfig = App.Get<GlobalDataSeedConfig>(required: false);

            if (ExportConfig == null || !ExportConfig.IsProductionDataFetchEnabled ||
                string.IsNullOrEmpty(ExportConfig.ProductionDataFetchOutputDirPath) ||
                string.IsNullOrEmpty(ExportConfig.DbConnectionString))
                return;

            GenerateExport();
        }

        public abstract void GenerateExport();
    }

    /// <summary>
    /// Reads entity data from a database and serializes that data to XML.
    /// </summary>
    public class EntityDbToXmlExporterGen : EntityExporterGenBase
    {
        public EntityDbToXmlExporterGen()
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
            Options = origType.GetGeneratorConfig<EntityExporterOptions>();
            if (Options?.IsEnabled == false)
                return;

            var type = origType.GetNearestStore();

            var rootElem = XE("Items", XA("EntityType", type.ClassName));

            var props = type.GetProps()
                .Where(x => x.Type.Type != null)
                .ToArray();

            var fields = props.Select(x => "[" + x.Name + "]").Join(", ");

            var table = type.TableName;

            Type queryType = MojenUtils.CreateType(type, props);

            using (var db = new DbContext(ExportConfig.DbConnectionString))
            {
                foreach (var entity in db.Database.SqlQuery(queryType, $"select {fields} from {table}"))
                {
                    var itemElem = XE("Item");
                    rootElem.Add(itemElem);

                    foreach (var prop in props)
                    {
                        var value = Casimodo.Lib.TypeHelper.GetTypeProperty(entity, prop.Name, required: true)
                            .GetValue(entity);

                        if (value == null)
                            // NULL values are expressed by leaving out the property.
                            continue;

                        itemElem.Add(XE("Prop", XA("Name", prop.Name), MojenUtils.ToXmlValue(value)));
                    }
                }
            }

            // Save to file


            string outputDirPath = Options?.OutputDirPath ?? ExportConfig.ProductionDataFetchOutputDirPath;
            var filePath = Path.Combine(outputDirPath, "Data." + type.ClassName + ".Xml.generated.cs");

            rootElem.Save(filePath);
        }
    }
}
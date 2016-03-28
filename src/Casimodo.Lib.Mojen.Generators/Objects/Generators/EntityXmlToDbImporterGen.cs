using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public class EntityImportConfig : MojBase
    {
        public bool IsEnabled { get; set; }
        public string DbConnectionString { get; set; }
        public string InputDirPath { get; set; }
    }

    public abstract class EntityImporterGenBase : DataLayerGenerator
    {
        public EntityImporterGenBase()
        {
            Scope = "Context";
        }

        public EntityImportConfig ImportConfig { get; set; }

        protected override void GenerateCore()
        {
            ImportConfig = App.Get<EntityImportConfig>(required: false);

            if (ImportConfig == null || !ImportConfig.IsEnabled ||
                string.IsNullOrEmpty(ImportConfig.InputDirPath) ||
                string.IsNullOrEmpty(ImportConfig.DbConnectionString))
                return;

            GenerateImport();
        }

        public abstract void GenerateImport();
    }

    /// <summary>
    /// Reads entity data from XML and imports that data to a database.
    /// </summary>
    public class EntityXmlToDbImporterGen : EntityImporterGenBase
    {
        public override void GenerateImport()
        {
            foreach (var type in App.GetTopTypes().Where(x => x.Uses(this)))
            {
                GenerateImport(type);
            }
        }

        public void GenerateImport(MojType type)
        {
            type = type.GetNearestStore();

            var props = type.GetProps()
                .Where(x => x.Type.Type != null)
                .ToArray();

            var filePath = Path.Combine(ImportConfig.InputDirPath, "Data." + type.ClassName + ".Xml.generated.cs");
            var rootElem = XElement.Load(filePath);

            var fields = props.Select(x => "[" + x.Name + "]").Join(", ");

            var table = type.TableName;

            // KABU TODO: REVISIT: Currently we don't need imports.
            throw new NotImplementedException();
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Reads entity data from XML and writes that data to a DB.
    /// KABU TODO: REMOVE? Not implemented yet. Maybe won't be ever needed.
    /// </summary>
    public class EntityToDbFromXmlGen : EntityToDbTransformationGenBase
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

            var filePath = Path.Combine(Options.InputDirPath, "Data." + type.ClassName + ".Xml.generated.cs");
            var rootElem = XElement.Load(filePath);

            var fields = props.Select(x => "[" + x.Name + "]").Join(", ");

            var table = type.TableName;

            // KABU TODO: REVISIT: Currently we don't need imports.
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public abstract class AppPartGenerator : MojenGenerator
    {
        public virtual void ProcessOnUse(object usedBy)
        {
            // NOP
        }

        public IEnumerable<string> GetAllDataNamespaces()
        {
            var result = App.GetItems<DataLayerConfig>().Select(x => x.DataNamespace).ToArray();
            return result;
        }

        public string LinqOrderBy(MojViewConfig view)
        {
            var orderProps = view.Props.Where(x => x.OrderByIndex > 0).OrderBy(x => x.OrderByIndex);
            if (!orderProps.Any())
                return "";

            string result = ".";
            int i = 0;
            foreach (var prop in orderProps)
            {
                if (i == 0)
                    result += "OrderBy(";
                else
                    result += "ThenBy(";

                result += "x => x." + prop.OrigTargetProp.Name + ")";
            }

            return result;
        }
    }
}

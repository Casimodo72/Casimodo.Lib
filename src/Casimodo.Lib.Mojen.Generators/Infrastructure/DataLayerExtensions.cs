using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public static class DataLayerExtensions
    {
        public static string GetTypeKeysClassName(this DataLayerConfig config)
        {
            return $"{config.Prefix ?? ""}TypeKeys";
        }
    }
}

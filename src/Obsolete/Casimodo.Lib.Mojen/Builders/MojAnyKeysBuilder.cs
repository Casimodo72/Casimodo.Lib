using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class MojAnyKeysBuilder
    {
        public Type ValueType { get; set; }

        public MojAnyKeysConfig Config { get; set; }

        public MojAnyKeysBuilder UseInstance()
        {
            Config.UseInstance = true;

            return this;
        }

        public MojAnyKeysBuilder Add(string key, object value)
        {
            Config.Items.Add(new MojAnyKeyItemConfig
            {
                Key = key,
                Value = value
            });

            return this;
        }
    }
}

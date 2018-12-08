using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public class DbMigrationSeedBase
    {
        public DateTimeOffset SeedTime { get; set; }

        public bool IsEnabled { get; set; }

        public virtual void SetBasics(object item)
        {
            if (GetProp<DateTimeOffset?>(item, CommonDataNames.CreatedOn, null) == null)
                SetProp(item, CommonDataNames.CreatedOn, SeedTime);

            if (GetProp<DateTimeOffset?>(item, CommonDataNames.ModifiedOn, null) == null)
                SetProp(item, CommonDataNames.ModifiedOn, SeedTime);
        }

        public void SetProp(object item, string name, object value)
        {
            item.GetTypeProperty(name)?.SetValue(item, value);
        }

        public T GetProp<T>(object item, string name, T defaultValue)
        {
            var prop = item.GetTypeProperty(name);
            if (prop == null)
                return defaultValue;

            return (T)prop.GetValue(item);
        }
    }
}

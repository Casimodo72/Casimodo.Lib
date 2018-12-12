using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public class DbSeedInfo
    {
        public readonly Dictionary<string, string[]> Sections = new Dictionary<string, string[]>();
        public readonly Dictionary<string, Action<DbSeedBase>> Items = new Dictionary<string, Action<DbSeedBase>>();

        public void AddSection(string name, string[] items)
        {
            Sections.Add(name, items);
        }

        public bool ContainsSection(string name)
        {
            return Sections.Keys.Contains(name);
        }

        public bool ContainsItem(string name)
        {
            return Items.Keys.Contains(name);
        }
    }

    public class DbSeedInfo<TDbSeed>
        where TDbSeed : DbSeedBase
    {
        public Dictionary<string, Action<TDbSeed>> Items { get; protected set; } = new Dictionary<string, Action<TDbSeed>>();
    }

    public class DbSeed<TContext> : DbSeedBase
        where TContext : DbContext
    {
        public TContext Context { get; set; }
    }

    public class DbSeedBase
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

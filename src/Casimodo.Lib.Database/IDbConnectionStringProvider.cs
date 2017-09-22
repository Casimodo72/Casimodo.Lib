using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public interface IDbConnectionStringProvider
    {
        string Get(string name);
    }

    public class DbConnectionStringProvider : IDbConnectionStringProvider
    {
        readonly ConcurrentDictionary<string, string> _items = new ConcurrentDictionary<string, string>();

        public void Set(string name, string connectionString)
        {
            _items.AddOrUpdate(name, connectionString, (prev, cur) => connectionString);
        }

        public string Get(string name)
        {
            string result;
            if (_items.TryGetValue(name, out result))
                return result;

            throw new CasimodoLibException($"Connection string with name '{name}' not found.");
        }
    }
}

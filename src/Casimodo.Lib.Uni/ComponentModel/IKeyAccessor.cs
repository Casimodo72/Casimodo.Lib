using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib
{
    public interface IGuidGenerateable
    {
        void GenerateGuid();
    }

    public interface IKeyAccessor<TKey>
        where TKey : IComparable<TKey>
    {
        TKey GetKey();
    }
}

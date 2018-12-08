using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public interface IODataDynamicPropertiesAccessor
    {
        IDictionary<string, object> DynamicProperties { get; }
    }
}
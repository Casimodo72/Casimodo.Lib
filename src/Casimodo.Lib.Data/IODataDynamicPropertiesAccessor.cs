using System.Collections.Generic;

namespace Casimodo.Lib.Data
{
    public interface IODataDynamicPropertiesAccessor
    {
        IDictionary<string, object> DynamicProperties { get; }
    }
}
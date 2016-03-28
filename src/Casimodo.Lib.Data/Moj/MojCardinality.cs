using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    [Flags]
    public enum MojCardinality
    {
        None = 0,
        Zero = 1 << 0,
        One = 1 << 1,
        OneOrZero = One | Zero,
        Many = 1 << 2 | Zero
    }
}

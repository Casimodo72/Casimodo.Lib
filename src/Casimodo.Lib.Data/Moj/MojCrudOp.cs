using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public enum MojCrudOp
    {
        None = 0,
        Create = 1,
        Update = 2,
        Delete = 3,
        Patch = 4,
        Touch = 5
    }
}

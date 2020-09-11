using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class ODataControllerOptions
    {
        public bool IsEmpty { get; set; }

        public bool IsReadOnly { get; set; }

        public bool IsReadable { get; set; } = true;

        public bool IsCreatable { get; set; } = true;

        public bool IsUpdateable { get; set; } = true;

        public bool IsDeletable { get; set; } = true;

        public bool UpdateMask { get; set; } = true;

        public int MaxExpansionDepth { get; set; } = 2;
    }

    // KABU TODO: REMOVE? Not used
    public class ODataControllerUpsert
    {
        public MojViewConfig Editor { get; set; }
    }
}

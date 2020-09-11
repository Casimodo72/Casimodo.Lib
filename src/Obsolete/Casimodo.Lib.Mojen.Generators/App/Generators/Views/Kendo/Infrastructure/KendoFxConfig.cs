using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class KendoFxConfig
    {
        public static readonly KendoFxConfig None = new KendoFxConfig(false);

        public KendoFxConfig(bool @is)
        {
            Is = @is;
        }

        public bool Is { get; set; }
        public string Effects { get; set; }
        public int Duration { get; set; } = 100;
    }
}


using System;

namespace Casimodo.Lib.Mojen
{
    public class KendoPagerOptions : ICloneable
    {
        public bool UseRefresh { get; set; } = false;
        public bool UseInput { get; set; } = true;
        public bool UsePageSizes { get; set; } = true;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}

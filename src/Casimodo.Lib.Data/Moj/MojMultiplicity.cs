using System;

namespace Casimodo.Lib.Data
{
    [Flags]
    public enum MojMultiplicity
    {
        None = 0,
        Zero = 1 << 0,
        One = 1 << 1,
        OneOrZero = One | Zero,
        Many = 1 << 2 | Zero
    }
}

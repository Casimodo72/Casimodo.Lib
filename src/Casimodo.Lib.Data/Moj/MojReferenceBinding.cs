using System;

namespace Casimodo.Lib.Data
{
    [Flags]
    public enum MojReferenceBinding
    {
        None = 0,
        Loose = 1 << 0, // Loose or Nested
        Nested = 1 << 1,
        Associated = 1 << 2, // Associated or Owned
        Owned = 1 << 3
    }
}

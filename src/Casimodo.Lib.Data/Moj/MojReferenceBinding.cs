using System;

namespace Casimodo.Lib.Data
{
    [Flags]
    public enum MojReferenceBinding
    {
        None = 0,
        // Loose or Nested
        Loose = 1 << 0,
        Nested = 1 << 1,
        // Associated or Owned or Independent
        Associated = 1 << 2,
        Owned = 1 << 3,
        ExclusivelyOwned = 1 << 4,
        Independent = 1 << 5,
        OwnedLoose = Owned | Loose
    }
}

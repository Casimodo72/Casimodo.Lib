using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib;

public static class CollectionExtensions
{
    public static IEnumerable<(T, int)> WithIndex<T>(this ICollection<T> items)
        => items.Select((item, index) => (item, index));

    public static bool HasNext(this ICollection items, int currentIndex)
        => currentIndex < items.Count - 1;
}

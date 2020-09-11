using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib
{
    public static class ListExtensions
    {
        public static IEnumerable<T> Exclude<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            var list = items.ToList();
            foreach (var item in list.Where(predicate).ToArray())
                list.Remove(item);

            return list;
        }

        public static IEnumerable<T> Exclude<T>(this IEnumerable<T> items, Func<List<T>, T, bool> predicate)
        {
            var list = items.ToList();
            foreach (var item in list.Where(x => predicate(list, x)).ToArray())
                list.Remove(item);

            return list;
        }

        public static IEnumerable<T> RemoveWhere<T>(this List<T> items, Func<T, bool> predicate)
        {
            var selected = items.Where(predicate).ToArray();
            foreach (var item in selected)
                items.Remove(item);

            return selected;
        }

        public static IEnumerable<T> RemoveWhere<T>(this List<T> items, Func<List<T>, T, bool> predicate)
        {
            var selected = items.Where(x => predicate(items, x)).ToArray();
            foreach (var item in selected)
                items.Remove(item);

            return selected;
        }
    }
}

using System.Linq;

namespace Casimodo.Lib
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ToQueryable<T>(this T item)
        {
            return (new T[] { item }).AsQueryable();
        }
    }
}
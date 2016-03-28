// KABU: TODO: REMOVE

namespace Casimodo.Lib
{
#if (false)
    public static class SetExtensions
    {
        public static bool IsInSet<T>(this T source, params T[] set)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return set.Contains(source);
        }
    }
#endif
}
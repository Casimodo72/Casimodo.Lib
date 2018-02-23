using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TSource> AddIfNotDefault<TSource>(this IEnumerable<TSource> items, Func<TSource> func)
        {
            foreach (var item in items)
                yield return item;

            var result = func();

            if (!object.Equals(result, default(TSource)))
                yield return result;
        }

        public static bool Contains(this string text, params string[] values)
        {
            if (string.IsNullOrEmpty(text) || values == null)
                return false;

            for (int i = 0; i < values.Length; i++)
                if (text.Contains(values[i]))
                    return true;

            return false;
        }

        public static string[] Split(this string text, char separator, bool emptyEntries = false)
        {
            return text.Split(new[] { separator }, emptyEntries ? StringSplitOptions.None : StringSplitOptions.RemoveEmptyEntries);
        }

        public static string JoinTrimmed(this IEnumerable<string> source, string separator)
        {
            return JoinToString(source, separator, trim: true);
        }

        /// <summary>
        /// NOTE: Returns an empty string if source is empty.
        /// </summary>
        public static string Join(this IEnumerable<string> source, string separator)
        {
            return JoinToString(source, separator);
        }

        public static string SafeTrim(this string value)
        {
            if (value == null) return null;
            return value.Trim();
        }

        /// <summary>
        /// NOTE: Returns an empty string if source is empty.
        /// </summary>
        public static string JoinToString<TSource>(this IEnumerable<TSource> source, string separator, Func<TSource, string> transform = null, bool trim = false)
        {
            if (source == null || !source.Any())
                return null;

            if (transform == null)
                transform = (x) => x.ToString();

            var sb = new StringBuilder();
            string value;
            foreach (var item in source)
            {
                if (item == null)
                    continue;

                if (typeof(TSource) == typeof(string))
                {
                    value = item as string;

                    if (trim)
                        value = value.Trim();

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    value = transform((TSource)(object)value);
                }
                else
                {
                    value = transform(item);
                }

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (sb.Length != 0)
                    sb.Append(separator);

                sb.Append(value);
            }

            return sb.ToString();
        }

        public static void AddRangeDistinctBy<TSource, TKey>(this ICollection<TSource> source, IEnumerable<TSource> items, Func<TSource, TKey> keySelector)
        {
            var keys = source.Select(keySelector).ToList();
            foreach (var item in items)
            {
                if (!keys.Contains(keySelector(item)))
                    source.Add(item);
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
                if (seenKeys.Add(keySelector(element)))
                    yield return element;

            yield break;
        }

        public static IEnumerable<T> EnsureNotNull<T>(this IEnumerable<T> source)
            where T : class
        {
            if (source == null)
                return Enumerable.Empty<T>();
            else
                return source;
        }
    }
}
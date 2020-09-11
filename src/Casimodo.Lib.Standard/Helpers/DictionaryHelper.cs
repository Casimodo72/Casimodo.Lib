using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib
{
    public static class DictionaryHelper
    {
        public static TResult GetOrDefault<TResult>(this IDictionary<string, object> dictionary, string key, TResult defaultValue = default(TResult))
        {
            if (dictionary.TryGetValue(key, out object value))
                return (TResult)value;

            return defaultValue;
        }

        public static TValue FindOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            return defaultValue;
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            throw new Exception(string.Format("The entry '{0}' does not exist in this dictionary.", key));
        }

        public static void Set<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }
    }
}
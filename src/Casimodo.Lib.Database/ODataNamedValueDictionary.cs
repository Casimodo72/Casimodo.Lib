using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    [DataContract]
    public class ODataNamedValueDictionary<TValue> : IDictionary<string, TValue>
    {
        [DataMember]
        public IDictionary<string, object> Items { get { return _items ?? (_items = new Dictionary<string, object>()); } set { _items = value; } }
        IDictionary<string, object> _items;

        public TValue this[string key]
        {
            get { return (TValue)Items[key]; }
            set { Items[key] = value; }
        }

        public int Count
        {
            get { return Items.Count; }
        }

        public bool IsReadOnly
        {
            get { return Items.IsReadOnly; }
        }

        public ICollection<string> Keys
        {
            get { return Items.Keys; }
        }

        /// <summary>
        /// NOTE: This method will create a new ReadOnlyCollection based on the
        /// values of the underlying dictionary.
        /// </summary>
        public ICollection<TValue> Values
        {
            get { return new ReadOnlyCollection<TValue>(Items.Values.Cast<TValue>().ToList()); }
        }

        public void Add(KeyValuePair<string, TValue> item)
        {
            Items.Add(Convert(item));
        }

        public void Add(string key, TValue value)
        {
            Items.Add(key, value);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(KeyValuePair<string, TValue> item)
        {
            return Items.Contains(Convert(item));
        }

        public bool ContainsKey(string key)
        {
            return Items.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (Items.Count > array.Length - arrayIndex)
                throw new ArgumentException("The number of elements in the source dictionary " +
                    "is greater than the available space from arrayIndex to the end of the destination array.",
                    nameof(arrayIndex));

            var i = 0;
            foreach (var item in Items)
            {
                array[i] = Convert(item);
                i++;
            }
        }

        public bool Remove(KeyValuePair<string, TValue> item)
        {
            return Items.Remove(Convert(item));
        }

        public bool Remove(string key)
        {
            return Items.Remove(key);
        }

        public bool TryGetValue(string key, out TValue value)
        {
            object obj;
            if (Items.TryGetValue(key, out obj))
            {
                value = (TValue)obj;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            foreach (var item in Items)
                yield return Convert(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        KeyValuePair<string, object> Convert(KeyValuePair<string, TValue> item)
        {
            return new KeyValuePair<string, object>(item.Key, item.Value);
        }

        KeyValuePair<string, TValue> Convert(KeyValuePair<string, object> item)
        {
            return new KeyValuePair<string, TValue>(item.Key, (TValue)item.Value);
        }        
    }
}

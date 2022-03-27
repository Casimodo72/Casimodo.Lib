// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.UI
{
    public class CustomObservableCollection<T> : CustomObservableCollection,
        IEnumerable<T>,
        ICollection<T>,
        IReadOnlyCollection<T>
    {
        public CustomObservableCollection(CollectionSourceAdapterBase source)
            : base(source)
        { }

        public CustomObservableCollection()
            : base()
        { }

        public CustomObservableCollection(IList<T> items)
            : base(new ShortedCollectionSourceAdapter())
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            BeginUpdate();
            try
            {
                foreach (var item in items)
                    _items.Add(item);
            }
            finally
            {
                EndUpdate();
            }
        }

        // IEnumerable<T> ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public IEnumerator<T> GetEnumerator()
        {
            return _items.Cast<T>().GetEnumerator();
        }

        // ICollection<T> ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>
        /// Adds the given item to this collection *and* the source collection.
        /// Member of ICollection&lt;T&gt;.
        /// </summary>
        public void Add(T item)
        {
            base.Add(item);
        }

        /// <summary>
        /// Member of ICollection&lt;T&gt;.
        /// </summary>
        public bool Contains(T item)
        {
            return base.Contains(item);
        }

        public T ItemAt(int index) => (T)base[index];

        /// <summary>
        /// Member of ICollection&lt;T&gt;.
        /// </summary>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            object[] objarray = new object[array.Length];
            _items.CopyTo(objarray, arrayIndex);

            for (int i = 0; i < objarray.Length; i++)
                array[i] = (T)objarray[i];
        }

        /// <summary>
        /// Removes the given item from the local collection *and* from the source collection.
        /// Returns true if the item was successfully removed, false otherwise.
        /// Member of ICollection&lt;T&gt;.
        /// </summary>
        public bool Remove(T item)
        {
            return base.Remove(item);
        }
    }
}
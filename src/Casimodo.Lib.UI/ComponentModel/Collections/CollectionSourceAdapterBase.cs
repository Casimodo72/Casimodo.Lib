// Copyright (c) 2009 Kasimier Buchcik

using Casimodo.Lib.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Casimodo.Lib.UI
{
    /// <summary>
    /// Base class adapter intended to be used as the source of the CustomObservableCollection.
    /// </summary>
    public abstract class CollectionSourceAdapterBase<T> : ObservableObject, IEnumerable<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public abstract int Count { get; }

        public abstract void Add(T item);

        public abstract void Remove(T item);

        public abstract bool IsReadOnly { get; }

        public abstract bool CanAdd { get; }

        public abstract bool CanRemove { get; }

        protected abstract IEnumerator<T> GetEnumeratorInternal();

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        /// <summary>
        /// Relays collection events of the source.
        /// </summary>
        protected virtual void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseCollectionChanged(e);
        }

        protected void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }
}
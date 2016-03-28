// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Casimodo.Lib.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    /// <summary>
    /// Base class adapter intended to be used as the source of the CustomObservableCollection.
    /// </summary>
    // TODO: Never tried this one in Silverlight yet.
    public abstract class CollectionSourceAdapterBase : ObservableObject, IEnumerable, INotifyCollectionChanged
    {
        static protected readonly PropertyChangedEventArgs CountChangedArgs = new PropertyChangedEventArgs("Count");
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public abstract int Count
        { get; }

        public abstract void Add(object item);

        public abstract void Remove(object item);

        public abstract bool IsReadOnly
        { get; }

        public abstract bool CanAdd
        { get; }

        public abstract bool CanRemove
        { get; }

#if (false)
        public abstract bool CanEdit
        { get; }
#endif

        protected abstract IEnumerator GetEnumeratorInternal();

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
            var handler = this.CollectionChanged;
            if (handler != null)
                handler(this, e);
        }
    }
}
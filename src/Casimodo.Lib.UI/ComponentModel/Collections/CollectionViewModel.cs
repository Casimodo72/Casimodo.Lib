// Copyright (c) 2009 Kasimier Buchcik

using Casimodo.Lib.ComponentModel;
using System.Collections.Specialized;

namespace Casimodo.Lib.Presentation
{
    public abstract class CollectionViewModel : ObservableObject
    {
        public abstract int Count { get; }

        public abstract bool IsReadOnly { get; }

        public abstract void AddObject(object data);

        public abstract void RemoveObject(object data);

        public abstract bool CanAdd { get; }

        public abstract bool CanRemove { get; }

        /// <summary>
        /// Notifies when the items collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler ItemsCollectionChanged;

        protected virtual void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ItemsCollectionChanged?.Invoke(this, e);
        }
    }
}
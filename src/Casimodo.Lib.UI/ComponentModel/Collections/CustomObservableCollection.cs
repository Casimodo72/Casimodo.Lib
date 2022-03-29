// Copyright (c) 2009 Kasimier Buchcik

using Casimodo.Lib.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Casimodo.Lib.UI
{
    public class CollectionModificationRequestedEventArgs : EventArgs
    {
        public CollectionModificationRequestedEventArgs()
        { }

        public void Deny(string reason)
        {
            IsDenied = true;
            ReasonForDenial = reason;
        }

        public bool IsDenied { get; private set; }

        public string ReasonForDenial { get; private set; }
    }

    public delegate void CollectionModificationRequestedEventHandler(object sender, CollectionModificationRequestedEventArgs e);

    /// <summary>
    /// Observable collection.
    /// </summary>
    /// <remarks>
    /// - Note that there are some needless casts to object here, because I
    ///   used Casimodo.Lib.Ria.CustomEntityCollection a as blue print.
    /// - We also implement IList, because otherwise the System.Windows.Data.PagedCollectionView
    ///   does not want to AddNew() items.
    /// - The scope of IRevertibleChangeTracking consists only of the
    ///   items *currently* in the collection. We don't track removed items.
    ///   I wonder if we should implement change tracking here at all.
    /// </remarks>
    public partial class CustomObservableCollection<T> : ObservableObject,
        IEnumerable<T>,
        ICollection<T>,
        IReadOnlyCollection<T>,
        IEnumerable,
        IList,
        ICollection,
        INotifyPropertyChanged,
        INotifyCollectionChanged,
        IChangeTracking,
        IRevertibleChangeTracking
        where T : class
    {
        static readonly PropertyChangedEventArgs IsChangedChangedArgs = new(nameof(IsChanged));
        static readonly PropertyChangedEventArgs CountChangedArgs = new(nameof(Count));
        static readonly PropertyChangedEventArgs IndexerChangedArgs = new("Item[]");
        protected readonly object _syncRoot = new();
        protected CollectionSourceAdapterBase<T> _source;

        private Collection<T> _items { get; }

        public CustomObservableCollection(CollectionSourceAdapterBase<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _items = new MyCollection<T>(this);

            SetSource(source);

            IsModificationAllowed = true;
            IsAdditionAllowed = true;
            IsRemovalAllowed = true;
        }

        public CustomObservableCollection()
            : this(new ShortedCollectionSourceAdapter<T>())
        {
            (_source as ShortedCollectionSourceAdapter<T>).Items = this;
        }

        public CustomObservableCollection(IList<T> items)
           : this(new ShortedCollectionSourceAdapter<T>())
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

        public void SetSource(CollectionSourceAdapterBase<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            BeginUpdate();
            try
            {
                DetachFromSource();
                _source = source;
                AttachToSource();
                FetchFromSource();
            }
            finally
            {
                EndUpdate();
            }
        }

        /// <summary>
        /// Sorts the items using the given comparer.
        /// </summary>
        public void ApplySort(IComparer comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            T[] sortedItems = _items.ToArray();
            Array.Sort(sortedItems, comparer);

            BeginUpdate();
            try
            {
                _items.Clear();
                for (int i = 0; i < sortedItems.Length; i++)
                    _items.Add(sortedItems[i]);
            }
            finally
            {
                EndUpdate();
            }
        }

        /// <summary>
        /// If set to false then IsReadOnly will always be true.
        /// </summary>
        public bool IsModificationAllowed { get; set; }

        /// <summary>
        /// If set to false then CanAdd will always be false.
        /// </summary>
        public bool IsAdditionAllowed { get; set; }

        /// <summary>
        /// If set to false then CanRemove will always be false.
        /// </summary>
        public bool IsRemovalAllowed { get; set; }

        public void SetRange(int startPosition, int count)
        {
            if (startPosition < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(startPosition), "The given start position must be greator or equal to 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(count), "The given count must be greator or equal to 0");

            if (_startPosition == startPosition && _maximumCount == count)
                return;

            _startPosition = startPosition;
            _maximumCount = count;

            FetchFromSource();
        }

        int _startPosition = -1;
        int _maximumCount;

        void FetchFromSource()
        {
            CheckIsModificationAllowedByConsumer();

            // Refetch items in the given range from the source collection.

            BeginUpdate();
            try
            {
                _items.Clear();

                IEnumerable newItems;

                if (_startPosition != -1)
                {
                    newItems = _source.Skip(_startPosition).Take(_maximumCount);
                }
                else
                {
                    newItems = _source;
                }

                foreach (T item in newItems)
                    _items.Add(item);
            }
            finally
            {
                EndUpdate();
            }
        }

        /// <summary>
        /// Mutes event notifications during bulk updates.
        /// </summary>
        public void BeginUpdate()
        {
            _updateTransactionCounter++;
        }

        int _updateTransactionCounter;

        /// <summary>
        /// Re-enables event notification after bulk updates and raises a collection reset event.
        /// </summary>
        public void EndUpdate()
        {
            EndUpdate(false);
        }

        /// <summary>
        /// Re-enables event notification after bulk updates and raises a collection reset event.
        /// </summary>
        public void EndUpdate(bool forced)
        {
            _updateTransactionCounter--;

            if (forced || _updateTransactionCounter <= 0)
            {
                // Re-enable event notifications.
                _updateTransactionCounter = 0;

                RaiseBulkChanged();
            }
        }

        public bool IsUpdating => _updateTransactionCounter > 0;

        internal event CollectionModificationRequestedEventHandler ModificationRequested;

        /// <summary>
        /// Adds the given item to the collection.
        /// This does *not* add the given item to the source collection.
        /// Not a member of any interface.
        /// </summary>
        public void AddLocal(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            CheckIsModificationAllowedByConsumer();

            _items.Add(item);
        }

        /// <summary>
        /// Adds the given items to the collection.
        /// This does *not* add the given item to the source collection.
        /// Not a member of any interface.
        /// </summary>
        public void AddRangeLocal(T[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            CheckIsModificationAllowedByConsumer();

            for (int i = 0; i < items.Length; i++)
                _items.Add(items[i]);
        }

        /// <summary>
        /// Removes an item at the given index from the collection.
        /// This does *not* remove any item from the source collection.
        /// Not a member of any interface.
        /// </summary>
        public void RemoveLocalAt(int index)
        {
            CheckIsModificationAllowedByConsumer();

            _items.RemoveAt(index);
        }

        /// <summary>
        /// Removes an item at the given index from the collection.
        /// This does *not* remove any item from the source collection.
        /// Not a member of any interface.
        /// </summary>
        public void RemoveLocal(T item)
        {
            CheckIsModificationAllowedByConsumer();

            _items.Remove(item);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// Note that this does *not* remove any items from the source collection.
        /// </summary>
        public void ClearLocal()
        {
            CheckIsModificationAllowedByConsumer();

            BeginUpdate();
            try
            {
                _items.Clear();
            }
            finally
            {
                EndUpdate();
            }
        }

#if (false)
        public void SetRangeLocal(int startPosition, int count)
        {
            if (startPosition < 0)
                throw new ArgumentOutOfRangeException(
                    "startPosition", "The given start position must be greator or equal to 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(
                    "count", "The given count must be greator or equal to 0");

            if (_startPositionLocal == startPosition && _maximumCountLocal == count)
                return;

            _startPositionLocal = startPosition;
            _maximumCountLocal = count;

            FetchFromSource();
        }
        int _startPositionLocal = -1;
        int _maximumCountLocal;

        int EffectiveStartPositionLocal
        {
            get { return (_startPositionLocal != -1) ? _startPositionLocal : 0; }
        }
#endif

        /// <summary>
        /// Return true if at least one of the items in the collection was changed.
        /// </summary>
        public bool HasChanges
        {
            get
            {
                IChangeTracking changeTracking;
                foreach (var item in _items)
                {
                    changeTracking = item as IChangeTracking;
                    if (changeTracking != null && changeTracking.IsChanged)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Adds the given item to the collection *and* to the source collection.
        /// Not a member of any interface.
        /// </summary>
        public void Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!CanAdd)
                throw new NotSupportedException("The collection cannot be added to.");

            CheckIsModificationAllowedByConsumer();

            // Add to local collection
            // KBU TODO: VERY IMPORTANT: BUG: This raises a ItemsCollectionChanged event, but
            //   too early, because in the event handler the item was not yet added to the source collection
            //   thus a LastOrDefault() over the collection will *not* return the added item :-(
            _items.Add(item);

            // Add to source collection.
            PerformFlaggedAction(() => _source.Add(item), ref _isAddingToSource, true);
        }

        /// <summary>
        /// Not a member of any interface.
        /// </summary>
        public void Insert(int index, T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!CanAdd)
                throw new NotSupportedException("The collection cannot be added to.");

            CheckIsModificationAllowedByConsumer();

            // Insert into local collection
            _items.Insert(index, item);

            // Add to source collection.
            PerformFlaggedAction(() => _source.Add(item), ref _isAddingToSource, true);
        }

        /// <summary>
        /// Not a member of any interface.
        /// </summary>
        internal T this[int index]
        {
            get { return _items[index]; }
        }

        /// <summary>
        /// Not a member of any interface.
        /// </summary>
        internal int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        /// <summary>
        /// Removes the given item from the collection *and* from the source collection.
        /// Note that whether and how items are actually removed from the source collection
        /// depends on the implementation of the source adapter in use.
        /// Not a member of any interface.
        /// </summary>
        public bool Remove(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!CanRemove)
                throw new NotSupportedException(
                    "The collection cannot be removed from.");

            CheckIsModificationAllowedByConsumer();

            int pos = _items.IndexOf(item);
            if (pos < 0)
                return false;

            _items.RemoveAt(pos);

            // Remove from the source collection.
            PerformFlaggedAction(() => _source.Remove(item), ref _isRemovingFromSource, true);

            return true;
        }

        /// <summary>
        /// Indicates whether the given item is contained by the collection.
        /// Not a member of any interface.
        /// </summary>
        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        /// <summary>
        /// The underlying source collection.
        /// Not a member of any interface.
        /// </summary>
        internal IEnumerable SourceCollection => _source as IEnumerable;

        /// <summary>
        /// Not a member of any interface.
        /// </summary>
        internal bool CanAdd => (!IsReadOnly) && IsAdditionAllowed && _source.CanAdd;

        /// <summary>
        /// Not a member of any interface.
        /// </summary>
        internal bool CanRemove => (!IsReadOnly) && IsRemovalAllowed && _source.CanRemove;

        // Interfaces =========================================================

        #region INotifyCollectionChanged --------------------------------------

        /// <summary>
        /// Member of INotifyCollectionChanged (the only one).
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion INotifyCollectionChanged --------------------------------------

        #region IEnumerable Members -------------------------------------------

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Member of IEnumerable (the only one).
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion IEnumerable Members -------------------------------------------

        #region ICollection Members -------------------------------------------

        /// <summary>
        /// Member of ICollection&lt;T&gt;.
        /// </summary>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Member of ICollection
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Member of ICollection
        /// </summary>
        public bool IsSynchronized => ((ICollection)_items).IsSynchronized;

        /// <summary>
        /// Member of ICollection
        /// </summary>
        public object SyncRoot => ((ICollection)_items).SyncRoot;

        /// <summary>
        /// Member of ICollection
        /// </summary>
        void ICollection.CopyTo(Array array, int index)
        {
            _items.CopyTo((T[])array, index);
        }

        #endregion ICollection Members -------------------------------------------

        #region IList (nasty) -------------------------

        /// <summary>
        /// Indicates whether the IList (or ICollection&lt;T&gt;) is read-only.
        /// Member of IList (and ICollection&lt;T&gt;).
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                if (!IsModificationAllowed)
                    return false;

                return _source.IsReadOnly;
            }
        }

        /// <summary>
        /// Clears all items of this collection *and* the source collection.
        /// Note that whether and how items are actually removed from the source collection
        /// depends on the implementation of the source adapter in use.
        /// Member of IList (and ICollection&lt;T&gt;).
        /// </summary>
        public void Clear()
        {
            if (!CanRemove)
                throw new NotSupportedException(
                    "The current state of the collection does not allow changes.");

            CheckIsModificationAllowedByConsumer();

            if (_items.Count == 0 && _source.Count == 0)
                return;

            BeginUpdate();
            try
            {
                bool prevFlag = _isRemovingFromSource;
                try
                {
                    _isRemovingFromSource = true;
                    // Remove items from the source collection.
                    foreach (var item in _items)
                        _source.Remove(item);
                }
                finally
                {
                    _isRemovingFromSource = prevFlag;
                }

                // Clear all items in the local collection.
                _items.Clear();
            }
            finally
            {
                EndUpdate();
            }
        }

        int IList.Add(object value)
        {
            Add((T)value);

            return IndexOf((T)value);
        }

        bool IList.Contains(object value)
        {
            return Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Insert(index, (T)value);
        }

        bool IList.IsFixedSize => IsReadOnly;

        void IList.Remove(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Remove((T)value);
        }

        void IList.RemoveAt(int index)
        {
            if (IsReadOnly)
                throw new NotSupportedException("This list is read-only.");

            _items.RemoveAt(index);
        }

        object IList.this[int index]
        {
            get => _items[index];
            set =>
                // TODO: How to replace an item in the source collection as well?
                throw new NotImplementedException();
        }

        #endregion IList (nasty) -------------------------

        /// <summary>
        /// Not a member of any interface.
        /// </summary>
        public T[] ToArrayInternal()
        {
            return _items.ToArray();
        }

        #region IChangeTracking Members ---------------------------------------

        /// <summary>
        /// Calls AccepChanges() on each of the items in the local collection.
        /// Member of IChangeTracking.
        /// </summary>
        public void AcceptChanges()
        {
            foreach (IChangeTracking item in _items)
            {
                if (item != null && item.IsChanged)
                    item.AcceptChanges();
            }
            RaisePropertyChanged(IsChangedChangedArgs);
        }

        /// <summary>
        /// Member of IChangeTracking.
        /// </summary>
        public bool IsChanged => HasChanges;

        #endregion IChangeTracking Members ---------------------------------------

        #region IRevertibleChangeTracking Members -----------------------------

        /// <summary>
        /// Calls RejectChanges() on each of the items in the local collection.
        /// Member of IRevertibleChangeTracking.
        /// </summary>
        public void RejectChanges()
        {
            foreach (IRevertibleChangeTracking item in _items)
            {
                if (item != null && item.IsChanged)
                    item.RejectChanges();
            }
            RaisePropertyChanged(IsChangedChangedArgs);
        }

        #endregion IRevertibleChangeTracking Members -----------------------------

        #region Source changes and processing ---------------------------------

        /// <summary>
        /// Handle changes of the source collection.
        /// </summary>
        protected void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // Skip if we are adding explicitely to the source collection.
                    if (IsAddingToSource)
                        return;

                    // Add items automatically to the local collection
                    // if added to the underying source collection.
                    ProcessAfterSourceItemsAdded(e);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    // Skip if we are removing explicitely from the source collection.
                    if (IsRemovingFromSource)
                        return;

                    // Remove items automatically from the local collection
                    // if removed from the underlying source collection.
                    ProcessAfterSourceItemsRemoved(e);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    ProcessAfterSourceItemsReset(e);
                    break;

                default:
                    throw new NotSupportedException(
                        string.Format("Collection change kind of '{0}' is not supported yet.",
                            e.Action.ToString()));
            }
        }

        /// <summary>
        /// Adds the newly added items of the source to the local collection.
        /// </summary>
        protected virtual void ProcessAfterSourceItemsAdded(NotifyCollectionChangedEventArgs args)
        {
            foreach (var item in args.NewItems)
                _items.Add((T)item);
        }

        /// <summary>
        /// Removes the newly removed items of the source from the local collection.
        /// </summary>
        protected virtual void ProcessAfterSourceItemsRemoved(NotifyCollectionChangedEventArgs args)
        {
            foreach (var item in args.OldItems)
            {
                int pos = _items.IndexOf((T)item);
                if (pos == -1)
                    continue;

                _items.RemoveAt(pos);
            }
        }

        /// <summary>
        /// Rebuilds the entire local collection based on the source collection.
        /// </summary>
        protected virtual void ProcessAfterSourceItemsReset(NotifyCollectionChangedEventArgs args)
        {
            BeginUpdate();
            try
            {
                _items.Clear();

                object[] sourceItems = _source.Cast<object>().ToArray();
                foreach (var item in sourceItems)
                    _items.Add((T)item);
            }
            finally
            {
                EndUpdate();
            }
        }

        #endregion Source changes and processing ---------------------------------

        /// <summary>
        /// Indicates whether an item is added explicitely to the source,
        /// so that we skip related collection events of the source.
        /// </summary>
        protected bool IsAddingToSource => _isAddingToSource;

        bool _isAddingToSource;

        /// <summary>
        /// Indicates whether an item is removed explicitely from the source,
        /// so that we skip related collection events of the source.
        /// </summary>
        protected bool IsRemovingFromSource => _isRemovingFromSource;

        bool _isRemovingFromSource;

        #region Event raisers -------------------------------------------------

        void RaiseCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            if (IsUpdating)
                return;

            NotifyCollectionChangedEventHandler handler = CollectionChanged;
            if (handler == null)
                return;

            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    handler(this, new NotifyCollectionChangedEventArgs(action, item, index));
                    break;

                case NotifyCollectionChangedAction.Reset:
                    handler(this, ResetNotifyCollectionArgs);
                    break;

                default:
                    throw new NotSupportedException(
                        string.Format("Collection change action of '{0}' is not supported yet.",
                            action.ToString()));
            }
        }

        static readonly NotifyCollectionChangedEventArgs ResetNotifyCollectionArgs = new(NotifyCollectionChangedAction.Reset);

        protected void RaiseBulkChanged()
        {
            if (IsUpdating)
                return;

            RaisePropertyChanged(CountChangedArgs);
            RaisePropertyChangedNoVerification(IndexerChangedArgs);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Reset, null, -1);
        }

        #endregion Event raisers -------------------------------------------------

        /// <summary>
        /// Raises the 'ModificationRequested' event and throws an InvalidOperationException
        /// if any consumer cancelled the the operation.
        /// </summary>
        void CheckIsModificationAllowedByConsumer()
        {
            var handler = ModificationRequested;
            if (handler == null)
                return;

            var args = new CollectionModificationRequestedEventArgs();
            handler(this, args);

            if (args.IsDenied)
                throw new InvalidOperationException("Modification of the collection was denied: " + args.ReasonForDenial);
        }

        protected void PerformBulkUpdate(Action action)
        {
            BeginUpdate();
            try
            {
                action();
            }
            finally
            {
                EndUpdate();
            }
        }

        static void PerformFlaggedAction(Action action, ref bool flag, bool value)
        {
            bool prevFlag = flag;
            try
            {
                flag = value;

                action();
            }
            finally
            {
                // Restore flag.
                flag = prevFlag;
            }
        }

        void AttachToSource()
        {
            if (_source != null)
            {
                // Detach from source.
                (_source as INotifyCollectionChanged).CollectionChanged += OnSourceCollectionChanged;
            }
        }

        void DetachFromSource()
        {
            if (_source != null)
            {
                // Detach from source.
                (_source as INotifyCollectionChanged).CollectionChanged -= OnSourceCollectionChanged;
            }
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();

            DetachFromSource();
            _source = null;
            _items.Clear();
        }

        /// <summary>
        /// The internal collection.
        /// </summary>
        sealed class MyCollection<TData> : Collection<TData>
        {
            readonly CustomObservableCollection<T> _owner;

            public MyCollection(CustomObservableCollection<T> owner)
            {
                _owner = owner;
            }

            protected override void InsertItem(int index, TData item)
            {
                base.InsertItem(index, item);

                if (!_owner.IsUpdating)
                {
                    _owner.RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
                    _owner.RaisePropertyChanged(CountChangedArgs);
                }
            }

            protected override void RemoveItem(int index)
            {
                if (!_owner.IsUpdating)
                {
                    TData item = this[index];

                    base.RemoveItem(index);

                    _owner.RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
                    _owner.RaisePropertyChanged(CountChangedArgs);
                }
                else
                {
                    base.RemoveItem(index);
                }
            }

            protected override void ClearItems()
            {
                base.ClearItems();

                if (!_owner.IsUpdating)
                {
                    _owner.RaiseBulkChanged();
                }
            }
        }
    }
}
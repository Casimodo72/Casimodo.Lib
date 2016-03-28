// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Globalization;
using Casimodo.Lib.ComponentModel;
using MyItemType = System.Object;

namespace Casimodo.Lib.Presentation
{
    // TODO: Never tried this one in Silverlight yet.
    public abstract class CustomCollectionView : ObservableObject,
        IEnumerable,
        ICollectionView,
        IEditableCollectionView,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        protected static PropertyChangedEventArgs CurrentPositionChangedArgs = new PropertyChangedEventArgs("CurrentPosition");
        protected static PropertyChangedEventArgs IsCurrentBeforeFirstChangedArgs = new PropertyChangedEventArgs("IsCurrentBeforeFirst");
        protected static PropertyChangedEventArgs IsCurrentAfterLastChangedArgs = new PropertyChangedEventArgs("IsCurrentAfterLast");
        public static PropertyChangedEventArgs CurrentItemChangedArgs = new PropertyChangedEventArgs("CurrentItem");
        public static PropertyChangedEventArgs IsAddingNewChangedArgs = new PropertyChangedEventArgs("IsAddingNew");
        public static PropertyChangedEventArgs IsEditingItemChangedArgs = new PropertyChangedEventArgs("IsEditingItem");
        protected static PropertyChangedEventArgs CurrentEditItemChangedArgs = new PropertyChangedEventArgs("CurrentEditItem");
        protected static PropertyChangedEventArgs CurrentAddItemChangedArgs = new PropertyChangedEventArgs("CurrentAddItem");
        protected static PropertyChangedEventArgs CanCancelEditChangedArgs = new PropertyChangedEventArgs("CanCancelEdit");
        static PropertyChangedEventArgs CountChangedArgs = new PropertyChangedEventArgs("Count");
        static PropertyChangedEventArgs ItemCountChangedArgs = new PropertyChangedEventArgs("ItemCount");

        protected CustomObservableCollection _items;
        MyItemType _currentAddItem;
        MyItemType _currentEditItem;
        SortDescriptionCollection _sortDescriptions;
        int _deferredRefreshTransactionCounter;

        /// <summary>
        /// Creates a new CustomEntityCollectionView.
        /// </summary>
        /// <param name="internalCollection">is used as the *internal* collection of the view.
        /// This is *not* the source collection.</param>
        public CustomCollectionView(CustomObservableCollection internalCollection)
        {
            if (internalCollection == null)
                throw new ArgumentNullException("internalCollection");

            this._sortDescriptions = new SortDescriptionCollection();
            (_sortDescriptions as INotifyCollectionChanged).CollectionChanged += OnSortDescriptionsChanged;

            this._items = internalCollection;
            this._items.CollectionChanged += OnInternalCollectionChanged;
            this._items.ModificationRequested += OnInternalCollectionModificationRequested;
        }

        public event EventHandler Refreshed;

        /// <summary>
        /// Requests a newly created instance of an item from the consumer whenever
        /// a new item is being added (e.g. via IEditableCollectionView.AddNew()).
        /// An InvalidOperationException exception is thrown if this callback
        /// is not assigned while a new item is being added.
        /// </summary>
        public Func<MyItemType> NewItemProvider { get; set; }

        public event NotifyCollectionChangedEventHandler SortDescriptionsChanged;

        /// <summary>
        /// Member of INotifyCollectionChanged.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// If set to true: the collection won't allow modification
        /// and won't allow changing the current item.
        /// </summary>
        public bool IsCollectionViewFrozen
        { get; set; }

        /// <summary>
        /// Returns the type-converted given item.
        /// Throws an exception if the given item is not derived from the expected type.
        /// </summary>        
        protected virtual MyItemType CheckEntityTypeAndConvert(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            MyItemType item = obj as MyItemType;
            if (item == null)
                throw new ArgumentException(
                    string.Format("The given item must be of type {0}.", typeof(MyItemType).Name),
                    "item");

            return item;
        }

        protected bool QueryCanMoveCurrent()
        {
            return !EvalCancelChanging();
        }

        protected virtual IEnumerator GetEnumeratorInternal()
        {
            return (_items as IEnumerable).GetEnumerator();
        }

        #region Interfaces ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        #region IEnumerable ---------------------------------------------------

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        #endregion

        #region ICollectionView -----------------------------------------------

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        // TODO: IMPL local filter.
        public virtual bool CanFilter
        {
            get { return false; }
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        // TODO: IMPL grouping.
        public virtual bool CanGroup
        {
            get { return false; }
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public virtual bool CanSort
        {
            get { return true; }
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public bool Contains(object item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            return _items.Contains(CheckEntityTypeAndConvert(item));
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        // TODO: IMPL Culture.
        public CultureInfo Culture
        {
            get { return Thread.CurrentThread.CurrentCulture; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public event EventHandler CurrentChanged;

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public event CurrentChangingEventHandler CurrentChanging;

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>        
        public object CurrentItem
        {
            get { return _currentItem; }
        }
        object _currentItem;

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public int CurrentPosition
        {
            get { return _currentPosition; }
        }
        int _currentPosition = -1;

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public virtual Predicate<object> Filter
        {
            get { return null; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public virtual ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { return null; }
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public virtual ReadOnlyObservableCollection<object> Groups
        {
            get { return null; }
        }

        /// <summary>
        /// Indicates whether the CurrentItem of the view is beyond the end of the collection.
        /// Member of ICollectionView.
        /// </summary>
        // TODO: What does this mean exactly? Is this somehow used for paging? How to implement this?
        public bool IsCurrentAfterLast
        {
            get { return _isCurrentAfterLast; }
            private set
            {
                SetValueTypeProperty(IsCurrentAfterLastChangedArgs, ref _isCurrentAfterLast, value);
            }
        }
        bool _isCurrentAfterLast = true;

        /// <summary>
        /// Indicates whether the CurrentItem of the view is beyond the end of the collection.
        /// In this implementation IsCurrentBeforeFirst is true when the CurrentItem is non null.
        /// Member of ICollectionView.
        /// </summary>
        // TODO: What does this mean exactly? Is this somehow used for paging?
        public bool IsCurrentBeforeFirst
        {
            get { return _isCurrentBeforeFirst; }
            private set
            {
                SetValueTypeProperty(IsCurrentBeforeFirstChangedArgs, ref _isCurrentBeforeFirst, value);
            }
        }
        bool _isCurrentBeforeFirst = true;

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public bool IsEmpty
        {
            get { return _items.Count == 0; }
        }

        /// <summary>        
        /// Sets the given item in the view as the CurrentItem.
        /// Member of ICollectionView.
        /// </summary>
        /// <returns>
        /// True if the resulting CurrentItem is an item in the view; otherwise, false.
        /// </returns>
        public bool MoveCurrentTo(object item)
        {
            CheckNotInDeferredRefresh();

            // In WPF, some UI is nasty enough to try to call us when we raise a
            // collection changed event. We don't want that.
            if (InInternalCollectionChangesProcessing)
                return false;

            // Moving to null has the same effect as moving to position -1.
            if (item == null)
                return MoveCurrentToPosition(-1);

            if (Count == 0)
                return false;

            if (_currentItem == item)
                return true;

            // Note that we will reject items of wrong type.
            MyItemType myItem = CheckEntityTypeAndConvert(item);

            // Evaluate whether member of the collection.
            int position = _items.IndexOf(myItem);

            // Note that moving to an item which is not a member of the collection has
            // the same effect as moving to position -1.

            return MoveCurrentToPosition(position);
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public bool MoveCurrentToFirst()
        {
            if (Count != 0)
                return MoveCurrentToPosition(0);

            return false;
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public bool MoveCurrentToLast()
        {
            if (Count != 0)
                return MoveCurrentToPosition(Count - 1);

            return false;
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public bool MoveCurrentToNext()
        {
            // Note that this allows moving to a position equal to Count.
            if (_currentPosition < Count)
                return MoveCurrentToPosition(_currentPosition + 1);

            return false;
        }

        bool InInternalCollectionChangesProcessing
        {
            get { return _internalCollectionChangesProcessingTransactionCounter > 0; }
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public bool MoveCurrentToPosition(int position)
        {
            // Check range.
            // Note that this allows the given position to be equal to Count,
            //   effectively moving the CurrentPosition beyond the items in the view.
            //   Dunno exactly, but I assume that this makes sense when used in
            //   scrolling scenarios; so when position == Count, one sets
            //   IsCurrentAfterLast to true, indicating that the consumer scrolled
            //   beyond the last item, so that some next-page-fetching mechanism
            //   can get the next bunch of items.
            if ((position < -1) || (position > Count))
                throw new ArgumentOutOfRangeException("position");

            CheckNotInDeferredRefresh();

            // In WPF, some UI is nasty enough to try to call us when we raise a
            // collection changed event. We don't want that.
            if (InInternalCollectionChangesProcessing)
                return false;

            // Note that movement of the current item is allowed when being in
            // editing-/adding-mode.            

            if (Count == 0)
                return false;

            if (position == Count)
            {
                // Move the CurrentPosition beyond (after) the items in the view and
                // set CurrentItem to null.
                // Note that IsCurrentAfterLast is set to true in this case.

                if (_currentPosition == position)
                    return false;

                // Notify and ask the consumer whether it's OK to perform the transition.
                if (EvalCancelChanging())
                    return false;

                ChangeCurrent(null, position, false);

                return false;
            }

            if (position == -1)
            {
                // Move the CurrentPosition beyond (before) the items in the view and
                // set CurrentItem to null.
                // Note that IsCurrentBeforeFirst is set to true in this case.

                if (_currentPosition == -1)
                    return false;

                // Notify and ask the consumer whether it's OK to perform the transition.
                if (EvalCancelChanging())
                    return false;

                ChangeCurrent(null, -1, false);

                return false;
            }

            if (position == _currentPosition)
                return true;

            // Notify and ask the consumer whether it's OK to perform the transition.
            if (EvalCancelChanging())
                return true;

            ChangeCurrent(_items[position], position, false);

            return true;
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public bool MoveCurrentToPrevious()
        {
            // Note that this allows moving to position -1.
            if (_currentPosition >= 0)
                return MoveCurrentToPosition(_currentPosition - 1);

            return false;
        }

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public void Refresh()
        {
            CheckNotInDeferredRefresh();

            var handler = Refreshed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            RaiseCollectionChanged(ResetNotifyCollectionArgs);
            // RaisePropertyChanged("CurrentItem");
            //RaisePropertyChanged(CountChangedArgs);
            //RaisePropertyChanged("IsEmpty");
        }

        static readonly NotifyCollectionChangedEventArgs ResetNotifyCollectionArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

        /// <summary>
        /// Member of ICollectionView.
        /// </summary>
        public SortDescriptionCollection SortDescriptions
        {
            get { return _sortDescriptions; }
        }

        /// <summary>
        /// Gets the underlying source collection.
        /// Note that this is *not* the internal collection used by the view.        
        /// Member of ICollectionView.
        /// </summary>
        public IEnumerable SourceCollection
        {
            get { return _items.SourceCollection as IEnumerable; }
        }

        /// <summary>        
        /// Member of ICollectionView.
        /// </summary>
        public IDisposable DeferRefresh()
        {
            _deferredRefreshTransactionCounter++;

            return new DeferredRefreshTransaction(OnDeferredRefreshTransactionCompleted);
        }

        #endregion End of ICollectionView -------------------------------------

        #region IEditableCollectionView ---------------------------------------

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary>        
        public object AddNew()
        {
            CheckNotInDeferredRefresh();

            Func<MyItemType> provider = NewItemProvider;

            if (provider == null)
                throw new InvalidOperationException(
                    "Cannot add a new item without an item-constructor callback being assigned.");

            MyItemType item = provider();

            if (item == null)
                throw new InvalidOperationException(
                    "The item-constructor callback did not provide an instance.");

            AddNew(item);

            return item;
        }

        /// <summary>
        /// Returns false if the view is currently eding an item (*not* adding).
        /// Otherwise, returns true if the internal collection and the underlying collection
        /// can be added to.
        /// Member of IEditableCollectionView.
        /// </summary>
        /// <remarks>
        /// TODO: Why doesn't System.Windows.Data.PagedCollectionView (the reference implementation)
        /// also return false when currently *adding* a new item? This doesn't make sense to me.
        /// Another questions is why this shall return false when currently editing an item,
        /// but when AddNew() is called the currently edited item is auto-committed anyway?
        /// How does this all make sense?
        /// </remarks>
        public bool CanAddNew
        {
            get
            {
                if (IsEditingItem)
                    return false;

                return _items.CanAdd;
            }
        }

        /// <summary>
        /// Returns true when an item is currently being edited (*not* added).        
        /// Member of IEditableCollectionView.
        /// </summary>
        /// <remarks>
        /// Note that there's actually no API to ask whether changes of a newly *added* item
        /// can be cancelled - why?
        /// </remarks>
        public bool CanCancelEdit
        {
            get
            {
                return _currentEditItem is IEditableObject;
            }
        }

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary> 
        public bool CanRemove
        {
            get
            {
                if (IsEditingItem || IsAddingNew)
                    return false;

                return _items.CanRemove;
            }
        }

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary> 
        public void CancelEdit()
        {
            CheckNotInDeferredRefresh();

            if (IsAddingNew)
            {
                throw new InvalidOperationException(
                    "Cannot CancelEdit() because the view is currently in adding-mode.");
            }

            // NOP if not in editing-mode.
            if (!IsEditingItem)
                return;


            // TODO: Should we first cancel the edit-mode with CancelEdit()
            // and then in the view or the other way round?
            MyItemType item = _currentEditItem;
            SetCurrentEditItem(null);
            ((IEditableObject)item).CancelEdit();
        }

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary> 
        public void CancelNew()
        {
            CheckNotInDeferredRefresh();

            if (IsEditingItem)
            {
                throw new InvalidOperationException(
                    "Cannot CancelNew() because the view is currently in editing-mode.");
            }

            // NOP if not in adding-mode.
            if (!IsAddingNew)
                return;

            MyItemType item = _currentAddItem;

            // TODO: Should we first cancel the edit-mode with CancelEdit()
            // and then in the view or the other way round?

            // Clear the newly added item on this view.
            SetCurrentAddItem(null);
            try
            {
                // Remove the newly added item form the collection.
                Remove(item);
            }
            finally
            {
                ((IEditableObject)item).CancelEdit();
            }
        }

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary> 
        public void CommitEdit()
        {
            CheckNotInDeferredRefresh();

            if (IsAddingNew)
            {
                throw new InvalidOperationException(
                    "Cannot CommitEdit() because the view is currently in adding-mode.");
            }

            // NOP if not in editing-mode.
            if (!IsEditingItem)
                return;

            ((IEditableObject)_currentEditItem).EndEdit();
            SetCurrentEditItem(null);
        }

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary> 
        public void CommitNew()
        {
            CheckNotInDeferredRefresh();

            if (IsEditingItem)
            {
                throw new InvalidOperationException(
                    "Cannot CommitNew() because the view is currently in editing-mode.");
            }

            // NOP if not in adding-mode.
            if (!IsAddingNew)
                return;

            ((IEditableObject)_currentAddItem).EndEdit();
            SetCurrentAddItem(null);
        }

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary> 
        public object CurrentAddItem
        {
            get { return _currentAddItem; }
        }


        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary> 
        public object CurrentEditItem
        {
            get { return _currentEditItem; }
        }

        /// <summary>
        /// Starts editing the given item.
        /// Note that this throws an exception if the givem item is not of expected type.
        /// Member of IEditableCollectionView.
        /// </summary> 
        public void EditItem(object item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            CheckNotInDeferredRefresh();

            // TODO: Is there a flag somewhere to disallow editing?

            // Note that this throws an exception if the givem item is not of expected type.
            MyItemType myItem = CheckEntityTypeAndConvert(item);

            if (IsEditingItem)
            {
                // Note that even when the CurrentEditItem is the *same* as the given
                // item, it is expected to auto-commit and restart editing.                
                CommitEdit();
            }

            if (IsAddingNew)
            {
                // Auto-commit the CurrentAddItem.
                CommitNew();
            }

            if (_items.IndexOf(myItem) < 0)
            {
                // NOTE: The reference implementation behaves differently:
                //   The reference implementation *allows* editing of an item which is not
                //   contained by the collection view.
                // TODO: I consider this not safe enough for my scenarios, so we'll throw
                //   an exception here (this behavior might change in the future).
                throw new ArgumentException("Cannot edit the given item because it is not contained by the view.");
            }

            SetCurrentEditItem(myItem);
            ((IEditableObject)myItem).BeginEdit();

#if (false)
            // Note that the CurrentItem is *not* expected to move to the
            //   edited item. I.e. the CurrentItem stays where it was.
            //   We may want to override this behavior in the future.
            if (IsMovingCurrentToEditingItemEnabled)
            {
                if (_currentItem != _currentEditItem)
                    ChangeCurrent(_currentEditItem, _items.IndexOf(_currentEditItem), true);       
            }
#endif
        }

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary> 
        public bool IsAddingNew
        {
            get { return _currentAddItem != null; }
        }

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary> 
        public bool IsEditingItem
        {
            get { return _currentEditItem != null; }
        }

        /// <summary>
        /// Removes the given item from the collection.        
        /// This has no effect if the given item is null or not contained by the view.
        /// An exception is raised if the given item is not of expected type.
        /// Member of IEditableCollectionView.        
        /// </summary>        
        public void Remove(object item)
        {
            if (item == null)
                return;

            CheckNotInDeferredRefresh();

            if (!CanRemove)
                throw new NotSupportedException(
                    "The current state of the collection view does not allow removing items.");

            MyItemType myItem = CheckEntityTypeAndConvert(item);

            // Remove from collection.
            // Note that this has no effect if the given item is not contained by the view.
            // I.e. *no* exception is expected.
            _items.Remove(myItem);
        }

        /// <summary>        
        /// Raises ArgumentOutOfRangeException if the given index is out of range.
        /// Member of IEditableCollectionView.
        /// </summary> 
        public void RemoveAt(int index)
        {
            CheckNotInDeferredRefresh();

            if (index < 0 || index >= _items.Count)
            {
                throw new ArgumentOutOfRangeException(
                    "The given index must be at least 0 and less than Count.");
            }

            if (!CanRemove)
                throw new NotSupportedException(
                    "The current state of the collection view does not allow removing items.");

            Remove(_items[index]);
        }

        /// <summary>
        /// Member of IEditableCollectionView.
        /// </summary>
        // TODO: IMPL.
        public NewItemPlaceholderPosition NewItemPlaceholderPosition
        {
            get
            {
                return NewItemPlaceholderPosition.None;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion End of IEditableCollectionView -----------------------------

        #endregion Interfaces ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        #region Internals

        /// <summary>
        /// NOTE that this method is *not* a member of any interface.
        /// </summary>        
        internal void AddNew(MyItemType item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            CheckNotInDeferredRefresh();

            CheckEntityTypeAndConvert(item);

            if (IsEditingItem)
            {
                // Auto-commit the CurrentEditItem.      
                CommitEdit();
            }

            if (IsAddingNew)
            {
                // Auto-commit the CurrentAddItem.
                CommitNew();
            }

            if (!CanAddNew)
                throw new NotSupportedException(
                    "The current state of the collection view does not allow adding items.");

            _isAddingToCollection = true;
            try
            {
                // Add to collection.
                // Note that we use the flag in order to disable automatic
                // positioning of the CurrentItem in OnInternalCollectionChanged(...),
                // because we want to set the CurrentItem *explicitely*.
                _items.Add(item);
            }
            finally
            {
                _isAddingToCollection = false;
            }

            SetCurrentAddItem(item);
            ((IEditableObject)item).BeginEdit();

            // The newly added item becomes the current item.
            ChangeCurrent(item, _items.IndexOf(item), true);
        }

        /// <summary>
        /// Indicates whether we are explicitely adding to the internal collection
        /// and don't want OnInternalCollectionChanged() to interfere.
        /// </summary>
        bool _isAddingToCollection;

        /// <summary>
        /// Sets the current item and position to the given item and position.
        /// If raiseChanging is true then a non-cancelable CurrentChanging event is raised.
        /// </summary>
        void ChangeCurrent(object item, int position, bool raiseChanging)
        {
            CheckNotInDeferredRefresh();

            // Tiny check whether we really change anything.
            if (_currentItem == item && _currentPosition == position)
                throw new InvalidOperationException(
                    "Mismatch while trying to change the current item or current position.");

            if (raiseChanging)
            {
                var handler = CurrentChanging;
                if (handler != null)
                    handler(this, new CurrentChangingEventArgs(false));
            }

            _currentItem = item;
            _currentPosition = position;

            RaisePropertyChanged(CurrentPositionChangedArgs);
            RaisePropertyChanged(CurrentItemChangedArgs);

            // Update out-of-view indicators.
            IsCurrentBeforeFirst = (_currentItem == null) && (_currentPosition == -1);
            IsCurrentAfterLast = (_currentItem == null) && ((Count == 0) || (_currentPosition >= Count));

            CheckIsCurrentValid();

            {
                var handler = CurrentChanged;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Throws an exception if the view is in a deferred refresh transaction.
        /// </summary>
        void CheckNotInDeferredRefresh()
        {
            if (_deferredRefreshTransactionCounter > 0)
                throw new InvalidOperationException(
                    "Cannot perform the desired operation because the " +
                    "collection view is in a deferred refresh transaction.");
        }

        void CheckNotFrozen()
        {
            if (IsCollectionViewFrozen)
                throw new Exception(
                    "Cannot perform the desired operation because the " +
                    "view is currently in add- or edit-mode and frozen.");
        }

        void CheckNotEditingOrAdding()
        {
            if (IsAddingNew || IsEditingItem)
                throw new Exception(
                    "Cannot perform the desired operation because the " +
                    "view is currently in add- or edit-mode.");
        }

        /// <summary>
        /// Decreases the transaction counter and calls Refresh() when the counter reaches zero.
        /// </summary>
        void OnDeferredRefreshTransactionCompleted()
        {
            _deferredRefreshTransactionCounter--;
            if (_deferredRefreshTransactionCounter <= 0)
            {
                _deferredRefreshTransactionCounter = 0;
                Refresh();
            }
        }

        /// <summary>
        /// Return true if the collection view is frozen.
        /// Notifies and asks the consumer whether it's OK to perform a change
        /// of the current item and position.
        /// Calls CurrentChanging and returns whether the subscriber wants to cancel
        /// the ongoing change of the current item and position.
        /// </summary>        
        protected bool EvalCancelChanging()
        {
            if (IsCollectionViewFrozen)
            {
                // Disallow changing the current item.
                return true;
            }

            // Query whether the consumer allows changing the current item.
            var handler = CurrentChanging;
            if (handler == null)
                return false;

            CurrentChangingEventArgs e = new CurrentChangingEventArgs(true);
            handler(this, e);
            return e.Cancel;
        }

        /// <summary>
        /// Returns the number of items in this collection view.
        /// Not a member of any interface.
        /// </summary>
        public int Count
        {
            get { return _items.Count; }
        }

        int _internalCollectionChangesProcessingTransactionCounter = 0;

        /// <summary>
        /// Handles changes of the internal collection.
        /// </summary>
        void OnInternalCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CheckNotInDeferredRefresh();
            CheckNotFrozen();
            CheckNotEditingOrAdding();

            _internalCollectionChangesProcessingTransactionCounter++;
            try
            {

                this.RaiseCollectionChanged(e);

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        UpdateCurrentAfterItemsReset();
                        break;

                    case NotifyCollectionChangedAction.Add:
                        if (_isAddingToCollection)
                            return;

                        UpdateCurrentAfterItemsAdded(e.NewStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        // TODO: REVISIT: Will this be a problem if this can only handle 1 item at a time?
                        if (e.OldItems.Count != 1)
                            throw new NotSupportedException(
                                "Removal of multiple items at once is currently not " +
                                "supported by the collection view.");
                        UpdateCurrentAfterItemsRemoved(e.OldStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        // TODO: REVISIT
                        throw new NotSupportedException();
                }

                if (e.Action != NotifyCollectionChangedAction.Replace
#if (!SILVERLIGHT)
                    && e.Action != NotifyCollectionChangedAction.Move
#endif
                )
                {
                    RaisePropertyChanged(CountChangedArgs);
                    RaisePropertyChanged("IsEmpty");
                }
            }
            finally
            {
                _internalCollectionChangesProcessingTransactionCounter--;
            }
        }

        /// <summary>
        /// Handles the 'ModificationRequested' event of the internal collection.
        /// Denies modification of the collection when this view is frozen.
        /// </summary>
        void OnInternalCollectionModificationRequested(object sender, CollectionModificationRequestedEventArgs e)
        {
            if (!e.IsDenied && IsCollectionViewFrozen)
            {
                e.Deny("A view using the collection does not allow modification currently.");
            }
        }

        /// <summary>
        /// Updates the CurrentItem and CurrentPosition.
        /// When the internal collection changes, the CurrentItem might
        /// not exist in the collection anymore or be out-of-sync with its position.      
        /// </summary>
        protected void UpdateCurrentAfterItemsReset()
        {
            if (Count == 0)
            {
                // We removed all items.

                if (_currentItem != null || _currentPosition != -1)
                {
                    // Set current item to 'before first'.
                    ChangeCurrent(null, -1, true);
                    return;
                }
            }
            else if (_currentItem != null)
            {
                // There are still items in the view.
                // There is an item currently selected.
                int position = _items.IndexOf((MyItemType)_currentItem);
                if (position < 0)
                {
                    // The current item does not exist in the collection any more.
                    // Move to first position.
                    ChangeCurrent(_items[0], 0, true);
                    return;
                }
                // The current item still exists in the view.

                if (position != _currentPosition)
                {
                    // The current item's position has changed; adjust the position.
                    ChangeCurrent(_currentItem, position, true);
                    return;
                }
            }
            else if (IsCurrentBeforeFirst)
            {
                // There are still items in the view.
                // The current item points to outside the view.
                // Move to first.
                ChangeCurrent(_items[0], 0, true);
            }
            else if (IsCurrentAfterLast)
            {
                // There are still items in the view.
                // The current item points to outside the view.

                // TODO: What to do here exactly?
                // 1) If the are more items than before:
                //   1.a) set CurrentPosition to Count
                //   1.b) OR set CurrentItem to the item at CurrentPosition
                //   1.c) OR set CurrentItem to the last item in the view.
                // 2) If there are less items than before:
                //   2.a) set CurrentPosition to Count
                //
                // Since we don't know here if items were added or removed,
                // we can only apply strategy (1.a) / (2.a).

                // Update CurrentPosition (which shall always be equal to Count).
                if (_currentPosition != Count)
                {
                    ChangeCurrent(null, Count, true);
                }
            }

            CheckIsCurrentValid();
        }

        void UpdateCurrentAfterItemsAdded(int position)
        {
            if (IsCurrentBeforeFirst)
            {
                // Move to first position.
                if (IsCurrentBeforeFirst)
                    ChangeCurrent(_items[0], 0, true);
            }
            else if (IsCurrentAfterLast)
            {
                // Move to last position.
                ChangeCurrent(_items[Count - 1], Count - 1, true);
            }
            else if (position <= _currentPosition)
            {
                // The added items are now positioned before the current item.
                // We need to update the current position.
                ChangeCurrent(_currentItem, _items.IndexOf((MyItemType)_currentItem), true);
            }
            // Note that the current position need not be updated when
            // the added items are positioned after the current position.
        }

        void UpdateCurrentAfterItemsRemoved(int position)
        {
            if (Count == 0)
            {
                // We removed all items.
                if (_currentItem != null || _currentPosition != -1)
                {
                    // Set current item to 'before first'.
                    ChangeCurrent(null, -1, true);
                }
            }
            else if (_currentItem != null)
            {
                // There are still items in the view.
                // There is an item currently selected.

                if (position < _currentPosition)
                {
                    // An item before the current item was removed.
                    // Keep current item.
                    // Update current position.                    
                    ChangeCurrent(_currentItem, _currentPosition - 1, true);
                }
                else if (position == _currentPosition)
                {
                    // The current item was removed.
                    // Update current item. 
                    // Keep current position except when the last item was removed.
                    if (position >= Count)
                        position = Count - 1;

                    ChangeCurrent(_items[position], position, true);
                }
                // Note that we don't need to change anything if the removed item
                // was positioned after the current item.
            }
            else if (IsCurrentBeforeFirst)
            {
                // There are still items in the view.
                // The current item points outside the view.

                // NOP.
            }
            else if (IsCurrentAfterLast)
            {
                // There are still items in the view.
                // The current item points outside the view.
                // Update CurrentPosition (which shall always be equal to Count).
                ChangeCurrent(null, Count, true);
            }

            CheckIsCurrentValid();
        }

        void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            var handler = CollectionChanged;
            if (handler != null)
                CollectionChanged(this, args);
        }

        void SetCurrentAddItem(MyItemType item)
        {
            if (_currentAddItem != null && item != null)
                throw new InvalidOperationException(
                    "Failed to set CurrentAddItem because the view is currently editing an item. " +
                    "Use CancelAdd() to clear the CurrentAddItem beforehand.");

            _currentAddItem = item;
            RaisePropertyChanged(IsAddingNewChangedArgs);
            RaisePropertyChanged(CurrentAddItemChangedArgs);
        }

        void SetCurrentEditItem(MyItemType item)
        {
            if (_currentEditItem != null && item != null)
                throw new InvalidOperationException(
                    "Failed to set CurrentEditItem because the view is currently editing an item. " +
                    "Use CancelEdit() to clear the CurrentEditItem beforehand.");

            _currentEditItem = item;
            RaisePropertyChanged(IsEditingItemChangedArgs);
            RaisePropertyChanged(CurrentEditItemChangedArgs);
            RaisePropertyChanged(CanCancelEditChangedArgs);
        }

        /// <summary>
        /// Handles changes of the sort descriptions.
        /// </summary>        
        void OnSortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var handler = SortDescriptionsChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Checks the valid combinations of CurrentItem, CurrentPosition,
        /// IsCurrentBeforeFirst and IsCurrentAfterLast.
        /// </summary>
        [Conditional("DEBUG")]
        void CheckIsCurrentValid()
        {
            if (_currentItem == null)
            {
                if (_currentPosition == -1)
                {
                    // The current item is positioned before the first item in the view.

                    // IsCurrentBeforeFirst must be true.
                    if (!_isCurrentBeforeFirst)
                    {
                        throw new InvalidOperationException(
                            "Current item is positioned before the first item, " +
                            "but IsCurrentBeforeFirst is false. IsCurrentBeforeFirst is expected to be true.");
                    }

                    if (Count == 0)
                    {
                        // If the view is empty, then IsCurrentAfterLast must also be true.
                        if (!_isCurrentAfterLast)
                            throw new InvalidOperationException(
                                "Current item is positioned before the first item and the view is empty, " +
                                "but IsCurrentAfterLast is false. IsCurrentAfterLast is expected to be true.");
                    }
                    else
                    {
                        // If the view is *not* empty, then IsCurrentAfterLast must be false.
                        if (_isCurrentAfterLast)
                            throw new InvalidOperationException(
                                "Current item is positioned before the first item and the view is not empty, " +
                                "but IsCurrentAfterLast is true. IsCurrentAfterLast is expected to be false.");
                    }

                    return;
                }
                else if (_currentPosition >= Count)
                {
                    // The current item is positioned after the first item in the view.

                    // IsCurrentAfterLast must be true.
                    if (!_isCurrentAfterLast)
                    {
                        throw new InvalidOperationException(
                            "Current item is positioned after the last item, " +
                            "but IsCurrentAfterLast is false. IsCurrentAfterLast is expected to be true.");
                    }

                    // IsCurrentBeforeFirst must be false.
                    if (_isCurrentBeforeFirst)
                        throw new InvalidOperationException(
                            "Current item is positioned after the last item, " +
                            "but IsCurrentBeforeFirst is true. IsCurrentBeforeFirst is expected to be false.");

                    return;
                }
                else
                {
                    throw new InvalidOperationException(
                        "When the CurrentItem is null, then CurrentPosition must be either -1 or equal to Count.");
                }
            }

            if (_currentPosition < -1 || _currentPosition > _items.Count)
                throw new InvalidOperationException(
                    "CurrentPosition is out of range.");

            if ((_currentPosition < 0) ||
                (_currentPosition == _items.Count) ||
                (_items[_currentPosition] != _currentItem))
            {
                throw new InvalidOperationException(
                    "CurrentItem and CurrentPosition are out of sync.");
            }
        }

        class DeferredRefreshTransaction : IDisposable
        {
            Action _callback;

            public DeferredRefreshTransaction(Action callback)
            {
                _callback = callback;
            }

            bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                var cb = _callback;
                _callback = null;
                if (cb != null)
                    cb();
            }
        }

        #endregion
    }
}

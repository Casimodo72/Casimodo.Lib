using Casimodo.Lib.ComponentModel;
using Casimodo.Lib.Presentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Casimodo.Lib.UI
{
    public class CollectionViewModel<TData> : CollectionViewModel, IEnumerable<TData>
        where TData : class
    {
        protected CustomObservableCollection<TData> _effectiveItems;
        protected CustomObservableCollection<TData> _sourceItems;

        public CollectionViewModel()
        {
            // We'll use the source items for sorting.
            _sourceItems = new CustomObservableCollection<TData>();

            // We'll use the effective items for filtering and paging.
            _effectiveItems = new CustomObservableCollection<TData>(new ChainedCollectionSourceAdapter(_sourceItems));
            _effectiveItems.CollectionChanged += OnItemsCollectionChanged;

            Pager = new PagingManager();
            Pager.PageSize = 0;
            Pager.PageChanged += new EventHandler<EventArgs>(OnPagerPageChanged);
            Pager.RefreshRequested += new EventHandler(OnPagerRefreshRequested);

            //MoveToNextCommand = CommandFactory.Create(
            //   () => View.MoveCurrentToNext(),
            //   () => CanMoveToNext());

            //MoveToPreviousCommand = CommandFactory.Create(
            //    () => View.MoveCurrentToPrevious(),
            //    () => CanMoveToPrevious());

            //MoveToFirstCommand = CommandFactory.Create(
            //    () => MoveCurrentTo(this.First()),
            //    () => CanMoveToFirst());

            //MoveToLastCommand = CommandFactory.Create(
            //    () => MoveCurrentTo(this.Last()),
            //    () => CanMoveToLast());

            DeselectAllCommand = CommandFactory.Create(
                () => Deselect(), () => _isChangingAllowed && CurrentItem != null);
        }

        //public ICommandEx MoveToPreviousCommand { get; private set; }

        //public ICommandEx MoveToNextCommand { get; private set; }

        //public ICommandEx MoveToFirstCommand { get; private set; }

        //public ICommandEx MoveToLastCommand { get; private set; }

        public ICommandEx DeselectAllCommand { get; private set; }

        public string Name
        {
            get => _name;
            set => SetProp(ref _name, value);
        }
        string _name;

        /// <summary>
        /// Gets the number of items.
        /// </summary>
        public override int Count => _effectiveItems.Count;

        public bool IsEmpty => Count == 0;

        public TData this[int index] => _effectiveItems.ItemAt(index);

        public bool Contains(TData data)
        {
            return _effectiveItems.Contains(data);
        }

        public TData CurrentItem
        {
            get => _currentItem;
            set
            {
                if (SetProp(ref _currentItem, value))
                {
                    OnCurrentItemChanged(this, EventArgs.Empty);
                }
            }
        }
        TData _currentItem;

        TData _previousCurrent;

        protected virtual void OnCurrentItemChanged(object sender, EventArgs e)
        {
            CheckNotDisposed();

            // TODO: Use equality comparer.
            if (_previousCurrent == CurrentItem)
            {
                return;
            }

            if (IsCurrentItemPropertyChangedNoficiationEnabled)
            {
                DetachFromPreviousCurrentItem();
            }

            _previousCurrent = CurrentItem;

            UpdateCommands();
            DeselectAllCommand.RaiseCanExecuteChanged();

            RaiseCurrentItemChanged();

            if (IsCurrentItemPropertyChangedNoficiationEnabled)
            {
                AttachToCurrentItem();
            }
        }

        protected void RaiseCurrentItemChanged()
        {
            RaisePropertyChanged(nameof(CurrentItem));

            CurrentChanged?.Invoke(this, EventArgs.Empty);
        }

        public override bool IsReadOnly => _sourceItems.IsReadOnly;

        public override bool CanAdd => !IsReadOnly && _effectiveItems.CanAdd && _sourceItems.CanAdd;

        public override bool CanRemove => !IsReadOnly && _effectiveItems.CanAdd && _sourceItems.CanAdd;

        /// <summary>
        /// Sets the given collection as the source collection.
        /// This one is handy when having multiple observable collections and
        /// you want to switch between those.
        /// </summary>        
        public void SetSource(CustomObservableCollection<TData> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            _effectiveItems.BeginUpdate();
            try
            {
                _sourceItems = collection;
                _effectiveItems.SetSource(new ChainedCollectionSourceAdapter(collection));
            }
            finally
            {
                _effectiveItems.EndUpdate();
            }
        }

        void OnPagerRefreshRequested(object sender, EventArgs e)
        {
            // TODO: IMPL.
        }

        void OnPagerPageChanged(object sender, EventArgs e)
        {
            // TODO: IMPL.
        }

        /// <summary>
        /// Sorts the items temporarily using the given key selector.
        /// </summary>        
        public void ApplySortOnce<TKey>(Func<TData, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            SetSortedItems(_sourceItems.OrderBy<TData, TKey>(keySelector, comparer).Cast<object>().ToArray());
        }

        /// <summary>
        /// Sorts the items temporarily using the given comparer.
        /// </summary>        
        public void ApplySortOnce(IComparer comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            object[] sortedItems = _sourceItems.ToObjectArray();
            Array.Sort(sortedItems, comparer);

            SetSortedItems(sortedItems);
        }

        void SetSortedItems(object[] sortedItems)
        {
            // Note that we sort the *source* items.
            _effectiveItems.BeginUpdate();
            try
            {
                _sourceItems.BeginUpdate();
                try
                {
                    _sourceItems.Clear();
                    _sourceItems.AddRangeLocal(sortedItems);
                }
                finally
                {
                    _sourceItems.EndUpdate();
                }
            }
            finally
            {
                _effectiveItems.EndUpdate();
            }
        }

        /// <summary>
        /// Filters the items. Note that this is just a temporary filter which
        /// only be applied once. Whenever the source collection changes,
        /// the filter will be erased.
        /// </summary>
        public void ApplyFilterOnce(Predicate<object> predicate)
        {
            RebuildEffectiveItems(predicate);
        }

        public PagingManager Pager { get; private set; }

        void RebuildEffectiveItems(Predicate<object> predicate)
        {
            _effectiveItems.BeginUpdate();
            try
            {
                int counter = 0;
                int max = Pager.PageSize;
                _effectiveItems.ClearLocal();
                foreach (var item in _sourceItems)
                {
                    if (predicate != null && predicate(item) == false)
                        continue;

                    _effectiveItems.AddLocal(item);

                    counter++;

                    if (max != 0 && counter >= max)
                        break;
                }
            }
            finally
            {
                _effectiveItems.EndUpdate();
            }
        }

        public bool MoveCurrentToFirst()
        {
            var first = _effectiveItems.FirstOrDefault();
            if (first == null)
                return false;

            return MoveCurrentTo(first);
        }

        public bool MoveCurrentTo(TData data)
        {
            CurrentItem = data;
            // TODO: REMOVE when clarified what to do with GetViewItemOfData.
            //if (data == null)
            //{
            //    CurrentItem = null;
            //}
            //else
            //{
            //    return _effectiveItems.MoveCurrentTo(GetViewItemOfData(data));
            //}

            return true;
        }

        public bool MoveToPosition(int position)
        {
            if (position < 0 || position >= _effectiveItems.Count)
                return false;

            CurrentItem = _effectiveItems.ItemAt(position);

            return true;
        }

        /// <summary>
        /// Adds the given data item.
        /// </summary>
        /// <param name="data"></param>
        public void Add(TData data)
        {
            CheckNotDisposed();

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (IsReadOnly)
                throw new InvalidOperationException("This collection view model is read-only and cannot be added to.");

            _effectiveItems.Add(data);

            AddToView(data);
        }

        // TODO: Do we still need this?
        protected virtual void AddToView(TData data)
        {
            // NOP
        }

        // TODO: Do we still need this?
        protected virtual void InsertToView(int index, TData data)
        {
            // NOP
        }

        // TODO: Do we still need this?
        protected virtual object GetViewItemOfData(TData data)
        {
            return data;
        }

        public override void AddObject(object data)
        {
            Add((TData)data);
        }

        public override void RemoveObject(object data)
        {
            Remove((TData)data);
        }

        public virtual bool Remove(TData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (IsReadOnly)
                throw new InvalidOperationException("This collection view model is read-only and cannot be removed from.");

            bool result = false;
            _isRemoving = true;
            try
            {
                result = _effectiveItems.Remove(data);
            }
            finally
            {
                _isRemoving = false;
            }

            return result;
        }
        bool _isRemoving;

        /// <summary>
        /// Removes all items.
        /// Sets CurrentItem to null.
        /// </summary>
        public virtual void Clear(bool refreshView = false)
        {
            CheckNotDisposed();

            MoveToPosition(-1);
            _effectiveItems.Clear();
            if (refreshView)
            {
                // TODO: Raise collection changed event.
                // _view.Refresh();
            }
        }

        public void RefreshView()
        {
            // TODO: Raise collection changed event.?
            //_view.Refresh();
        }

        public void Insert(int index, TData data)
        {
            CheckNotDisposed();
            CheckNotReadOnly();

            _effectiveItems.Insert(index, data);

            InsertToView(index, data);
        }

        void CheckNotReadOnly()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("This collection view model is read-only and cannot be inserted into.");
        }

        public event EventHandler CurrentChanged;

        public event PropertyChangedEventHandler CurrentItemPropertyChanged;

        protected virtual void OnCurrentItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Note that we are setting this as the sender of the event.
            CurrentItemPropertyChanged?.Invoke(this, e);
        }

        public bool IsCurrentItemPropertyChangedNoficiationEnabled
        {
            get => _isCurrentItemPropertyChangedNoficiationEnabled;
            set
            {
                if (_isCurrentItemPropertyChangedNoficiationEnabled != value)
                {
                    _isCurrentItemPropertyChangedNoficiationEnabled = value;
                    if (value)
                        AttachToCurrentItem();
                    else
                        DetachFromPreviousCurrentItem();
                }
            }
        }
        bool _isCurrentItemPropertyChangedNoficiationEnabled;

        void AttachToCurrentItem()
        {
            if (CurrentItem is INotifyPropertyChanged propChanged)
                propChanged.PropertyChanged += OnCurrentItemPropertyChanged;
        }

        void DetachFromPreviousCurrentItem()
        {
            if (_previousCurrent is INotifyPropertyChanged propChanged)
                propChanged.PropertyChanged -= OnCurrentItemPropertyChanged;
        }

        public bool IsChangingAllowed
        {
            get => _isChangingAllowed;
            set
            {
                if (SetProp(ref _isChangingAllowed, value))
                {
                    UpdateCommands();
                }
            }
        }

        bool _isChangingAllowed = true;

        protected void UpdateCommands()
        {
            // TODO: IMPL
            //MoveToFirstCommand.RaiseCanExecuteChanged();
            //MoveToLastCommand.RaiseCanExecuteChanged();
            //MoveToPreviousCommand.RaiseCanExecuteChanged();
            //MoveToNextCommand.RaiseCanExecuteChanged();
            DeselectAllCommand.RaiseCanExecuteChanged();
        }

        public void Deselect()
        {
            CheckNotDisposed();

            if (!_isChangingAllowed)
                return;

            if (CurrentItem != null)
                MoveToPosition(-1);
        }

        public int IndexOf(TData item)
        {
            int idx = 0;
            bool found = false;
            foreach (var i in this)
            {
                if (object.Equals(i, item))
                {
                    found = true;
                    break;
                }
                idx++;
            }

            if (found)
                return idx;
            else
                return -1;
        }

        // IEnumerable ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public virtual IEnumerator<TData> GetEnumerator()
        {
            return ((IEnumerable<TData>)_effectiveItems).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

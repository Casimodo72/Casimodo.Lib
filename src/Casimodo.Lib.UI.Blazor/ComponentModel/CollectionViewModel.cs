using Casimodo.Lib.Presentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

// This is just a non functional stub in order to get rid of the WPF dependencies while moving to Blazor.

namespace Casimodo.Lib.UI
{
    public class CollectionView<TData> : List<TData>
    {
        public CollectionView(IEnumerable<TData> collection)
            : base(collection)
        { }

        public TData CurrentItem { get; set; }

        public bool MoveCurrentToPosition(int position)
        {
            // TODO
            return false;
        }

        public bool MoveCurrentTo(object data)
        {
            return false;
            // TODO:
        }

        public void Refresh()
        {
            // TODO
        }
    }

    public class CollectionViewModel<TData> : CollectionViewModel, IEnumerable<TData>
        where TData : class
    {
        protected CustomObservableCollection<TData> _effectiveItems;
        protected CustomObservableCollection<TData> _sourceItems;
        readonly CollectionView<TData> _view;

        public CollectionViewModel()
            : this(false, default)
        { }

        public CollectionViewModel(TData nullObject)
            : this(true, nullObject)
        { }

        protected CollectionViewModel(bool useNullObject, TData nullObject)
        {
            // We'll use the source items for sorting.
            _sourceItems = new CustomObservableCollection<TData>();

            // We'll use the effective items for filtering and paging.
            _effectiveItems = new CustomObservableCollection<TData>(new ChainedCollectionSourceAdapter(_sourceItems));
            _effectiveItems.CollectionChanged += OnItemsCollectionChanged;

            _view = CreateView();

            //_view.CurrentChanging += OnViewCurrentChanging;
            //_view.CurrentChanged += OnViewCurrentChanged;
            //((INotifyCollectionChanged)_view).CollectionChanged += OnItemsOrViewCollectionChanged;

            Pager = new PagingManager();
            Pager.PageSize = 0;
            // Pager.PageChanged += new EventHandler<EventArgs>(OnPagerPageChanged);
            // Pager.RefreshRequested += new EventHandler(OnPagerRefreshRequested);

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

            //DeselectAllCommand = CommandFactory.Create(
            //    () => Deselect(), () => _isChangingAllowed && CurrentItem != null);
        }

        protected virtual CollectionView<TData> CreateView()
        {
            return new CollectionView<TData>(_effectiveItems);
        }

        public CollectionView<TData> View => _view;

        public string Name
        {
            get { return _name; }
            set { SetProp(ref _name, value); }
        }
        string _name;

        /// <summary>
        /// Gets the number of items in the view.
        /// </summary>
        public override int Count
        {
            get
            {
                return _view.Count;
            }
        }

        public bool IsEmpty => Count == 0;

        public TData this[int index]
        {
            get { return _view[index]; }
        }

        public bool ViewContains(TData data)
        {
            return _view.Contains(data);
        }

        public TData CurrentItem
        {
            get { return GetCurrentItem(); }
        }

        protected virtual TData GetCurrentItem()
        {
            return (TData)_view.CurrentItem;
        }

        object _previousCurrent;

        /// <summary>
        /// Gets the enumeration over the items in the view.
        /// </summary>
        public IEnumerable<TData> Items
        {
            get { return _view.Cast<TData>(); }
        }

        public override bool IsReadOnly
        {
            get { return _sourceItems.IsReadOnly; }
        }

        public override bool CanAdd
        {
            get { return !IsReadOnly && _effectiveItems.CanAdd && _sourceItems.CanAdd; }
        }

        public override bool CanRemove
        {
            get { return !IsReadOnly && _effectiveItems.CanAdd && _sourceItems.CanAdd; }
        }

        /// <summary>
        /// Sets the given collection as the source collection.
        /// This one is handy when having multiple observable collections and
        /// you want to switch between those.
        /// </summary>        
        public void SetSource(CustomObservableCollection<TData> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

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

        /// <summary>
        /// Sorts the items temporarily using the given key selector.
        /// </summary>        
        public void ApplySortOnce<TKey>(Func<TData, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");

            SetSortedItems(_sourceItems.OrderBy<TData, TKey>(keySelector, comparer).Cast<object>().ToArray());
        }

        /// <summary>
        /// Sorts the items temporarily using the given comparer.
        /// </summary>        
        public void ApplySortOnce(IComparer comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

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
            // Note that we apply the filter on the *effective* items
            // which are directly used by the view.
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
            var first = _view.FirstOrDefault();
            if (first == null)
                return false;

            return MoveCurrentTo(first);
        }

        public bool MoveCurrentTo(TData data)
        {
            if (data == null)
                return _view.MoveCurrentTo(null);

            return _view.MoveCurrentTo(GetViewItemOfData(data));
        }

        public bool MoveToPosition(int position)
        {
            return _view.MoveCurrentToPosition(position);
        }

        /// <summary>
        /// Adds the given data item to the view.
        /// </summary>
        /// <param name="data"></param>
        public void Add(TData data)
        {
            CheckNotDisposed();

            if (data == null)
                throw new ArgumentNullException("data");

            if (IsReadOnly)
                throw new InvalidOperationException("This collection view model is read-only and cannot be added to.");

            _effectiveItems.Add(data);

            AddToView(data);
        }

        protected virtual void AddToView(TData data)
        {
            // NOP
        }

        protected virtual void InsertToView(int index, TData data)
        {
            // NOP
        }

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
                throw new ArgumentNullException("data");

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
        /// Removes all items from the view.
        /// Sets CurrentItem to null.
        /// </summary>
        public virtual void Clear(bool refreshView = false)
        {
            CheckNotDisposed();

            _view.MoveCurrentToPosition(-1);
            _effectiveItems.Clear();
            if (refreshView)
                _view.Refresh();
        }

        public void RefreshView()
        {
            _view.Refresh();
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
            get { return _isCurrentItemPropertyChangedNoficiationEnabled; }
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
            INotifyPropertyChanged propChanged = _view.CurrentItem as INotifyPropertyChanged;
            if (propChanged != null)
                propChanged.PropertyChanged += OnCurrentItemPropertyChanged;
        }

        void DetachFromPreviousCurrentItem()
        {
            INotifyPropertyChanged propChanged = _previousCurrent as INotifyPropertyChanged;
            if (propChanged != null)
                propChanged.PropertyChanged -= OnCurrentItemPropertyChanged;
        }

        public bool IsChangingAllowed
        {
            get { return _isChangingAllowed; }
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
            //DeselectAllCommand.RaiseCanExecuteChanged();
        }

        public void Deselect()
        {
            CheckNotDisposed();

            if (!_isChangingAllowed)
                return;

            if (_view.CurrentItem != null)
                _view.MoveCurrentToPosition(-1);
        }

        public int PositionOf(TData item)
        {
            int pos = 0;
            bool found = false;
            foreach (var i in this)
            {
                if (object.Equals(i, item))
                {
                    found = true;
                    break;
                }
                pos++;
            }

            if (found)
                return pos;
            else
                return -1;
        }

        // IEnumerable ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public virtual IEnumerator<TData> GetEnumerator()
        {
            return ((IEnumerable<TData>)_view).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

// Copyright (c) 2009 Kasimier Buchcik

using Casimodo.Lib.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

#if (WINDOWS_UWP)
    using Windows.UI.Xaml.Data;
    //using MyView = Windows.UI.Xaml..ListCollectionView;
#else
    using System.Windows.Data;
    using MyView = System.Windows.Data.ListCollectionView;
#endif

namespace Casimodo.Lib.Presentation
{
    public class CollectionViewModel<TData> : CollectionViewModel, IEnumerable<TData>
    {
        protected static readonly PropertyChangedEventArgs CurrentItemChangedArgs = new PropertyChangedEventArgs("CurrentItem");
        protected static readonly PropertyChangedEventArgs NameChangedArgs = new PropertyChangedEventArgs("Name");
        protected CustomObservableCollection<TData> _effectiveItems;
        protected CustomObservableCollection<TData> _sourceItems;
        protected MyView _view;
        protected MyView _extendedView;
        protected BindingInfo _binding;
        protected bool _isCurrentChangingCancelable;

        public event EventHandler CurrentChanged;

        public event CurrentChangingEventHandler CurrentChanging;

        public CompositeCollection _compositeCollection;

        protected class BindingInfo
        {
            public PropertyInfo Property;
            public object Source;
            public Func<TData, object> ItemValueSelector;
            public bool IsUpdatingFromSource;
            public bool IsUpdatingToSource;
        }

        public CollectionViewModel()
            : this(false, default(TData))
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

            if (useNullObject)
            {
                _compositeCollection = new CompositeCollection();
                _compositeCollection.Add(nullObject);
                _compositeCollection.Add(new CollectionContainer { Collection = _effectiveItems });
                _view = new MyView(_compositeCollection);
            }
            else
            {
                _view = CreateView();
            }

            _view.CurrentChanging += OnViewCurrentChanging;
            _view.CurrentChanged += OnViewCurrentChanged;
            ((INotifyCollectionChanged)_view).CollectionChanged += OnItemsOrViewCollectionChanged;

            Pager = new PagingManager();
            Pager.PageSize = 0;
            Pager.PageChanged += new EventHandler<EventArgs>(OnPagerPageChanged);
            //Pager.PageChanging
            Pager.RefreshRequested += new EventHandler(OnPagerRefreshRequested);

            MoveToNextCommand = CommandFactory.Create(
               () => View.MoveCurrentToNext(),
               () => CanMoveToNext());

            MoveToPreviousCommand = CommandFactory.Create(
                () => View.MoveCurrentToPrevious(),
                () => CanMoveToPrevious());

            MoveToFirstCommand = CommandFactory.Create(
                () => MoveCurrentTo(this.First()),
                () => CanMoveToFirst());

            MoveToLastCommand = CommandFactory.Create(
                () => MoveCurrentTo(this.Last()),
                () => CanMoveToLast());

            DeselectAllCommand = CommandFactory.Create(
                () => Deselect(), () => _isChangingAllowed && CurrentItem != null);
        }

        public string Name
        {
            get { return _name; }
            set { SetProp(ref _name, value, NameChangedArgs); }
        }
        string _name;

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

        void OnPagerRefreshRequested(object sender, EventArgs e)
        {
            // TODO: IMPL.
        }

        void OnPagerPageChanged(object sender, EventArgs e)
        {
            // TODO: IMPL.
        }

        public override bool IsReadOnly
        {
            get { return _sourceItems.IsReadOnly; }
        }

        public PagingManager Pager { get; private set; }

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

        public TData this[int position]
        {
            get { return this.ElementAt(position); }
        }

        protected override void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCommands();
            base.OnItemsCollectionChanged(sender, e);
        }

        protected override void OnItemsOrViewCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCommands();
            base.OnItemsOrViewCollectionChanged(sender, e);
        }

        public TData CurrentItem
        {
            get { return GetCurrentItem(); }
        }

        public MyView View
        {
            get { return _view; }
        }

        public MyView ExtendedView
        {
            get { return _extendedView; }
        }

        public virtual IList ViewItems
        {
            get { return _effectiveItems; }
        }

        /// <summary>
        /// Indicates whether the position of the current item in the collection can be changed.
        /// </summary>
        public bool IsChangingAllowed
        {
            get { return _isChangingAllowed; }
            set
            {
                if (SetProp(ref _isChangingAllowed, value, IsChangingAllowedChangedArgs))
                {
                    UpdateCommands();
                }
            }
        }

        bool _isChangingAllowed = true;
        protected static readonly PropertyChangedEventArgs IsChangingAllowedChangedArgs = new PropertyChangedEventArgs("IsChangingAllowed");
        public override bool CanAdd
        {
            get { return !IsReadOnly && _effectiveItems.CanAdd && _sourceItems.CanAdd; }
        }

        public override bool CanRemove
        {
            get { return !IsReadOnly && _effectiveItems.CanAdd && _sourceItems.CanAdd; }
        }

        public bool CanMoveToNext()
        {
            return Count != 0 && _isChangingAllowed && View.CurrentPosition < Count - 1;
        }

        public bool CanMoveToPrevious()
        {
            return Count != 0 && _isChangingAllowed && View.CurrentPosition > 0;
        }

        public bool CanMoveToFirst()
        {
            return CanMoveToPrevious();
        }

        public bool CanMoveToLast()
        {
            return CanMoveToNext();
        }

        protected void UpdateCommands()
        {
            MoveToFirstCommand.RaiseCanExecuteChanged();
            MoveToLastCommand.RaiseCanExecuteChanged();
            MoveToPreviousCommand.RaiseCanExecuteChanged();
            MoveToNextCommand.RaiseCanExecuteChanged();
            DeselectAllCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Gets the enumeration over the items in the view.
        /// </summary>
        public IEnumerable<TData> Items
        {
            get { return _view.Cast<TData>(); }
        }

        public void Deselect()
        {
            CheckNotDisposed();

            if (!_isChangingAllowed)
                return;

            if (_view.CurrentItem != null)
                _view.MoveCurrentToPosition(-1);
        }

        public TData FindMod(object data)
        {
            if (!typeof(ItemViewModel).IsAssignableFrom(typeof(TData)))
                return default(TData);

            ItemViewModel m;
            foreach (var item in _sourceItems)
            {
                m = item as ItemViewModel;
                if (m != null && object.Equals(m.DataObject, data))
                    return (TData)(object)m;
            }

            return default(TData);
        }

        //public T FindData<T>(TData model)
        //    where T: class
        //{
        //    if (!typeof(ItemViewModel).IsAssignableFrom(typeof(TData)))
        //        return null;

        //}

        public bool MoveCurrentToFirst()
        {
            var first = this.FirstOrDefault();
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
        /// Gets the number of items in the view.
        /// </summary>
        public override int Count
        {
            get
            {
                if (_view.NewItemPlaceholderPosition == NewItemPlaceholderPosition.None)
                    return _view.Count;
                else
                    return _view.Count - 1;
            }
        }

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

        public bool IsEmpty
        {
            get { return _view.IsEmpty; }
        }

        public bool IsRemoving
        {
            get { return _isRemoving; }
        }

        protected bool _isRemoving;

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

        public void AddRange(IEnumerable<TData> data)
        {
            if (data == null)
                return;
            foreach (var item in data)
                Add(item);
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

        public override void AddObject(object data)
        {
            Add((TData)data);
        }

        public override void RemoveObject(object data)
        {
            Remove((TData)data);
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

        public void RefreshView()
        {
            _view.Refresh();
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

        protected virtual object GetViewItemOfData(TData data)
        {
            return data;
        }

        protected virtual void AddToView(TData data)
        { }

        protected virtual void InsertToView(int index, TData data)
        { }

        protected virtual TData GetCurrentItem()
        {
            return (TData)_view.CurrentItem;
        }

        protected virtual MyView CreateView()
        {
            return new MyView(_effectiveItems);
        }

        protected virtual void OnViewCurrentChanging(object sender, CurrentChangingEventArgs e)
        {
            CheckNotDisposed();

            _isCurrentChangingCancelable = e.IsCancelable;

            if (!_isChangingAllowed)
            {
                if (!e.IsCancelable)
                    throw new InvalidOperationException(
                        "The collection view model does not allow changing, " +
                        "but the underlying collection view want to change without being cancelable.");

                e.Cancel = true;

                return;
            }

            if (CurrentChanging != null)
                CurrentChanging(this, e);
        }

        object _previousCurrent;

        protected virtual void OnViewCurrentChanged(object sender, EventArgs e)
        {
            CheckNotDisposed();

            if (_previousCurrent == _view.CurrentItem)
            {
                // Yes, we do want to be *only* notified when the current item *really* changes.
                return;
            }

            if (IsCurrentItemPropertyChangedNoficiationEnabled)
            {
                DetachFromPreviousCurrentItem();
            }

            _previousCurrent = _view.CurrentItem;

            UpdateCommands();
            DeselectAllCommand.RaiseCanExecuteChanged();

            UpdateToSource((TData)_view.CurrentItem);

            RaiseCurrentItemChanged();

            if (IsCurrentItemPropertyChangedNoficiationEnabled)
            {
                AttachToCurrentItem();
            }
        }

        protected virtual void OnCurrentItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Note that we are setting this as the sender of the event.
            if (CurrentItemPropertyChanged != null)
                CurrentItemPropertyChanged(this, e);
        }

        public event PropertyChangedEventHandler CurrentItemPropertyChanged;

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

        protected void RaiseCurrentItemChanged()
        {
            RaisePropertyChanged(CurrentItemChangedArgs);

            var handler = CurrentChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        // Binding stuff ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public void BindSelectedValue(INotifyPropertyChanged source, string sourcePropertyName,
            Func<TData, object> itemValueSelector)
        {
            CheckNotDisposed();

            if (source == null)
                throw new ArgumentNullException("source");
            if (string.IsNullOrWhiteSpace(sourcePropertyName))
                throw new ArgumentNullException("sourcePropertyName");
            if (itemValueSelector == null)
                throw new ArgumentNullException("itemValueSelector");

            if (_binding == null)
                _binding = new BindingInfo();

            if (_binding.Source != null)
                throw new InvalidOperationException(
                    "A source is already assigned to the LookupCollectionViewModel.");

            Type type = ((object)source).GetType();
            ObservableObject.ValidatePropertyExistance(type, sourcePropertyName);

            _binding.Source = (object)source;
            _binding.Property = source.GetTypeProperty(sourcePropertyName);
            _binding.ItemValueSelector = itemValueSelector;

            // Subscribe to property changes of the source.
            ((INotifyPropertyChanged)_binding.Source).PropertyChanged += OnSourcePropertyChanged;

            UpdateFromSource();
        }

        /// <summary>
        /// Called when the property of the source changes.
        /// </summary>
        void OnSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CheckNotDisposed();

            if (_binding == null)
                return;

            if (_binding.IsUpdatingToSource)
                return;

            if (e.PropertyName != _binding.Property.Name)
                return;

            UpdateFromSource();
        }

        public void UpdateSelectionFromSource()
        {
            UpdateFromSource();
        }

        void UpdateFromSource()
        {
            if (_binding == null)
                return;

            // Get the current value from the source.
            object sourceValue = _binding.Property.GetValue(_binding.Source, null);

            // Find a matching value in the data collection.
            TData selectedData =
                _effectiveItems
                .Cast<TData>()
                .FirstOrDefault((a) => object.Equals(_binding.ItemValueSelector(a), sourceValue));

            _binding.IsUpdatingFromSource = true;
            try
            {
                if (selectedData == null)
                    _view.MoveCurrentToPosition(-1);
                else
                    _view.MoveCurrentTo(GetViewItemOfData(selectedData));
            }
            finally
            {
                _binding.IsUpdatingFromSource = false;
            }
        }

        protected void UpdateToSource(TData data)
        {
            if (_binding == null)
                return;

            if (_binding.Source == null)
                return;

            if (_binding.IsUpdatingFromSource)
                return;

            object value = null;

            if (data == null)
            {
                // TODO: REVISIT:
                // Nasty scenario: When the View is being attached to a UI-control,
                // then the View will automatically raise a CurrentChanged event with
                // CurrentItem == null, effectively erasing any existing value on the source.
                // We don't want that, so we currently skip all non-cancellable movement.
                // Note that, if the user changed the current item vie the UI, then
                // those movements will be cancellable. So at least we can distinguish
                // those scenarios a bit.
                if (!_isCurrentChangingCancelable)
                    return;
            }

            // If the curren item is null, then we'll use null as the value to be set.
            if (data != null)
                value = _binding.ItemValueSelector(data);

            // Set this value on the source.
            _binding.IsUpdatingToSource = true;
            try
            {
                _binding.Property.SetValue(_binding.Source, value, null);
            }
            finally
            {
                _binding.IsUpdatingToSource = false;
            }
        }

        // IEnumerable ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public virtual IEnumerator<TData> GetEnumerator()
        {
            IEnumerator enumerator = ((IEnumerable)_view).GetEnumerator();
            TData cur;
            while (enumerator.MoveNext())
            {
                cur = (TData)enumerator.Current;

                // We must not return the CollectionView.NewItemPlaceholder.
                if (cur != null)
                    yield return cur;
            }

            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // Commands ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public ICommandEx MoveToPreviousCommand { get; private set; }

        public ICommandEx MoveToNextCommand { get; private set; }

        public ICommandEx MoveToFirstCommand { get; private set; }

        public ICommandEx MoveToLastCommand { get; private set; }

        public ICommandEx DeselectAllCommand { get; private set; }

        // Disposing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected override void OnDispose()
        {
            base.OnDispose();

            if (IsCurrentItemPropertyChangedNoficiationEnabled)
            {
                DetachFromPreviousCurrentItem();
            }

            if (_view != null)
            {
                _view.CurrentChanging -= OnViewCurrentChanging;
                _view.CurrentChanged -= OnViewCurrentChanged;
                ((INotifyCollectionChanged)_view).CollectionChanged -= OnItemsOrViewCollectionChanged;
            }
            _view = null;

            _effectiveItems = null;

            if (_binding != null && _binding.Source != null)
            {
                var src = _binding.Source;
                // Unsubscribe from source.
                ((INotifyPropertyChanged)src).PropertyChanged -= OnSourcePropertyChanged;
            }
            _binding = null;
            CurrentChanged = null;
        }
    }
}
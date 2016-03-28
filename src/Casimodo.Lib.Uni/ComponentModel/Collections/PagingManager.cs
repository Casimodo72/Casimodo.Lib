// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.ComponentModel;
using Casimodo.Lib.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    /// <summary>
    /// Encapsulates paging logic and states.
    /// </summary>
    public class PagingManager : ObservableObject
#if (SILVERLIGHT)
        ,IPagedCollectionView
#endif
    {
        static readonly public PropertyChangedEventArgs ItemCountChangedArgs = new PropertyChangedEventArgs("ItemCount");
        static readonly public PropertyChangedEventArgs TotalItemCountChangedArgs = new PropertyChangedEventArgs("TotalItemCount");
        static readonly public PropertyChangedEventArgs PageIndexChangedArgs = new PropertyChangedEventArgs("PageIndex");
        static readonly public PropertyChangedEventArgs PageSizeChangedArgs = new PropertyChangedEventArgs("PageSize");
        static readonly public PropertyChangedEventArgs CanChangePageChangedArgs = new PropertyChangedEventArgs("CanChangePage");
        static readonly public PropertyChangedEventArgs IsPageChangingChangedArgs = new PropertyChangedEventArgs("IsPageChanging");

        public PagingManager()
        {
            _pageSize = 0;
            _pageIndex = -1;
        }

        public event EventHandler RefreshRequested;

        /// <summary>
        /// Used to ask the consumer whether it's OK to change the page size.
        /// </summary>
        public event CancelableChangingEventHandler<int> PageSizeChanging;

        /// <summary>
        /// Indicates whether the paging manager is frozen, i.e. does not allow changing states.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
            set
            {
                if (_isFrozen != value)
                {
                    _isFrozen = value;
                    RaisePropertyChanged(CanChangePageChangedArgs);
                }
            }
        }
        bool _isFrozen;


        #region IPagedCollectionView ------------------------------------------

        /// <summary>
        /// Member of IPagedCollectionView.
        /// </summary>
        public bool CanChangePage
        {
            get { return !IsFrozen; }
        }

        /// <summary>
        /// Member of IPagedCollectionView.
        /// </summary>
        public bool IsPageChanging
        {
            get { return _isPageChanging; }
            private set { SetValueTypeProperty(IsPageChangingChangedArgs, ref _isPageChanging, value); }
        }
        bool _isPageChanging;

        /// <summary>
        /// Gets the number of known items in the view before paging is applied.
        /// Member of IPagedCollectionView.
        /// </summary>
        public int ItemCount
        {
            get { return _itemCount; }
        }
        int _itemCount;

        public void SetItemCount(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException();

            CheckNotFrozen();
            SetValueTypeProperty(ItemCountChangedArgs, ref _itemCount, value);
        }

        /// <summary>
        /// Paging is disabled when PageIndex is -1.
        /// Member of IPagedCollectionView.
        /// </summary>
        public int PageIndex
        {
            get { return _pageIndex; }
        }
        int _pageIndex;

        /// <summary>
        /// Indicates the number of items in a page.
        /// Paging is disabled when PageSize is 0.
        /// Member of IPagedCollectionView.
        /// </summary>
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException();

                if (_pageSize == value)
                    return;

                CheckNotFrozen();

                // Ask the consumer whether it's OK to change the PageSize.
                if (!RaisePageSizeChanging(value))
                    return;

                SetValueTypeProperty(PageSizeChangedArgs, ref _pageSize, value);

                if (_pageSize == 0)
                {
                    // Paging is disabled when PageSize is 0. The PageIndex becomes -1 in this case.
                    MoveToPage(-1);
                }
                else if (_pageIndex != 0)
                {
                    // When PageSize changes, then this effectively resets the paging
                    // and we'll move to the first page.
                    MoveToPage(0);
                }
            }
        }
        int _pageSize;

        /// <summary>
        /// The total number of items in the view before paging is applied, or -1 if
        //  the total number is unknown.
        /// Member of IPagedCollectionView.
        /// </summary>
        public int TotalItemCount
        {
            get { return _totalItemCount; }
        }
        int _totalItemCount;

        public void SetTotalItemCount(int value)
        {
            if (value < -1)
                throw new ArgumentOutOfRangeException();

            CheckNotFrozen();
            SetValueTypeProperty(TotalItemCountChangedArgs, ref _totalItemCount, value);
        }

        /// <summary>
        /// Member of IPagedCollectionView.
        /// </summary>
        public event EventHandler<EventArgs> PageChanged;

        /// <summary>
        /// Member of IPagedCollectionView.
        /// </summary>
        public event EventHandler<PageChangingEventArgs> PageChanging;

        /// <summary>
        /// Member of IPagedCollectionView.
        /// </summary>
        public bool MoveToFirstPage()
        {
            return MoveToPage(0);
        }

        /// <summary>
        /// Member of IPagedCollectionView.
        /// </summary>
        public bool MoveToLastPage()
        {
            return MoveToPage(TotalItemCount / PageSize);
        }

        /// <summary>
        /// Member of IPagedCollectionView.
        /// </summary>
        public bool MoveToNextPage()
        {
            return MoveToPage(PageIndex + 1);
        }

        /// <summary>
        /// Member of IPagedCollectionView.
        /// </summary>
        public bool MoveToPage(int index)
        {
            if (index < -1)
                throw new ArgumentOutOfRangeException("index",
                    "The given page index must be greater than or equal to -1.");

            if (index == _pageIndex)
                return false;

            // A PageIndex of -1 is only allowed when the paging was disabled; e.g. when PageSize is 0.
            if ((index == -1) && (_pageSize > 0))
                return false;

            // Evaluate if the given index is beyond the number of available pages.
            // Note that TotalItemCount *can* be -1 (e.g. when using RIA Services),
            // so we must allow to move to *any* page in this case.
            if ((index != 0) && (_totalItemCount != -1) && (index > _totalItemCount / _pageSize))
                throw new ArgumentOutOfRangeException(
                    "PageIndex must be less than the page count");

            if (IsFrozen)
                return false;

            PageChangingEventArgs args = new PageChangingEventArgs(index);

            try
            {
                IsPageChanging = true;

                RaisePageChanging(args);

                if (!args.Cancel)
                {
                    int oldIndex = _pageIndex;

                    _pageIndex = index;
                    try
                    {
                        // Refresh.                       
                        RaiseRefreshRequested();
                    }
                    catch
                    {
                        // Restore page index if refresh failed.
                        // TODO: If the restored index is still inconsistent with the currently
                        //   loaded page then we'll still have a problem.
                        //   This means that consumer must compensate for inconsistency anyway.
                        _pageIndex = oldIndex;
                        throw;
                    }

                    RaisePropertyChanged(PageIndexChangedArgs);
                    RaisePageChanged();

                    return true;
                }

                return false;
            }
            finally
            {
                IsPageChanging = false;
            }
        }

        /// <summary>
        /// Member of IPagedCollectionView.
        /// </summary>
        public bool MoveToPreviousPage()
        {
            return MoveToPage(PageIndex - 1);
        }

        #endregion

        /// <summary>
        /// Returns false if the change was cancelled.
        /// </summary>
        bool RaisePageSizeChanging(int newPageSize)
        {
            var handler = PageSizeChanging;
            if (handler == null)
                return true;

            var args = new CancelableChangingEventArgs<int>(_pageSize, newPageSize, true);
            handler(this, args);

            return !args.IsCancelled;
        }

        void RaisePageChanging(PageChangingEventArgs e)
        {
            var handler = this.PageChanging;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        void RaisePageChanged()
        {
            var handler = this.PageChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        void RaiseRefreshRequested()
        {
            var handler = this.RefreshRequested;
            // We really want the handler to be assigned.
            if (handler == null)
                throw new InvalidOperationException("There's no handler assigned for the event 'RefreshRequested'.");

            handler(this, EventArgs.Empty);
        }

        void CheckNotFrozen()
        {
            if (IsFrozen)
                throw new InvalidOperationException("Paging related stated cannot be modified currently.");
        }
    }
}
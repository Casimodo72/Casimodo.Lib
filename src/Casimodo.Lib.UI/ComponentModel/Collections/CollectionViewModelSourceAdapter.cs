// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Collections;
using System.Collections.Generic;

namespace Casimodo.Lib.UI
{
    /// <summary>
    /// This adapter is intended to chain a CustomObservableCollection
    /// with an underlying CollectionViewModel.
    /// </summary>
    public class CollectionViewModelSourceAdapter<T> : CollectionSourceAdapterBase<T>
        where T : class
    {
        public CollectionViewModelSourceAdapter(CollectionViewModelBase<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _source.ItemsCollectionChanged += OnSourceCollectionChanged;
        }

        readonly CollectionViewModelBase<T> _source;

        public override int Count => _source.Count;

        public override void Add(T item)
        {
            _source.AddObject(item);
        }

        public override void Remove(T item)
        {
            _source.RemoveObject(item);
        }

        public override bool IsReadOnly => _source.IsReadOnly;

        public override bool CanAdd => _source.CanAdd;

        public override bool CanRemove => _source.CanRemove;

        protected override IEnumerator<T> GetEnumeratorInternal()
        {
            return ((IEnumerable<T>)_source).GetEnumerator();
        }
    }
}
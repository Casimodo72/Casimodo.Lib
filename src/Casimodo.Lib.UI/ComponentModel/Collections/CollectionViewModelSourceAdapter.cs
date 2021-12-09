// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Collections;

namespace Casimodo.Lib.Presentation
{
    /// <summary>
    /// This adapter is intended to chain a CustomObservableCollection
    /// with an underlying CollectionViewModel.
    /// </summary>
    public class CollectionViewModelSourceAdapter : CollectionSourceAdapterBase
    {
        public CollectionViewModelSourceAdapter(CollectionViewModel source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _source.ItemsCollectionChanged += OnSourceCollectionChanged;
        }

        readonly CollectionViewModel _source;

        public override int Count => _source.Count;

        public override void Add(object item)
        {
            _source.AddObject(item);
        }

        public override void Remove(object item)
        {
            _source.RemoveObject(item);
        }

        public override bool IsReadOnly => _source.IsReadOnly;

        public override bool CanAdd => _source.CanAdd;

        public override bool CanRemove => _source.CanRemove;

        protected override IEnumerator GetEnumeratorInternal()
        {
            return ((IEnumerable)_source).GetEnumerator();
        }
    }
}
// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Collections.Generic;

namespace Casimodo.Lib.UI
{
    /// <summary>
    /// This adapter is intended to chain a CustomObservableCollection
    /// with another underlying CustomObservableCollection.
    /// </summary>
    public class ChainedCollectionSourceAdapter<T> : CollectionSourceAdapterBase<T>
        where T : class
    {
        public ChainedCollectionSourceAdapter(CustomObservableCollection<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _source.CollectionChanged += OnSourceCollectionChanged;
        }

        readonly CustomObservableCollection<T> _source;

        public override int Count => _source.Count;

        public override void Add(T item)
        {
            _source.Add(item);
        }

        public override void Remove(T item)
        {
            _source.Remove(item);
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
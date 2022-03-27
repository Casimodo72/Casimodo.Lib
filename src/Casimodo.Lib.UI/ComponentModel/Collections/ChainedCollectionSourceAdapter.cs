// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Collections;

namespace Casimodo.Lib.UI
{
    /// <summary>
    /// This adapter is intended to chain a CustomObservableCollection
    /// with another underlying CustomObservableCollection.
    /// </summary>
    public class ChainedCollectionSourceAdapter : CollectionSourceAdapterBase
    {
        public ChainedCollectionSourceAdapter(CustomObservableCollection source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _source.CollectionChanged += OnSourceCollectionChanged;
        }

        readonly CustomObservableCollection _source;

        public override int Count => _source.Count;

        public override void Add(object item)
        {
            _source.Add(item);
        }

        public override void Remove(object item)
        {
            _source.Remove(item);
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
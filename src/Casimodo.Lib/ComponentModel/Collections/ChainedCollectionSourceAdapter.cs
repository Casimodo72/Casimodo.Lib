// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Collections;

namespace Casimodo.Lib.Presentation
{
    /// <summary>
    /// This adapter is intended to chain a CustomObservableCollection
    /// with another underlying CustomObservableCollection.
    /// </summary>
    // TODO: Never tried this one in Silverlight yet.
    public class ChainedCollectionSourceAdapter : CollectionSourceAdapterBase
    {
        public ChainedCollectionSourceAdapter(CustomObservableCollection source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            _source = source;
            _source.CollectionChanged += OnSourceCollectionChanged;
        }

        CustomObservableCollection _source;

        public override int Count
        {
            get { return _source.Count; }
        }

        public override void Add(object item)
        {
            _source.Add(item);
        }

        public override void Remove(object item)
        {
            _source.Remove(item);
        }

        public override bool IsReadOnly
        {
            get { return _source.IsReadOnly; }
        }

        public override bool CanAdd
        {
            get { return _source.CanAdd; }
        }

        public override bool CanRemove
        {
            get { return _source.CanRemove; }
        }

        protected override IEnumerator GetEnumeratorInternal()
        {
            return ((IEnumerable)_source).GetEnumerator();
        }
    }
}
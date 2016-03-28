// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Casimodo.Lib.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    /// <summary>
    /// This adapter is intended to chain a CustomObservableCollection
    /// with an underlying CollectionViewModel.
    /// </summary>
    // TODO: Never tried this one in Silverlight yet.
    public class CollectionViewModelSourceAdapter : CollectionSourceAdapterBase
    {
        public CollectionViewModelSourceAdapter(CollectionViewModel source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            _source = source;
            _source.ItemsOrViewCollectionChanged += OnSourceCollectionChanged;
        }

        CollectionViewModel _source;

        public override int Count
        {
            get { return _source.Count; }
        }

        public override void Add(object item)
        {
            _source.AddObject(item);
        }

        public override void Remove(object item)
        {
            _source.RemoveObject(item);
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
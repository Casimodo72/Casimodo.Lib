// Copyright (c) 2009 Kasimier Buchcik

using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.UI
{
    /// <summary>
    /// This adapter is intended to short-circuit a root CustomObservableCollection.
    /// </summary>
    public class ShortedCollectionSourceAdapter<T> : CollectionSourceAdapterBase<T>
        where T : class
    {
        public ShortedCollectionSourceAdapter()
        { }

        public CustomObservableCollection<T> Items { get; set; }

        public override int Count => Items?.Count ?? 0;

        public override void Add(T item)
        {
            // NOP.
        }

        public override void Remove(T item)
        {
            // NOP.
        }

        public override bool IsReadOnly => false;

        public override bool CanAdd => true;

        public override bool CanRemove => true;

        protected override IEnumerator<T> GetEnumeratorInternal()
        {
            return ((IEnumerable<T>)Items)?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();
        }
    }
}
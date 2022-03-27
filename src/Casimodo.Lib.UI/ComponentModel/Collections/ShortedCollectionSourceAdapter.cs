// Copyright (c) 2009 Kasimier Buchcik

using System.Collections;
using System.Linq;

namespace Casimodo.Lib.UI
{
    /// <summary>
    /// This adapter is intended to short-circuit a root CustomObservableCollection.
    /// </summary>
    public class ShortedCollectionSourceAdapter : CollectionSourceAdapterBase
    {
        public ShortedCollectionSourceAdapter()
        { }

        public CustomObservableCollection Items { get; set; }

        public override int Count => Items?.Count ?? 0;

        public override void Add(object item)
        {
            // NOP.
        }

        public override void Remove(object item)
        {
            // NOP.
        }

        public override bool IsReadOnly => false;

        public override bool CanAdd => true;

        public override bool CanRemove => true;

        protected override IEnumerator GetEnumeratorInternal()
        {
            return Items != null
                ? ((IEnumerable)Items).GetEnumerator()
                : Enumerable.Empty<object>().GetEnumerator();
        }
    }
}
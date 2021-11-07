// Copyright (c) 2009 Kasimier Buchcik

using System.Collections;

namespace Casimodo.Lib.Presentation
{
    /// <summary>
    /// This adapter is intended to short-circuit a root CustomObservableCollection.
    /// </summary>
    public class ShortedCollectionSourceAdapter : CollectionSourceAdapterBase
    {
        public ShortedCollectionSourceAdapter()
        { }

        public CustomObservableCollection Items { get; set; }

        public override int Count => Items.Count;

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
            return ((IEnumerable)Items).GetEnumerator();
        }
    }
}
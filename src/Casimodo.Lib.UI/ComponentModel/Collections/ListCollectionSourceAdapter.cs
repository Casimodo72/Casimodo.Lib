using System;
using System.Collections.Generic;

namespace Casimodo.Lib.UI
{
    public class ListCollectionSourceAdapter<T> : CollectionSourceAdapterBase<T>
        where T : class
    {
        public ListCollectionSourceAdapter(IList<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        readonly IList<T> _source;

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

        public override bool CanAdd => !IsReadOnly;

        public override bool CanRemove => !IsReadOnly;

        protected override IEnumerator<T> GetEnumeratorInternal()
        {
            return _source.GetEnumerator();
        }
    }
}

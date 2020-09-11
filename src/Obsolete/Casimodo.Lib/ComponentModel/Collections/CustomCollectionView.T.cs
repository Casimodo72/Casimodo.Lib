// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Collections.Generic;
using System.Linq;
using MyItemType = System.Object;

namespace Casimodo.Lib.Presentation
{
    // TODO: Never tried this one in Silverlight yet.
    public class CustomCollectionView<T> : CustomCollectionView,
            IEnumerable<T>
            where T : class
#if (false)
        // This is only for Entities.
        ,IEditableObject
#endif
    {
        /// <summary>
        /// Creates a new CustomEntityCollectionView.
        /// </summary>
        /// <param name="internalCollection">is used as the *internal* collection of the view.
        /// This is *not* the source collection.</param>
        public CustomCollectionView(CustomObservableCollection internalCollection)
            : base(internalCollection)
        { }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _items.Cast<T>().GetEnumerator();
        }

        protected sealed override MyItemType CheckEntityTypeAndConvert(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            T item = obj as T;
            if (item == null)
                throw new ArgumentException(
                    string.Format("The given item must be of type '{0}'.", typeof(T).Name),
                    "item");

            return item;
        }
    }
}
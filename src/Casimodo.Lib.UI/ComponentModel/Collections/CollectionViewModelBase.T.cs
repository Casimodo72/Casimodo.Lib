// Copyright (c) 2009 Kasimier Buchcik

using Casimodo.Lib.ComponentModel;
using System.Collections.Specialized;

namespace Casimodo.Lib.UI
{
    public abstract class CollectionViewModelBase<T> : CollectionViewModel
        where T : class
    {
        public abstract void Add(T data);

        public abstract bool Remove(T data);
    }
}
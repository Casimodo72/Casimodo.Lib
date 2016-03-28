// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.Linq;
using System.Windows;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.ComponentModel;
using System.Reflection;
using Casimodo.Lib.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using MyView = System.Windows.Data.ListCollectionView;

namespace Casimodo.Lib.Presentation
{
    public abstract class CollectionViewModel : ObservableObject
    {
        public abstract int Count { get; }
        public abstract bool IsReadOnly { get; }
        public abstract void AddObject(object data);
        public abstract void RemoveObject(object data);
        public abstract bool CanAdd { get; }
        public abstract bool CanRemove { get; }

        /// <summary>
        /// Notifies when the items collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler ItemsCollectionChanged;

        /// <summary>
        /// Notifies when the items collection or the view's collection changes.
        /// This might fire twice for a single change, but ensures that you will be
        /// notified even if the view's internal collection is
        /// changed via the UI (e.g. via the filter, sort descriptions, etc.).
        /// </summary>
        public event NotifyCollectionChangedEventHandler ItemsOrViewCollectionChanged;

        protected virtual void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var handler = ItemsCollectionChanged;
            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnItemsOrViewCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var handler = ItemsOrViewCollectionChanged;
            if (handler != null)
                handler(this, e);
        }
    }   

   
}
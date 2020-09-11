// Copyright (c) 2009 Kasimier Buchcik

// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;

namespace Casimodo.Lib.Presentation
{    
    public abstract class MenuItemAdapter
    {
        public abstract void Initialize(object model);
        public abstract void Clear();
        public abstract object ViewObject { get; }
    }
    
    public abstract class MenuItemAdapter<TModel, TView> : MenuItemAdapter
        where TModel : class, INotifyPropertyChanged
        where TView : class, new()
    {
        public sealed override void Initialize(object model)
        {
            this.Model = (TModel)model;
            this.View = new TView();

            this.Model.PropertyChanged += OnModelPropertyChanged;

            InitializeCore();
        }

        public sealed override object ViewObject
        {
            get { return this.View; }
        }

        public sealed override void Clear()
        {
            // Detach.            
            Model.PropertyChanged -= OnModelPropertyChanged;

            ClearCore();
        }

        public TModel Model { get; private set; }
        public TView View { get; private set; }

        protected abstract void InitializeCore();
        protected abstract void ClearCore();
        protected abstract void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args);
    }
    
    public class MenuItemAdapterCollection<TAdapter> : IEnumerable, INotifyCollectionChanged
        where TAdapter : MenuItemAdapter, new()
    {
        List<MenuItemAdapter> _adapters = new List<MenuItemAdapter>();
        IEnumerable _model;

        public MenuItemAdapterCollection(IEnumerable model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (model as INotifyCollectionChanged == null)
                throw new ArgumentException("The given model must implement the INotifyCollectionChanged interface.");

            this._model = model;

            (this._model as INotifyCollectionChanged).CollectionChanged += OnSourceCollectionChanged;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Clear()
        {
            ClearItems();

            if (this._model == null)
                return;

            (this._model as INotifyCollectionChanged).CollectionChanged -= OnSourceCollectionChanged;

            this._model = null;
        }

        void ClearItems()
        {
            foreach (var adapter in this._adapters)
                adapter.Clear();

            this._adapters.Clear();
        }

        public IEnumerator GetEnumerator()
        {
            ClearItems();

            if (this._model == null)
                yield break;

            TAdapter adapter;
            foreach (var item in this._model)
            {
                adapter = new TAdapter();
                adapter.Initialize(item);

                yield return adapter.ViewObject;
            }
        }

        void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseCollectionChanged(e);
        }

        void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var handler = this.CollectionChanged;
            if (handler != null)
                handler(this, e);
        }
    }
}

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
using Casimodo.Lib.ComponentModel;

namespace Casimodo.Lib.Presentation
{    
    public class MenuItem : ObservableObject
    {
        public static readonly ObservablePropertyMetadata HeaderChangedArgs = ObservablePropertyMetadata.Create("Header");
        public static readonly ObservablePropertyMetadata IsCheckedChangedArgs = ObservablePropertyMetadata.Create("IsChecked");
        public static readonly ObservablePropertyMetadata IsEnabledChangedArgs = ObservablePropertyMetadata.Create("IsEnabled");
        public static readonly ObservablePropertyMetadata IsVisibleChangedArgs = ObservablePropertyMetadata.Create("IsVisible");

        MenuItemCollection _items = new MenuItemCollection();

        public MenuItem()
        {
            this._isEnabled = true;
            this._isVisible = true;
        }

        public string Id { get; set; }

        public string Header
        {
            get { return _header; }
            set { SetProperty(HeaderChangedArgs, ref _header, value); }
        }
        string _header;

        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetValueTypeProperty(IsCheckedChangedArgs, ref _isChecked, value); }
        }
        bool _isChecked;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetValueTypeProperty(IsEnabledChangedArgs, ref _isEnabled, value); }
        }
        bool _isEnabled;

        public bool IsVisible
        {
            get { return _isVisible; }
            set { SetValueTypeProperty(IsVisibleChangedArgs, ref _isVisible, value); }
        }
        bool _isVisible;

        public MenuItemCollection Items
        {
            get { return _items; }
        }

        public MenuItem Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            if (this.Id == id)
                return this;

            return this.Items.Find(id);
        }

        public void Visit(Action<MenuItem> visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException("visitor");

            visitor(this);

            this.Items.Visit(visitor);
        }

        public MenuItem AddItem(string header)
        {
            var item = new MenuItem();
            item.Header = header;
            ObtainItems().Add(item);

            return item;
        }

        public void AddItem(MenuItem item)
        {            
            ObtainItems().Add(item);         
        }

        MenuItemCollection ObtainItems()
        {
            if (this._items == null)
            {
                this._items = new MenuItemCollection();
                RaisePropertyChanged("Items");
            }
            return this._items;
        }

        public virtual void ExecuteAction()
        {
            // NOP.
        }
    }
}

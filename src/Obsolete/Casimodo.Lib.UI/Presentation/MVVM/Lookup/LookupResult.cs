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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Casimodo.Lib.Presentation
{
    public class SimpleViewModelArgs
    { }

    public class SingleViewModelArgs<TArg> : SimpleViewModelArgs
    {       
        public TArg Item
        {
            get { return _item; }
            set { _item = value; }
        }
        TArg _item;
    }

    public class ViewModelResult
    {
        public ViewModelResultState State { get; set; }

        public virtual void Clear()
        {
            // Stub - NOP.
        }
    }

    /// <summary>
    /// Used for dialog containers (e.g. Silverlight's ChildWindow)
    /// which actually do not return a result value.
    /// </summary>
    public class EmptyViewModelResult : ViewModelResult
    { }

    public class SingleViewModelResult : ViewModelResult
    {
        public override void Clear()
        {
            ItemObject = null;
        }

        public virtual object ItemObject { get; set; }
    }

    public class ViewModelResult<TItem> : SingleViewModelResult
        where TItem : class
    {
        public override void Clear()
        {
            Item = null;
        }

        public override object ItemObject
        {
            get { return _item; }
            set { this.Item = (TItem)value; }
        }

        public TItem Item
        {
            get { return _item; }
            set { _item = value; }
        }
        TItem _item;
    }

    public class ValueTypeViewModelResult<TStruct> : SingleViewModelResult
       where TStruct : struct
    {
        public override object ItemObject
        {
            get { return (object)_item; }
            set { this.Item = (TStruct)value; }
        }

        public TStruct Item
        {
            get { return _item; }
            set { _item = value; }
        }
        TStruct _item;

        public override void Clear()
        {
            // NOP.
        }
    }

    public class NullableViewModelResult<TStruct> : SingleViewModelResult
       where TStruct : struct
    {
        public override object ItemObject
        {
            get { return (object)_item; }
            set { this.Item = (TStruct)value; }
        }

        public TStruct? Item
        {
            get { return _item; }
            set { _item = value; }
        }
        TStruct? _item;

        public override void Clear()
        {
            _item = null;
        }
    }

    public class CollectionViewModelResult<TItem> : ViewModelResult
    {
        public CollectionViewModelResult()
        {
            this.Items = new Collection<TItem>();
        }

        public CollectionViewModelResult(IEnumerable<TItem> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            this.Items = new Collection<TItem>();
            foreach (var item in items)
                this.Items.Add(item);
        }

        public override void Clear()
        {
            Items.Clear();
        }

        public ICollection<TItem> Items { get; private set; }
    }
}
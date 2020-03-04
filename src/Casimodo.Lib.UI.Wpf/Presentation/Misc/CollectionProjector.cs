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
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using Casimodo.Lib.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    public class CollectionProjector<TSource, TProjection> : ObservableObject, INotifyCollectionChanged, IEnumerable<TProjection>, IEnumerable        
    {
        IEnumerable _source;

        public CollectionProjector(IEnumerable source)
        {
            if (source == null)
                throw new ArgumentNullException("source");            
            if (source as INotifyCollectionChanged == null)
                throw new ArgumentNullException("source", "The source does not implement INotifyCollectionChanged.");

            this._source = source;

            (this._source as INotifyCollectionChanged).CollectionChanged += (s, args) =>
                {
                    this.RaiseCollectionChanged(args);
                };
        }

        public Func<TSource, TProjection> Projector { get; set; }               

        public event NotifyCollectionChangedEventHandler CollectionChanged;        

        IEnumerator<TProjection> IEnumerable<TProjection>.GetEnumerator()
        {
            foreach (var item in Select())
                yield return item;            
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var item in Select())
                yield return item;
        }

        IEnumerable<TProjection> Select()
        {
            return
                from o in _source.Cast<TSource>()
                select this.Projector(o);
        }      

        void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            var handler = this.CollectionChanged;
            if (handler != null)
                this.CollectionChanged(this, args);
        }
    }
}

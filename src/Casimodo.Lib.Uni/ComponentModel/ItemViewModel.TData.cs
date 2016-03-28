using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Presentation
{
    public class ItemViewModel<TData> : ItemViewModel
    {
        public ItemViewModel()
        { }

        public ItemViewModel(TData data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            SetData(data);
        }

        public sealed override object DataObject
        {
            get { return _data; }
            set { SetData((TData)value); }
        }

        public TData Data
        {
            get { return _data; }
            set { SetData(value); }
        }
        public static readonly PropertyChangedEventArgs DataChangedArgs = new PropertyChangedEventArgs("Data");

        /// <summary>
        /// Obsolete: use Data instead.
        /// KABU TODO: REMOVE
        /// </summary>
        public TData Item
        {
            get { return _data; }
            set { SetData(value); }
        }
        protected TData _data;
        public static readonly PropertyChangedEventArgs ItemChangedArgs = new PropertyChangedEventArgs("Item");

        protected virtual void SetData(TData data)
        {
            if (object.Equals(data, _data))
                return;

            _data = data;

            RaisePropertyChanged(DataChangedArgs);
            RaisePropertyChanged(ItemChangedArgs);
        }
    }
}

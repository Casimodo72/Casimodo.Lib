using System;
using System.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    public class ItemViewModel<TData> : ItemViewModel
    {
        // KABU TODO: REMOVE
        //public static readonly PropertyChangedEventArgs DataChangedArgs = new PropertyChangedEventArgs("Data");
        //public static readonly PropertyChangedEventArgs ItemChangedArgs = new PropertyChangedEventArgs("Item");

        public ItemViewModel()
        { }

        public ItemViewModel(TData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

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

        /// <summary>
        /// Obsolete: use Data instead.
        /// KABU TODO: REMOVE
        /// </summary>
        //public TData Item
        //{
        //    get { return _data; }
        //    set { SetData(value); }
        //}

        protected TData _data;

        protected virtual void SetData(TData data)
        {
            SetProp(ref _data, data, nameof(Data));
            // KABU TODO: REMOVE
            //if (SetProp(ref _data, data, nameof(Data)))
            //{
            //    RaisePropertyChanged(nameof(Item));
            //}
        }
    }
}
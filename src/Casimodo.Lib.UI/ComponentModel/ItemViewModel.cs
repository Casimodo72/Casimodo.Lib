// Copyright (c) 2010 Kasimier Buchcik

namespace Casimodo.Lib.UI
{
    public abstract class ItemViewModel : CollectionItemViewModel
    {
        public ItemViewModel()
        { }

        public abstract object DataObject { get; set; }
    }
}
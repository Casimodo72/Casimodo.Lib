// Copyright (c) 2010 Kasimier Buchcik
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Casimodo.Lib.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    public abstract class ItemViewModel : CollectionItemViewModel
    {
        public ItemViewModel()
        { }

        public abstract object DataObject { get; set; }
    }
}
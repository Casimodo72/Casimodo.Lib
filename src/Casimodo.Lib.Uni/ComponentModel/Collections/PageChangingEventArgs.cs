﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO: There's no IPagedCollectionView and no PageChangingEventArgs in WPF.
#if (!SILVERLIGHT)
namespace System.ComponentModel
{
    // Summary:
    //     Provides data for the System.ComponentModel.IPagedCollectionView.PageChanging
    //     event.
    public sealed class PageChangingEventArgs : CancelEventArgs
    {
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.PageChangingEventArgs
        //     class.
        //
        // Parameters:
        //   newPageIndex:
        //     The index of the requested page.
        public PageChangingEventArgs(int newPageIndex)
        {
            NewPageIndex = newPageIndex;
        }

        // Summary:
        //     Gets the index of the requested page.
        //
        // Returns:
        //     The index of the requested page.
        public int NewPageIndex { get; private set; }
    }
}
#endif

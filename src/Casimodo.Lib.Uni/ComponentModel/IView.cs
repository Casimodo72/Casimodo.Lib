// Copyright (c) 2009 Kasimier Buchcik
using System;

namespace Casimodo.Lib.Presentation
{
    public interface IView
    { }

    public interface IView<TViewModel> : IView
        where TViewModel : class
    { }

    public interface ICloseable
    {
        void Close();
        event EventHandler Closed;
    }
}

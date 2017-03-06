// Copyright (c) 2009 Kasimier Buchcik
using System;

namespace Casimodo.Lib.Presentation
{
    public interface IView
    { }

    public interface IView<TViewModel> : IView
        where TViewModel : class
    { }

    // KABU TODO: Move to separate file.
    public interface ICloseable
    {
        void Close();

        event EventHandler Closed;
    }
}
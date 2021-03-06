﻿// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    public interface IViewModel : IDisposable
    {
        void Initialize(object arguments);

        void OnNavigatingAway(CancelableEventArgs args);

        void Refresh();

        void Close();

        event EventHandler Closed;

        object ViewObject { get; }

        void SetViewObject(object view);

        void SetArgumentObject(object argument);

        event PropertyChangedEventHandler PropertyChanged;

        IViewModel Parent { get; set; }
    }

    // KABU TODO: Move to separate file.
    public interface IViewModel<TView> : IViewModel
        where TView : class
    {
        TView View { get; }

        void SetView(TView view);
    }
}
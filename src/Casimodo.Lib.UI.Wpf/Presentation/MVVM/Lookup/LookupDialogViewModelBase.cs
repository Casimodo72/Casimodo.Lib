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
using System.Windows;
using System.ComponentModel;
using Casimodo.Lib.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    public interface IDialogView<TViewModel> : IDialogViewBase<TViewModel>, ICloseable
        where TViewModel : class
    {
        void Show();        
        void SetContentSize(double? width, double? height);        
        //bool? DialogResult { get; }
    }

    /// <summary>
    /// Abstract base class for dialog based views (e.g. for Silverlight's ChildWindow, WPF's modal Window).
    /// Closing can be performed either via the View or the ViewModel.
    /// </summary>    
    public abstract class DialogViewModel<TView, TViewModel, TParams, TResult> :
        LookupViewModelBase<TView, TViewModel, TParams, TResult>
        where TViewModel : class, IViewModel<TView>
        where TParams : class
        where TView : class, IDialogView<TViewModel>

        where TResult : ViewModelResult, new()
    {
        public DialogViewModel()
        { }

        protected override void OnViewAvailable()
        {
            base.OnViewAvailable();

            View.Closed += (s, e) =>
            {
                if (IsDisposed)
                    return;

                if (IsClosing)
                    return;                

                OnClosed();
            };
        }

        public virtual void Show()
        {
            HasResult = false;
            Result.Clear();

            if (View != null)
            {
                View.SetContentSize(_preferredWidth, _preferredHeight);                
                View.Show();
            }
        }               

        protected override void OnClosed()
        {
            try
            {
                if (this.DialogResult == true)
                {
                    BuildResult();

                    // NOTE that HasResult is solely dependent on the DialogResult of the View.
                    // This means that the actual result object can be empty even if HasResult is true.
                    HasResult = true;
                }
                else
                {
                    Cancel();
                }
            }
            finally
            {
                base.OnClosed();               
            }
        }

        public bool? DialogResult
        {
            get { return _dialogResult; }
            set { SetProperty(DialogResultProperty, ref _dialogResult, value); }
        }
        bool? _dialogResult;
        public static readonly ObservablePropertyMetadata DialogResultProperty = ObservablePropertyMetadata.Create("DialogResult");

        double? _preferredWidth;
        double? _preferredHeight;

        public void SetPreferredSize(double? width, double? height)
        {
            _preferredWidth = width;
            _preferredHeight = height;
        }
    }
}
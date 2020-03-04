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
using System.ComponentModel.Composition;
using System.ComponentModel;
using Casimodo.Lib.ComponentModel;

namespace Casimodo.Lib.Presentation
{
    public interface IDialogContainerViewModel<TView> : IDialogViewModel<TView, EmptyViewModelResult>
         where TView : class
    {
        bool? DialogResult { get; set; }
    }

    public interface IDialogContainerView<TViewModel> : IDialogView<TViewModel>
         where TViewModel : class
    {
        void SetContent(IDialogViewModel contentModel);
    }

    public interface IDialogViewModel<TView, TResult> : IViewModel<TView>, IDialogViewModel
        where TView : class
        where TResult : ViewModelResult, new()
    {
        TResult Result { get; }
    }

    public interface IDialogViewBase<TViewModel> : IView<TViewModel>
        where TViewModel : class
    { }

    public abstract class LookupViewModelBase<TView, TViewModel, TParams, TResult> : ViewModel3<TView, TViewModel, TParams>
        where TViewModel : class, IViewModel<TView>
        where TParams : class
        where TResult : ViewModelResult, new()
        where TView : class, IDialogViewBase<TViewModel>
    {
        public LookupViewModelBase()           
        {
            ArgumentPolicy = ViewModelArgumentPolicy.NotAccepted;
            _canConfirm = true;
        }       

        [Import]
        protected Lazy<TView> _lazyView { get; set; }

        protected override TView GetLazyView()
        {
            return _lazyView.Value;
        }

        public void BuildResult()
        {
            CheckNotDisposed();

            Result.Clear();
            HasResult = false;

            OnBuildResult();
        }

        public virtual void Cancel()
        {
            // NOP.
        }

        /// <summary>
        /// Set this to true/false in order to activate/deactive the OK button
        /// when this view is used in a dialog.
        /// </summary>
        public bool CanConfirm 
		{
			get { return _canConfirm; }
			set { SetProp(ref _canConfirm, value); }
		}
		bool _canConfirm;

        protected virtual void OnBuildResult()
        {
            // NOP.
        }

        public bool HasResult
        { get; protected set; }

        public TResult Result
        {
            get { return _result; }
        }
        TResult _result = new TResult();

        public object ResultObject
        {
            get { return _result; }
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            _lazyView = null;

            HasResult = false;
            if (_result != null)
                _result.Clear();
            _result = null;
            
        }
    }
}

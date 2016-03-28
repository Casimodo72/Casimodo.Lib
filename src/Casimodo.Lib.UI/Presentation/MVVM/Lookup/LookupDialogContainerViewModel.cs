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
using Casimodo.Lib.Presentation;
using System.ComponentModel.Composition;
using Casimodo.Lib.ComponentModel;
using System.ComponentModel;
using System.Windows;

namespace Casimodo.Lib.Presentation
{
    #region Interfaces for the WPF dialog window, ViewModel and View.

    public interface IWpfDialogShellViewModel : IDialogShellViewModel
    { }

    public interface IWpfDialogContainerViewModel : IDialogContainerViewModel<IWpfDialogContainerView>
    { }

    public interface IWpfDialogContainerView : IDialogContainerView<IWpfDialogContainerViewModel>
    { }

    #endregion

    /// <summary>
    /// Base class for dialog based lookup views (e.g. for Silverlight's ChildWindow, WPF's modal Window, or some custom stuff).
    /// This one takes a lookup view model as its content.
    /// </summary>    
    [ViewModelExport(typeof(IWpfDialogShellViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class WpfDialogContainerViewModel :
        DialogViewModel<IWpfDialogContainerView, IWpfDialogContainerViewModel, ViewModelArgs, EmptyViewModelResult>,
        IWpfDialogContainerViewModel, IWpfDialogShellViewModel
    {
        IDialogViewModel _contentModel;

        [ImportingConstructor]
        public WpfDialogContainerViewModel()
        {
            CanConfirm = false;
            IsOKVisible = true;
            IsCancelVisible = true;
        }

        protected override void OnBuildResult()
        {
            if (_contentModel == null)
                throw new Exception("Content model not assigned on the lookup child window.");

            _contentModel.BuildResult();
        }

        public void SetContent(IDialogViewModel contentModel)
        {
            if (contentModel == null)
                throw new ArgumentNullException("contentModel");

            _contentModel = contentModel;

            CanConfirm = _contentModel.CanConfirm;
            ((INotifyPropertyChanged)_contentModel).PropertyChanged += OnContentPropertyChanged;


            View.SetContent(contentModel);
        }

        void OnContentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // The content will indicate whether we can confirm (i.e. press the "OK" button) or not.
            if (e.PropertyName == "CanConfirm")
            {
                CanConfirm = _contentModel.CanConfirm;
            }
        }

        public void SetButtons(MessageBoxButton? buttons)
        {
            if (buttons == null)
            {
                IsOKVisible = false;
                IsCancelVisible = false;
                IsYesVisible = false;
                IsNoVisible = false;
            }
            else
            {
                IsOKVisible = buttons == MessageBoxButton.OK || buttons == MessageBoxButton.OKCancel;
                IsCancelVisible = buttons == MessageBoxButton.OKCancel || buttons == MessageBoxButton.YesNoCancel;
                IsYesVisible = buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.YesNoCancel;
                IsNoVisible = IsYesVisible;
            }
        }

        public override void Cancel()
        {
            // Notify the contained lookup of the cancellation.
            if (_contentModel != null)
                _contentModel.Cancel();
        }

        protected override void OnViewLoaded()
        {
            base.OnViewLoaded();

            // When the View of the Model is shown: make it initialize its data.
            _contentModel.Refresh();
        }

        public override void Show()
        {
            base.Show();

            // KBU TODO: REMOVE: We can't do this here, because the Window will block on Show()
            // and be already closed when we get to this code. 
            //// When the View of the Model is shown: make it initialize its data.
            //_contentModel.Refresh();
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool IsOKVisible
        {
            get { return _isOKVisible; }
            set { SetValueTypeProperty(IsOKVisibleProperty.ChangedArgs, ref _isOKVisible, value); }
        }
        bool _isOKVisible;
        public static readonly ObservablePropertyMetadata IsOKVisibleProperty = ObservablePropertyMetadata.Create("IsOKVisible");

        public bool IsCancelVisible
        {
            get { return _isCancelVisible; }
            set { SetValueTypeProperty(IsCancelVisibleProperty.ChangedArgs, ref _isCancelVisible, value); }
        }
        bool _isCancelVisible;
        public static readonly ObservablePropertyMetadata IsCancelVisibleProperty = ObservablePropertyMetadata.Create("IsCancelVisible");

        public bool IsYesVisible
        {
            get { return _isYesVisible; }
            set { SetValueTypeProperty(IsYesVisibleProperty.ChangedArgs, ref _isYesVisible, value); }
        }
        bool _isYesVisible;
        public static readonly ObservablePropertyMetadata IsYesVisibleProperty = ObservablePropertyMetadata.Create("IsYesVisible");

        public bool IsNoVisible
        {
            get { return _isNoVisible; }
            set { SetValueTypeProperty(IsNoVisibleProperty.ChangedArgs, ref _isNoVisible, value); }
        }
        bool _isNoVisible;
        public static readonly ObservablePropertyMetadata IsNoVisibleProperty = ObservablePropertyMetadata.Create("IsNoVisible");

        protected override void OnDispose()
        {
            base.OnDispose();

            if (_contentModel != null)
                _contentModel.Dispose();
            _contentModel = null;
        }
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    #region Interfaces for the custom dialog presenter, ViewModel and View.

    public interface ICustomDialogShellViewModel : IDialogShellViewModel
    {}

    public interface ICustomDialogContainerViewModel : IDialogContainerViewModel<ICustomDialogContainerView>
    { }

    public interface ICustomDialogContainerView : IDialogContainerView<ICustomDialogContainerViewModel>
    { }

    #endregion

    /// <summary>
    /// Base class for dialog based lookup views (e.g. for Silverlight's ChildWindow, WPF's modal Window, or some custom stuff).
    /// This one takes a lookup view model as its content.
    /// </summary>    
    [ViewModelExport(typeof(ICustomDialogShellViewModel), Strategy = ViewModelStrategy.ModelFirst)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CustomDialogContainerViewModel :
        DialogViewModel<ICustomDialogContainerView, ICustomDialogContainerViewModel, ViewModelArgs, EmptyViewModelResult>,
        ICustomDialogContainerViewModel, ICustomDialogShellViewModel
    {
        IDialogViewModel _contentModel;

        [ImportingConstructor]
        public CustomDialogContainerViewModel()
        {
            CanConfirm = false;
            IsOKVisible = true;
            IsCancelVisible = true;
        }

        protected override void OnBuildResult()
        {
            if (_contentModel == null)
                throw new Exception("Content model not assigned on the lookup child window.");

            _contentModel.BuildResult();
        }

        public void SetContent(IDialogViewModel contentModel)
        {
            if (contentModel == null)
                throw new ArgumentNullException("contentModel");

            _contentModel = contentModel;

            CanConfirm = _contentModel.CanConfirm;
            ((INotifyPropertyChanged)_contentModel).PropertyChanged += OnContentPropertyChanged;


            View.SetContent(contentModel);
        }

        void OnContentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // The content will indicate whether we can confirm (i.e. press the "OK" button) or not.
            if (e.PropertyName == "CanConfirm")
            {
                CanConfirm = _contentModel.CanConfirm;
            }
        }

        public void SetButtons(MessageBoxButton? buttons)
        {
            if (buttons == null)
            {
                IsOKVisible = false;
                IsCancelVisible = false;
                IsYesVisible = false;
                IsNoVisible = false;
            }
            else
            {
                IsOKVisible = buttons == MessageBoxButton.OK || buttons == MessageBoxButton.OKCancel;
                IsCancelVisible = buttons == MessageBoxButton.OKCancel || buttons == MessageBoxButton.YesNoCancel;
                IsYesVisible = buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.YesNoCancel;
                IsNoVisible = IsYesVisible;
            }
        }

        public override void Cancel()
        {
            // Notify the contained lookup of the cancellation.
            if (_contentModel != null)
                _contentModel.Cancel();
        }

        public override void Show()
        {
            base.Show();

            // When the View of the Model is shown: make it initialize its data.
            _contentModel.Refresh();
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool IsOKVisible
        {
            get { return _isOKVisible; }
            set { SetValueTypeProperty(IsOKVisibleProperty.ChangedArgs, ref _isOKVisible, value); }
        }
        bool _isOKVisible;
        public static readonly ObservablePropertyMetadata IsOKVisibleProperty = ObservablePropertyMetadata.Create("IsOKVisible");

        public bool IsCancelVisible
        {
            get { return _isCancelVisible; }
            set { SetValueTypeProperty(IsCancelVisibleProperty.ChangedArgs, ref _isCancelVisible, value); }
        }
        bool _isCancelVisible;
        public static readonly ObservablePropertyMetadata IsCancelVisibleProperty = ObservablePropertyMetadata.Create("IsCancelVisible");

        public bool IsYesVisible
        {
            get { return _isYesVisible; }
            set { SetValueTypeProperty(IsYesVisibleProperty.ChangedArgs, ref _isYesVisible, value); }
        }
        bool _isYesVisible;
        public static readonly ObservablePropertyMetadata IsYesVisibleProperty = ObservablePropertyMetadata.Create("IsYesVisible");

        public bool IsNoVisible
        {
            get { return _isNoVisible; }
            set { SetValueTypeProperty(IsNoVisibleProperty.ChangedArgs, ref _isNoVisible, value); }
        }
        bool _isNoVisible;
        public static readonly ObservablePropertyMetadata IsNoVisibleProperty = ObservablePropertyMetadata.Create("IsNoVisible");

        protected override void OnDispose()
        {
            base.OnDispose();

            if (_contentModel != null)
                _contentModel.Dispose();
            _contentModel = null;
        }
    }
}
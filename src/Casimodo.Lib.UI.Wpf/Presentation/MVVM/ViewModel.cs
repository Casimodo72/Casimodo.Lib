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

using Casimodo.Lib.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Casimodo.Lib.Presentation
{
    public enum ViewModelArgumentPolicy
    {
        Required,
        Optional,
        NotAccepted
    }

    public class ViewModelArgs
    {
        public static readonly ViewModelArgs Empty = new ViewModelArgs();
    }

    /// <summary>
    /// Almost the same as ViewModel2, but the argument's type is specified.
    /// </summary>    
    public abstract class ViewModel3<TView, TViewModel, TArg> : ViewModel2<TView, TViewModel>
        where TView : class, IView
        where TViewModel : IViewModel<TView>
        where TArg : class
    {
        public ViewModel3(TView view)
            : base(view)
        {
            // We want arguments.
            ArgumentPolicy = ViewModelArgumentPolicy.Required;
        }

        /// <summary>
        /// This constructor is only for design time purposes.
        /// </summary>
        protected ViewModel3()
        {
            ArgumentPolicy = ViewModelArgumentPolicy.Required;
        }

        protected TArg Arguments
        {
            get { return (TArg)ArgumentObject; }
        }

        public virtual void SetArgument(TArg argument)
        {
            SetArgumentObject(argument);
        }

        protected override void ValidateArgumentType(object argument)
        {
            TArg arg = argument as TArg;

            // Check for expected type of argument.
            if (arg == null)
                throw new ViewModelException(
                    string.Format(
                        "The given argument of type '{0}' is not the expected argument type '{1}' of ViewModel '{2}'.",
                        argument.GetType().Name, typeof(TArg).Name, this.GetType().Name));
        }


    }

    public abstract class ViewModel2<TView, TViewModel> : ViewModel<TView>
        where TView : class, IView
        where TViewModel : IViewModel<TView>
    {
        protected ViewModel2()
        { }

        protected ViewModel2(TView view)
            : base(view)
        { }

        protected override PropertyInfo FindModelOnView(TView view)
        {
            PropertyInfo prop = base.FindModelOnView(view);

            if (prop == null)
            {
                // Fallback: try to find a property with the model's interface type.
                Type modeInterfaceType = typeof(TViewModel);
                prop =
                    view.GetType().GetProperties()
                    .Where(x => x.PropertyType == modeInterfaceType)
                    .FirstOrDefault();
            }

            return prop;
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            _argumentObject = null;
        }
    }

    /// <summary>
    /// Abstract base class for view models.
    /// </summary>
    /// <typeparam name="TView"></typeparam>
    public abstract class ViewModel<TView> : ValidatingObservableObject, IViewModel<TView>
        // TODO: REMOVE: , ISupportInitialize
        where TView : class, IView
    {
        TView _view;
        ViewModelStrategy _strategy = ViewModelStrategy.ModelFirst;
        // TODO: PRISM: SubscriptionToken _shutdownSubscriptionToken;

        protected ViewModel()
        {
            ArgumentPolicy = ViewModelArgumentPolicy.NotAccepted;

            this._strategy = ValidateAndGetStrategy(this.GetType(), true);

            this._labels = new Dictionary<string, string>();

            this.RefreshCommand = CommandFactory.Create(() => Refresh(), () => CanRefresh);
        }

        protected virtual void OnShutdown()
        {
            // NOP.
        }

        void OnShutdownEvent(bool arg)
        {
            if (IsDisposed)
                return;

            OnShutdown();
        }

        // [Import]
        // TODO: PRISM: IEventAggregator EventAggregator { get; set; }

        protected ViewModel(TView view)
            : this()
        {
            if (view == null)
                throw new ArgumentNullException("view");

            SetView(view);
        }

        public IViewModel Parent { get; set; }

        public virtual void OnNavigatingAway(CancelableEventArgs args)
        {
            // NOP.
        }

        public event EventHandler Closed;

        protected bool IsClosing;

        public virtual void Close()
        {
            if (((IDisposableEx)this).IsDisposed)
                return;
            if (IsClosing)
                return;

            IsClosing = true;

            try
            {
                ICloseable closable = View as ICloseable;
                if (closable != null)
                    closable.Close();
            }
            finally
            {
                OnClosed();
            }
        }

        protected virtual void OnClosed()
        {
            try
            {
                var handler = Closed;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
            finally
            {
                Dispose();
            }
        }

        public virtual void SetArgumentObject(object argument)
        {
            if (argument == null)
            {
                if (ArgumentPolicy == ViewModelArgumentPolicy.Required)
                    throw new ViewModelException(
                        string.Format("Arguments are required by the ViewModel '{0}'.", this.GetType().Name));

                _argumentObject = null;

                return;
            }

            if (ArgumentPolicy == ViewModelArgumentPolicy.NotAccepted)
                throw new ViewModelException(
                    string.Format("Arguments are not accepted by the ViewModel '{0}'.", this.GetType().Name));

            ValidateArgumentType(argument);

            _argumentObject = argument;
        }

        protected virtual void ValidateArgumentType(object argument)
        {
            // NOP.
        }

        // Initialization ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected bool IsInitializing { get; private set; }

        public void Initialize(object arguments)
        {
            if (IsInitialized)
                throw new InvalidOperationException(string.Format("The ViewModel '{0}' was already initialized.", this.GetType().Name));
            if (IsInitializing)
                throw new ViewModelException(string.Format("The ViewModel (type {0}) is already initializing.", GetType().Name));

            IsInitializing = true;
            try
            {
                // TODO: PRISM: 
                //if (_shutdownSubscriptionToken == null)
                //    _shutdownSubscriptionToken = EventAggregator.GetEvent<ShutdownEvent>().Subscribe(OnShutdownEvent);

                _argumentObject = arguments;

                if ((_argumentObject == null) &&
                (ArgumentPolicy == ViewModelArgumentPolicy.Required))
                {
                    throw new InvalidOperationException(
                        string.Format("EndInit error: Arguments are required by the ViewModel '{0}'.", this.GetType().Name));
                }

                OnInitialize();
                OnEndInit();

                IsInitialized = true;

                // Refresh the data after initialization.
                Refresh();
            }
            finally
            {
                IsInitializing = false;
            }
        }

        protected virtual void OnEndInit()
        {
            // Stub - NOP.
        }

        protected virtual void OnInitialize()
        {
            // Stub - NOP.
        }

        // End of initialization ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public void SetView(TView view)
        {
            CheckNotDisposed();

            if (view == null)
                throw new ArgumentNullException("view");

            // We can not always set the View at design time due to a Designer bug:
            // https://connect.microsoft.com/VisualStudio/feedback/details/558954/the-designer-ignores-interfaces-defined-on-usercontrol-s
            if (DesignTimeHelper.IsInDesignTime)
                return;

            if (view == _view)
                return;

            this._view = view;
            InitializeView();
        }

        public void SetViewObject(object view)
        {
            CheckNotDisposed();

            if (view == null)
                throw new ArgumentNullException("view");

            // Note that we can not always set the View at design time due to a limitation of the Designer:
            // https://connect.microsoft.com/VisualStudio/feedback/details/558954/the-designer-ignores-interfaces-defined-on-usercontrol-s
            if (DesignTimeHelper.IsInDesignTime)
                return;

            if (view == _view)
                return;

            if (view as TView == null)
                throw new Exception(
                    string.Format("The given View does not implement interface '{0}'.", typeof(TView).Name));

            this._view = (TView)view;
            InitializeView();
        }

        protected virtual void InitializeView()
        {
            CheckNotDisposed();

            if (_view == null)
                return;

            if (_isViewInitialized)
                return;

            _isViewInitialized = true;

            ValidateStrategies();

            CreateMetaData();

            if (_strategy == ViewModelStrategy.ModelFirst)
            {
                PropertyInfo prop = FindModelOnView(_view);

                if (prop != null)
                {
                    // Set the Model property.
                    prop.SetValue(_view, this, null);
                }
                else
                {
                    // Note that it's OK if we cannot set the Model on the View,
                    // although it might indicate an error, so report a warning.
                    System.Diagnostics.Debug.WriteLine(
                        string.Format("View of type '{0}' does not expose the Model of type '{1}' via a public property.",
                            _view.GetType().Name, this.GetType().Name));
                }
            }


            if (!IsInitialized)
                Initialize(null);

            bool wasLoaded = true;
            FrameworkElement viewElem = (object)_view as FrameworkElement;
            if (viewElem != null)
            {
                wasLoaded = viewElem.IsLoaded;
                if (!wasLoaded)
                    viewElem.Loaded += HandleViewLoaded;

                viewElem.Unloaded += HandleViewUnloaded;
            }

            OnViewAvailable();

            if (wasLoaded)
                OnViewLoaded();
        }

        void HandleViewUnloaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement viewElem = (FrameworkElement)sender;
            viewElem.Loaded -= HandleViewUnloaded;

            if (!IsDisposed)
                OnViewUnloaded();
        }

        void HandleViewLoaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement viewElem = (FrameworkElement)sender;
            viewElem.Loaded -= HandleViewLoaded;

            OnViewLoaded();
        }

        protected virtual PropertyInfo FindModelOnView(TView view)
        {
            // Find the [Model] property in the view.
            PropertyInfo prop = _view.GetType().GetProperties().Where(x => x.IsDefined(typeof(ModelAttribute), true)).FirstOrDefault();

            return prop;
        }

        /// <summary>
        /// Called when the view becomes available the first time.
        /// </summary>
        protected virtual void OnViewAvailable()
        {
            CheckNotDisposed();
            // Stub - NOP.
        }

        protected virtual void OnViewLoaded()
        {
            CheckNotDisposed();
            // Stub - NOP.
        }

        protected virtual void OnViewUnloaded()
        {
            CheckNotDisposed();
            // Stub - NOP.
        }

        TAttr GetAttribute<TAttr>(Type type)
            where TAttr : Attribute
        {
            return
                type.GetCustomAttributes(typeof(TAttr), true)
                .Cast<Attribute>()
                .FirstOrDefault() as TAttr;
        }

        void ValidateStrategies()
        {
            if (_view == null)
                throw new InvalidOperationException("The View must not be null.");

            ViewModelStrategy modelStrategy = this._strategy;
            ViewModelStrategy viewStrategy = ValidateAndGetStrategy(this._view.GetType(), false);

            if (modelStrategy != viewStrategy)
            {
                string message =
                    string.Format("The Model uses the '{0}' strategy, but the View uses the '{1}' strategy.",
                        (modelStrategy == ViewModelStrategy.ViewFirst) ? "view-first" : "model-first",
                        (viewStrategy == ViewModelStrategy.ViewFirst) ? "view-first" : "model-first");

                throw new ViewModelException(
                    string.Format("MVVM strategy mismatch (Model '{0}', View '{1}'): {2}",
                        this.GetType().Name, _view.GetType().Name, message));
            }
        }

        ViewModelStrategy ValidateAndGetStrategy(Type type, bool isModel)
        {
            // KBU TODO: REMOVE
            //var modelFirst = GetAttribute<ModelFirstStrategyAttribute>(type);
            //var viewFirst = GetAttribute<ViewFirstStrategyAttribute>(type);
            //if (modelFirst != null && viewFirst != null)
            //    throw new ViewModelException(
            //        string.Format("The type '{0}' defines both, the [ModelFirstStrategy] " +
            //            "and [ViewFirstStrategy] attributes. Those attributes are mutually exclusive.",
            //            type.Name));

            MvvmExportAttribute exportAttr = null;
            ViewModelStrategy? exportStrategy = null;

            if (isModel)
            {
                if (GetAttribute<ViewExportAttribute>(type) != null)
                    throw new ViewModelException(
                        string.Format(
                            "ViewModel '{0}' is incorrectly annotated with the attribute [ViewExport]. " +
                            "The attribute [ViewExport] is not allowed on ViewModels.",
                            type.Name));

                exportAttr = GetAttribute<ViewModelExportAttribute>(type);
                if (exportAttr != null)
                    exportStrategy = exportAttr.Strategy;
            }
            else
            {
                if (GetAttribute<ViewModelExportAttribute>(type) != null)
                    throw new ViewModelException(
                        string.Format(
                            "View '{0}' is incorrectly annotated with the attribute [ViewModelExport]. " +
                            "The attribute [ViewModelExport] is not allowed on Views.",
                            type.Name));

                exportAttr = GetAttribute<ViewExportAttribute>(type);
                if (exportAttr != null)
                    exportStrategy = exportAttr.Strategy;
            }

            // KBU TODO: REMOVE
            //if (modelFirst != null && exportStrategy == ViewModelStrategy.ViewFirst)
            //    throw new ViewModelException(
            //        string.Format("The type '{0}' defines the [ModelFirstStrategy] " +
            //            "but also an export attribute with 'Strategy' set to 'ViewFirst'.",
            //            type.Name));

            //if (viewFirst != null && exportStrategy == ViewModelStrategy.ModelFirst)
            //    throw new ViewModelException(
            //        string.Format("The type '{0}' defines the [ViewFirstStrategy] " +
            //            "but also an export attribute with 'Strategy' set to 'ModelFirst'.",
            //            type.Name));

            // Note that ModelFirst is the default strategy.
            if (exportStrategy == null)
                return ViewModelStrategy.ModelFirst;
            else
                return exportStrategy.Value;
        }

        protected void CreateMetaData()
        {
            CheckNotDisposed();

            if (_wasMetaDataCreated)
                return;
            _wasMetaDataCreated = true;

#if (SILVERLIGHT)
            this.Meta = DataSourceCreator.ToDataObject(this._labels);
#endif
        }

        protected virtual void OnRefreshed()
        {
            CheckNotDisposed();
            // NOP.
        }

        public void Refresh()
        {
            CheckNotDisposed();

            if (!CanRefresh)
                return;

            OnRefreshed();
        }

        protected virtual TView GetLazyView()
        {
            CheckNotDisposed();
            return null;
        }

        TView GetView()
        {
            if (_view != null)
                return _view;

            if (IsInitializing)
                throw new ViewModelException(string.Format("The ViewModel ({0}) tries to access the View during initialization.", GetType().Name));

            // The view might be lazily created if using the "model first" approach.
            var view = GetLazyView();
            if (view == null)
            {
                string wrongStrategyErrorMsg = null;
                if (_strategy != ViewModelStrategy.ModelFirst)
                {
                    wrongStrategyErrorMsg =
                        "The Model does *not* use the 'Model first' strategy, " +
                        "this may indicate that the View was not initialized yet. ";
                }

                string furtherInfo = null;
                if (wrongStrategyErrorMsg != null)
                {
                    furtherInfo = " Further info: " + wrongStrategyErrorMsg;
                }

                throw new ViewModelException(
                    string.Format("Failed to acquire View '{0}' of Model '{1}'.{2}",
                    typeof(TView).Name, this.GetType().Name, furtherInfo));
            }

            SetView(view);

            return view;
        }

#if (SILVERLIGHT)
        protected void RegisterLabels(EntityPresenter item)
        {
            RegisterLabels(null, item);
        }

        protected void RegisterLabels(string prefix, EntityPresenter item)
        {
            //if (!string.IsNullOrEmpty(prefix))
            //    prefix += ".";

            string key, value;
            foreach (EntityPropertyDescriptor prop in item.GetEntityDescriptor().GetEntityPropertyDescriptors())
            {
                key = prefix + prop.PropertyName + "DisplayName";

                if (_labels.ContainsKey(key))
                    throw new ViewModelException(
                        string.Format("String resource with key '{0}' cannot be added because it " +
                        "was already added to the ViewModel.", key));

                value = prop.DisplayName;
                if (value == null)
                    value = string.Empty;

                _labels.Add(key, value);
            }
        }
#endif

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>
        /// Indicates whether the ViewModel wants arguments or not.
        /// The default is 'NotAccepted'.
        /// </summary>
        public ViewModelArgumentPolicy ArgumentPolicy { get; protected set; }

        protected object ArgumentObject
        {
            get { return _argumentObject; }
        }
        protected object _argumentObject;

        public string Title
        {
            get { return _title; }
            set
            {
                CheckNotDisposed();
                SetProperty(TitleChangedArgs, ref _title, value);
            }
        }
        string _title;
        public static readonly ObservablePropertyMetadata TitleChangedArgs = ObservablePropertyMetadata.Create("Title");

        public TView View
        {
            get
            {
                CheckNotDisposed();

                return GetView();
            }
        }

        public object ViewObject
        {
            get { return View; }
        }

        protected virtual void OnBusyChanged(bool value)
        {
            // NOP.
        }

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                if (SetProp(ref _IsBusy, value))
                {
                    IsNotBusy = !IsBusy;
                    OnBusyChanged(_IsBusy);
                }
            }
        }
        bool _IsBusy;

        public bool IsNotBusy
        {
            get { return _IsNotBusy; }
            set
            {
                if (SetProp(ref _IsNotBusy, value))
                {
                    IsBusy = !IsNotBusy;
                }
            }
        }
        bool _IsNotBusy = true;

        protected bool CanRefresh
        {
            get { return _canRefresh; }
            set
            {
                CheckNotDisposed();

                if (_canRefresh == value)
                    return;

                _canRefresh = value;

                RefreshCommand.RaiseCanExecuteChanged();
            }
        }
        bool _canRefresh = true;

        public object Meta
        {
            get { return _meta; }
            protected set
            {
                CheckNotDisposed();
                SetProperty<object>(MetaChangedArgs, ref _meta, value);
            }
        }
        object _meta;
        static readonly ObservablePropertyMetadata MetaChangedArgs = ObservablePropertyMetadata.Create("Meta");

        Dictionary<string, string> Labels
        {
            get { return _labels; }
        }
        Dictionary<string, string> _labels;

        protected bool IsInitialized { get; private set; }

        bool _isViewInitialized;
        bool _wasMetaDataCreated;

        // Commands ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public ICommandEx RefreshCommand
        {
            get { return _refreshCommand; }
            private set { SetProperty(RefreshCommandChangedArgs, ref _refreshCommand, value); }
        }
        ICommandEx _refreshCommand;
        public static readonly ObservablePropertyMetadata RefreshCommandChangedArgs = ObservablePropertyMetadata.Create("RefreshCommand");

        protected override void OnDispose()
        {
            base.OnDispose();

            // TODO: PRISM: EventAggregator.GetEvent<ShutdownEvent>().Unsubscribe(_shutdownSubscriptionToken);
            // TODO: PRISM: EventAggregator = null;
            _refreshCommand = null;
            Closed = null;
            _argumentObject = null;
            Parent = null;
            _view = null;
            _canRefresh = false;
            _labels = null;
            _meta = null;
        }
    }
}

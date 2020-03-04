using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Casimodo.Lib.ComponentModel;

using System;
using System.ComponentModel.Composition;

namespace Casimodo.Lib.Presentation.Navigation
{
    public interface INavigationService
    {
        void Add(AppNode node);
        ObservableCollection<AppNode> Items { get; }
        event AppNodeExecutedEventHandler ItemActionOccurred;
        bool Navigate(Uri source);
        bool Navigate(Uri source, object args);        
        object Content { get; }
        event NavigatedEventHandler Navigated;        
        INavigationContentProvider ContentProvider { get; set; }
    }

    public interface INavigationContentProvider
    {
        object GetContent(Uri source, object args);
    }  

    public class NavigationEventArgs : EventArgs
    {
        public NavigationEventArgs(object content, Uri uri)
        {
            this.Content = content;
            this.Uri = uri;
        }

        public object Content { get; private set; }
        public Uri Uri { get; private set; }        
    }

    public delegate void NavigatedEventHandler(object sender, NavigationEventArgs e);
    
    [Export(typeof(INavigationService))]    
    public class NavigationService : ObservableObject, INavigationService
    {
        [ImportingConstructor]
        public NavigationService()
        {
            this.Items = new ObservableCollection<AppNode>();
        }

        public INavigationContentProvider ContentProvider { get; set; }

        public object Content { get; private set; }

        public event NavigatedEventHandler Navigated;        

        public bool Navigate(Uri source)
        {
            return Navigate(source, null);
        }

        public bool Navigate(Uri source, object args)
        {
            // Get the part via the content provider.
            this.Content = this.ContentProvider.GetContent(source, args);

            // Note that we don't do anything yet here with the part.
            // We just deliver the new navigated-to part to subscribers.
            // E.g. a subscriber could be a shell view which does know how and where
            // to put the navigated-to part.

            var navigated = this.Navigated;
            if (navigated != null)
            {
                navigated(this, new NavigationEventArgs(this.Content, source));
            }            

            return true;
        }

        public event AppNodeExecutedEventHandler ItemActionOccurred;

        public void Add(AppNode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            node.Manager = this;
            this.Items.Add(node);
        }

        public ObservableCollection<AppNode> Items { get; private set; }

        internal void OnExecuted(AppNode item)
        {
            var handler = this.ItemActionOccurred;
            if (handler == null)
                return;

            handler(this, new AppNodeEventArgs(item));
        }

        public AppNode CurrentItem
        {
            get { return _currentItem; }
            set { SetProperty(CurrentItemProperty, ref _currentItem, value); }
        }
        AppNode _currentItem;
        public static readonly ObservablePropertyMetadata CurrentItemProperty = ObservablePropertyMetadata.Create("CurrentItem");
    }   

    public class AppNodeEventArgs : EventArgs
    {
        public AppNodeEventArgs(AppNode item)
        {
            this.Item = item;
        }

        public AppNode Item { get; private set; }
    }

    public delegate void AppNodeExecutedEventHandler(object sender, AppNodeEventArgs e);


    public class AppNode : ObservableObject
    {
        bool _canExecute = true;

        public AppNode()
        {
            Initialize();
        }

        public AppNode(NavigationService manager)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");

            this.Manager = manager;

            Initialize();
        }

        void Initialize()
        {
            this.ExecuteCommand = CommandFactory.Create(
               () => Execute(), () => _canExecute);

            this.Children = new ObservableCollection<AppNode>();
            this.Children.CollectionChanged +=
                (s, e) =>
                {
                    // Update HasChildren.
                    if (_hasChildren)
                    {
                        if (this.Children.Count == 0)
                            this.HasChildren = false;
                    }
                    else if (this.Children.Count != 0)
                        this.HasChildren = true;
                };
        }

        public NavigationService Manager { get; set; }

        void Execute()
        {
            this.Manager.OnExecuted(this);
        }

        public ICommandEx ExecuteCommand { get; private set; }

        public string DisplayName
        {
            get { return _displayName; }
            set { SetProp(ref _displayName, value); }
        }
        string _displayName;

        public bool HasChildren
        {
            get { return _hasChildren; }
            private set { SetProp(ref _hasChildren, value); }
        }
        bool _hasChildren;     

        public ObservableCollection<AppNode> Children { get; private set; }
    }

}
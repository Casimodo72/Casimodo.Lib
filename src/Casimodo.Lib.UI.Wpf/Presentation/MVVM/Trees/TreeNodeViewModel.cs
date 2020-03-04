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

using System.ComponentModel;
using System.Collections.ObjectModel;
using Casimodo.Lib.ComponentModel;
using System.Collections.Generic;
using System;
using System.Diagnostics;
namespace Casimodo.Lib.Presentation
{
    // Just a quick and initial sketch for a generic tree view model machinery.
    // Does not support async loading yet.

    static class ExampleScenario
    {
        public static void DoScenario()
        {
            var repository = new MyRepository();
            var treeManager = new TreeViewModelManager();

            treeManager.RegisterModel<Ardi, ArdiTreeItem>();
            treeManager.RegisterModel<Lucy, LucyTreeItem>();
            treeManager.RegisterModel<Egghead, EggheadTreeItem>();

            treeManager.RegisterChildrenLoader<Ardi>(
                (ardi) => (IEnumerable<object>)repository.GetDescendants(ardi));

            treeManager.RegisterChildrenLoader<Lucy>(
                (lucy) => (IEnumerable<object>)repository.GetDescendants(lucy));

            treeManager.CurrentChanged += OnTreeCurrentChanged;
        }

        static void OnTreeCurrentChanged(object sender, EventArgs e)
        {
            // NOP.
        }

        internal class Ardi
        { }

        internal class Lucy
        { }

        internal class Egghead
        { }

        internal class ArdiTreeItem : TreeNodeViewModel
        { }

        internal class LucyTreeItem : TreeNodeViewModel
        { }

        internal class EggheadTreeItem : TreeNodeViewModel
        { }

        internal class MyRepository
        {
            public IEnumerable<Lucy> GetDescendants(Ardi ardi)
            {
                return null;
            }

            public IEnumerable<Egghead> GetDescendants(Lucy lucy)
            {
                return null;
            }
        }
    }

    public class TreeViewModelManager : ObservableObject, IDisposable
    {
        Dictionary<Type, TreeChildrenLoader> _childrenLoaders = new Dictionary<Type, TreeChildrenLoader>();
        Dictionary<Type, TreeModelFactory> _modelFactories = new Dictionary<Type, TreeModelFactory>();

        public TreeViewModelManager()
        { }

        public void RegisterModel<TItem, TModel>()
            where TItem : class
            where TModel : TreeNodeViewModel, new()
        {
            _modelFactories.Add(typeof(TItem), new TreeModelFactory<TModel>());
        }

        public event EventHandler CurrentChanged;

        public void RegisterChildrenLoader<TParent>(Func<TParent, IEnumerable<object>> loadCallback)
            where TParent : class
        {
            if (loadCallback == null)
                throw new ArgumentNullException("loadCallback");

            var loader = new TreeChildrenLoader<TParent>(this, loadCallback);

            _childrenLoaders.Add(typeof(TParent), loader);
        }

        public TreeNodeViewModel CreateModel(object item, bool lazyLoadChildren = true)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            TreeModelFactory factory;

            if (!_modelFactories.TryGetValue(item.GetType(), out factory))
                throw new Exception(
                    string.Format("ViewModel factory not found for type '{0}'.", item.GetType().Name));

            TreeNodeViewModel model = factory.CreateModel();
            model.Initialize(this, null, item, lazyLoadChildren);

            return model;
        }

        public TreeNodeViewModel CurrentItem
        {
            get { return _currentItem; }
            set { SetProperty(ref _currentItem, value); }
        }
        TreeNodeViewModel _currentItem;

        internal void OnCurrentItemChanged(TreeNodeViewModel node, bool isSelected)
        {
            if (isSelected)
                CurrentItem = node;

            var handler = CurrentChanged;
            if (handler == null)
                return;

            handler(this, EventArgs.Empty);
        }

        internal TreeNodeViewModel CreateModel(Type itemType)
        {
            if (itemType == null)
                throw new ArgumentNullException("itemType");

            TreeModelFactory factory;

            if (!_modelFactories.TryGetValue(itemType, out factory))
                throw new Exception(
                    string.Format("ViewModel factory not found for type '{0}'.", itemType.Name));

            return factory.CreateModel();
        }

        /// <summary>        
        /// </summary>        
        /// <returns>the loader or null if no matching loader was registered.</returns>
        internal TreeChildrenLoader TryGetChildrenLoader(Type parentItemType)
        {
            if (parentItemType == null)
                throw new ArgumentNullException("parentItemType");

            TreeChildrenLoader loader;

            _childrenLoaders.TryGetValue(parentItemType, out loader);

            return loader;
        }


        protected override void OnDispose()
        {
            base.OnDispose();

            foreach (var loader in _childrenLoaders.Values)
                loader.Dispose();
            _childrenLoaders.Clear();
        }
    }

    internal abstract class TreeModelFactory
    {
        public abstract TreeNodeViewModel CreateModel();
    }

    internal sealed class TreeModelFactory<TModel> : TreeModelFactory
        where TModel : TreeNodeViewModel, new()
    {
        public override TreeNodeViewModel CreateModel()
        {
            return new TModel();
        }
    }

    internal abstract class TreeChildrenLoader : IDisposable
    {
        public abstract IEnumerable<TreeNodeViewModel> LoadChildren(object parentItem);
        public abstract void Dispose();
    }

    internal sealed class TreeChildrenLoader<TParent> : TreeChildrenLoader
        where TParent : class
    {
        TreeViewModelManager _manager;
        Func<TParent, IEnumerable<object>> _callback;

        public TreeChildrenLoader(TreeViewModelManager manager, Func<TParent, IEnumerable<object>> callback)
        {
            _manager = manager;
            _callback = callback;
        }

        public override IEnumerable<TreeNodeViewModel> LoadChildren(object parentItem)
        {
            foreach (var childItem in _callback((TParent)parentItem))
            {
                // Create the specific ViewModel type registerd with the child.
                TreeNodeViewModel childNode = _manager.CreateModel(childItem.GetType());

                // Initialize the ViewModel with the child.
                childNode.Initialize(_manager, null, childItem);

                yield return childNode;
            }
        }

        public override void Dispose()
        {
            _manager = null;
            _callback = null;
        }
    }

    /// <summary>    
    /// </summary>
    /// <remarks>
    /// The ITreeNodeViewModel interface is based on http://www.codeproject.com/KB/WPF/TreeViewWithViewModel.aspx
    /// </remarks>
    interface ITreeNodeViewModel : INotifyPropertyChanged
    {
        TreeViewModelItemCollection Children { get; }
        bool HasDummyChild { get; }
        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
        TreeNodeViewModel Parent { get; }
    }

    public class TreeViewModelItemCollection : ObservableCollection<TreeNodeViewModel>
    {
        public TreeViewModelItemCollection()
        { }
    }

    /// <summary>    
    /// </summary>
    /// <remarks>
    /// The TreeNodeViewModel code is partly based on http://www.codeproject.com/KB/WPF/TreeViewWithViewModel.aspx
    /// </remarks>
    public class TreeNodeViewModel : ObservableObject, ITreeNodeViewModel, IDisposable
    {
        static readonly TreeNodeViewModel DummyChild = new TreeNodeViewModel();

        internal TreeViewModelManager _manager;
        object _item;

        public TreeNodeViewModel()
        { }

        public TreeNodeViewModel(TreeViewModelManager manager, TreeNodeViewModel parentNode, object item)
            : this(manager, parentNode, item, true)
        { }

        public TreeNodeViewModel(TreeViewModelManager manager, TreeNodeViewModel parentNode, object item, bool lazyLoadChildren)
        {
            Initialize(manager, parentNode, item, lazyLoadChildren);
        }

        internal void Initialize(TreeViewModelManager manager, TreeNodeViewModel parentNode, object item)
        {
            Initialize(manager, parentNode, item, true);
        }

        internal void Initialize(TreeViewModelManager manager, TreeNodeViewModel parentNode, object item, bool lazyLoadChildren)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");
            if (item == null)
                throw new ArgumentNullException("item");

            _manager = manager;
            Parent = parentNode;
            _item = item;

            Children = new TreeViewModelItemCollection();

            if (lazyLoadChildren)
            {
                // Add a dummy node in order to load children at a later point in time.
                Children.Add(DummyChild);
            }
            else
            {
                // Try to load children now - if available.
                // LoadChildren();
            }
        }

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        public virtual void LoadChildren()
        {
            TreeChildrenLoader loader = _manager.TryGetChildrenLoader(_item.GetType());
            if (loader == null)
            {
                // If there is no loader, then this means that the ViewModel was defined with an empty content.
                // I.e. no children-loader was defined.
                return;
            }

            foreach (var child in loader.LoadChildren(_item))
            {
                child.Parent = this;
                Children.Add(child);
            }
        }

        public Object Item
        {
            get { return _item; }
        }

        public Object Data
        {
            get { return _item; }
        }

        public TreeNodeViewModel Parent { get; private set; }

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public TreeViewModelItemCollection Children { get; private set; }

        public void AddChild(TreeNodeViewModel childNode)
        {
            if (childNode.Parent == null)
                childNode.Parent = this;

            Children.Add(childNode);
        }

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild
        {
            get { return Children != null && Children.Count == 1 && Children[0] == DummyChild; }
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (!SetProp(ref _isExpanded, value))
                    return;

                // Expand all the way up to the root.
                if (IsExpanded && Parent != null)
                    Parent.IsExpanded = true;

                // Lazy load the child items, if necessary.
                if (HasDummyChild)
                {
                    Children.Remove(DummyChild);
                    LoadChildren();
                }

            }
        }
        bool _isExpanded;

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (SetProp(ref _isSelected, value))
                {
                    // Notify the manager of the selection change.
                    _manager.OnCurrentItemChanged(this, _isSelected);
                }
            }
        }
        bool _isSelected;
      
        protected override void OnDispose()
        {          
            var tempChildren = Children;
            _item = null;
            Parent = null;
            Children = null;
            if (tempChildren != null)
            {
                foreach (var child in tempChildren)
                    child.Dispose();
            }
        }
    }
}
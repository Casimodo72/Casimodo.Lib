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
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Resources;
using System.Reflection;

namespace Casimodo.Lib.Presentation
{
    public class ModelSource : Grid
    {
        public ModelSource()
        {
            //HorizontalContentAlignment = HorizontalAlignment.Stretch;
            //VerticalContentAlignment = VerticalAlignment.Stretch;

            Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignTimeHelper.IsInDesignTime)
                return;

            if (this.IsModelInherited && this.Model == null)
            {
                DependencyObject parent = null, cur = this;

                while ((parent = VisualTreeHelper.GetParent(cur)) != null)
                {
                    cur = parent;
                    if (cur is ModelSource)
                    {
                        // Inherit the model in context.
                        this.SetInheritedModel(((ModelSource)cur).Model as IViewModel);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Not intended to be used at design time.
        /// </summary>
        public void SetModel(IViewModel model, object view)
        {
            if (DesignTimeHelper.IsInDesignTime)
                return;

            SetValue(ModelProperty, model);
            model.SetViewObject(view);
        }

        /// <summary>
        /// This one is used when propagating the ViewModel down into a subsequent View.
        /// </summary>        
        internal void SetInheritedModel(IViewModel model)
        {
            // At design time, the model won't be connected to the view.
            if (DesignTimeHelper.IsInDesignTime)
                return;

            SetValue(ModelProperty, model);
        }

        #region Model DP ------------------------------------------------------

        public object Model
        {
            get { return GetValue(ModelProperty); }
            // set { SetValue(ModelProperty, value); }
        }

        void OnModelPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
#if (DEBUG)
            if (!DesignTimeHelper.IsInDesignTime)
            {
                if (e.OldValue != null && e.NewValue != null)
                    throw new Exception(
                        "Suspicious scenario: The property 'Model' on the 'ModeSource' is being reassigned. " +
                        "We don't support this.");
            }
#endif

            // TODO: Doesn't work at design time, why?
            if (this.IsModelDataContext && this.DataContext != e.NewValue)
                this.DataContext = e.NewValue;
        }

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                "Model",
                typeof(object),
                typeof(ModelSource),
                new PropertyMetadata(
                    (d, e) => ((ModelSource)d).OnModelPropertyChanged(e)));

        #endregion

        #region IsModelDataContext DP -----------------------------------------

        // TODO: Doesn't work duringt design time. Why?

        /// <summary>
        /// Identifies the IsModelDataContext dependency property.
        /// <summary>
        public static readonly DependencyProperty IsModelDataContextProperty =
            DependencyProperty.Register("IsModelDataContext", typeof(bool), typeof(ModelSource),
                new PropertyMetadata(true,
                    (d, e) => ((ModelSource)d).OnIsModelDataContextPropertyChanged(e)));

        public bool IsModelDataContext
        {
            get { return (bool)GetValue(IsModelDataContextProperty); }
            set { SetValue(IsModelDataContextProperty, value); }
        }

        void OnIsModelDataContextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                ClearValue(FrameworkElement.DataContextProperty);
                return;
            }

            // TODO: Can't make this work during design time, why?
            var model = this.Model;
            if (model != null && this.DataContext != model)
                this.DataContext = model;
        }

        #endregion

        #region DesignModelTypeName DP ----------------------------------------

        // NOTE: This takes an assembly qualified type name. E.g. "My.Stuff.MyViewModelDesignTime, My.StuffAssembly".
        // TODO: Unfortunately neither x:Type exists in Silverlight, nor markup extensions,
        //  so for now we must use a string in order to express the type of the design time ViewModel.
        public string DesignModelTypeName
        {
            get { return (string)GetValue(DesignModelTypeNameProperty); }
            set { SetValue(DesignModelTypeNameProperty, value); }
        }

        void OnDesignModelTypeNamePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            // Intended only for disign time.
            if (!DesignTimeHelper.IsInDesignTime)
                return;

            var typeName = e.NewValue as string;
            if (string.IsNullOrEmpty(typeName))
                return;

            Type type = null;
            try
            {
                // Note that GetType still throws FileLoadException even if we specified that it should not raise errors!
                type = Type.GetType(typeName, false);
            }
            catch
            {
                // Suppress.
            }

#if (SILVERLIGHT)
            if (type == null)
            {
                string[] steps = typeName.Split(',').Select(s => s.Trim()).ToArray();

                if (steps.Length > 1)
                {
                    string assemblyName = steps[0];
                    typeName = steps[1];
                    type = GetAssemblyType(assemblyName + ".dll", typeName);
                }
                else
                {
                    type = GetAssemblyType(typeName);
                }
            }
#endif

            if (type == null)
                throw new Exception(
                    string.Format("Failed to create design time model. Type '{0}' could not be found.", typeName));

            var instance = Activator.CreateInstance(type);
            if (instance == null)
                throw new Exception(
                    string.Format("Failed to create design time model. Failed to create an instance of type '{0}'.", typeName));

            SetValue(ModelProperty, instance);

            // TODO: Doesn't work at design time, why?
            //if (IsModelDataContext)
            //    DataContext = instance;
        }
#if (SILVERLIGHT)

        // See http://inquisitorjax.blogspot.com/2009/10/gettype-from-referenced-assembly-in.html
        public static Type GetAssemblyType(string assemblyName, string className)
        {
            StreamResourceInfo info = Application.GetResourceStream(new Uri(assemblyName+ ".dll", UriKind.Relative));
            if (info == null)
                return null;

            Assembly assembly = new AssemblyPart().Load(info.Stream);

            Type type = assembly.GetType(className);
            return type;
        }

        // See http://inquisitorjax.blogspot.com/2009/10/gettype-from-referenced-assembly-in.html
        public static Type GetAssemblyType(string className)
        {
            Type type = null;
            foreach (AssemblyPart part in Deployment.Current.Parts)
            {
                type = GetAssemblyType(part.Source, className);
                if (type != null)
                    break;
            }

            return type;
        }
#endif

        /// <summary>
        /// Identifies the DesignModelTypeName dependency property.
        /// <summary>
        public static readonly DependencyProperty DesignModelTypeNameProperty =
            DependencyProperty.Register(
                "DesignModelTypeName",
                typeof(string),
                typeof(ModelSource),
                new PropertyMetadata(
                    (d, e) => ((ModelSource)d).OnDesignModelTypeNamePropertyChanged(e)));

        #endregion       

        /// <summary>
        /// The "DesignModelType" property (DP).
        /// <summary>        
        public object DesignModelType
        {
            get { return (object)GetValue(DesignModelTypeProperty); }
            set { SetValue(DesignModelTypeProperty, value); }
        }

        /// <summary>
        /// The "DesignModelTypeProperty" dependency property.
        /// <summary>
        public static readonly DependencyProperty DesignModelTypeProperty =
            DependencyProperty.Register("DesignModelType", typeof(object), typeof(ModelSource),
                new PropertyMetadata(null, (d, e) => ((ModelSource)d).OnDesignModelTypePropertyChanged(e)));

        void OnDesignModelTypePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            // Intended only for disign time.
            if (!DesignTimeHelper.IsInDesignTime)
                return;

            Type type = e.NewValue as Type;
            if (type == null)
                return;

            var instance = Activator.CreateInstance(type);

            SetValue(ModelProperty, instance);
        }

        #region IsModelInherited DP -------------------------------------------

        /// <summary>
        /// Identifies the IsModelInherited dependency property.
        /// <summary>
        public static readonly DependencyProperty IsModelInheritedProperty =
            DependencyProperty.Register("IsModelInherited", typeof(bool), typeof(ModelSource),
                new PropertyMetadata(false));

        public bool IsModelInherited
        {
            get { return (bool)GetValue(IsModelInheritedProperty); }
            set { SetValue(IsModelInheritedProperty, value); }
        }

        #endregion
    }

    public class View : DependencyObject
    {
        #region Model DP ------------------------------

        /// <summary>
        /// Identifies the Model attached dependency property.
        /// <summary>
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.RegisterAttached("Model", typeof(object),
            typeof(View), new PropertyMetadata(null, OnModelPropertyChanged));
       
        public static object GetModel(DependencyObject obj)
        {
            return (object)obj.GetValue(ModelProperty);
        }

        public static void SetModel(DependencyObject obj, object value)
        {
            obj.SetValue(ModelProperty, value);
        }

        static void OnModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement elem = d as FrameworkElement;
            if (elem == null)
                return;

            // NOP at design time.
            if (DesignTimeHelper.IsInDesignTime)
                return;

            IViewModel model = null;
            if (e.NewValue != null)
            {
                model = e.NewValue as IViewModel;
                if (model == null)
                    throw new Exception(
                        "Failed to propagate a model to a view: The given value does not implement 'IViewModel'");
            }

#if (SILVERLIGHT)
            ModelSource child;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(elem); i++)
            {
                child = VisualTreeHelper.GetChild(elem, i) as ModelSource;
                if (child != null)
                {
                    child.SetInheritedModel(model);
                    // EXIT.
                    return;
                }
            }
#else
            foreach (var child in LogicalTreeHelper.GetChildren(elem))
            {
                if (child is ModelSource)
                {
                    ((ModelSource)child).SetInheritedModel(model);
                    // EXIT.
                    return;
                }
            }

#endif
            throw new Exception(
                "Failed to propagate a model to a view: No 'ModelSource' found in the target view.");      
        }

        public static IViewModel FindModel(FrameworkElement element)
        {
#if (SILVERLIGHT)
            ModelSource child;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                child = VisualTreeHelper.GetChild(element, i) as ModelSource;
                if (child != null)
                {
                    return (IViewModel)child.Model;
                }
            }
#else
            foreach (var child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is ModelSource)
                {
                    return (IViewModel)(child as ModelSource).Model;                    
                }
            }

#endif 
            return null;
        }
       
        #endregion Model DP ------------------------------
    }
}

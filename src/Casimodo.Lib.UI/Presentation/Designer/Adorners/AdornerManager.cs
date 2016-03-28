using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace Casimodo.Lib.Designer
{
    public class AdornerManager : IDesignerManager
    {
        //AdornerPane _pane;
        DesignerCanvas _canvas;        
        List<ISelectable> _selectedItems = new List<ISelectable>();
        Point? _rubberbandStart = null;
        
        //public AdornerManager(AdornerPane pane)
        //{
        //    _pane = pane;
        //    _canvas = pane.Canvas;
        //}

        public Point? RubberbandStart
        {
            get { return _rubberbandStart; }
        }

        public void Initialize(DesignerCanvas canvas)
        {
            _canvas = canvas;
        }

        public List<ISelectable> SelectedItems
        {
            get { return _selectedItems; }
            set { _selectedItems = value; }
        }

        public void AdornElement(FrameworkElement target)
        {
            MindPic.Designer.Controls.Resizer adorner =
                new MindPic.Designer.Controls.Resizer(this, target);
            AddAdorner(adorner);
        }

        public void AdornSingleElement(FrameworkElement target)
        {
            RemoveAdorners();
            AdornElement(target);

            //adorner.Focus();
            //adorner.CaptureMouseForDrag();
        }

        public void AddAdorner(FrameworkElement adorner)
        {
            if (!(adorner is IDesignAdorner))
                throw new DesignerException("The given element must implement the IDesignAdorner interface.");

            _canvas.Children.Add(adorner);
            (adorner as IDesignAdorner).Initialize();
        }

        public void RemoveAdorner(IDesignAdorner adorner)
        {
            if (adorner == null)
                throw new ArgumentNullException();

            FrameworkElement item = adorner as FrameworkElement;
            if (item == null)
                throw new DesignerException("The given object must be a FrameworkElement.");

            RemoveAdorner(item);
        }

        public void RemoveAdorner(FrameworkElement item)
        {
            if (item == null)
                throw new ArgumentNullException();

            _canvas.Children.Remove(item);
        }

        void RemoveAdorners()
        {
            _canvas.Children.Clear();
        }

        public IInputElement GetCoordinateSourceOfAdorner(FrameworkElement item)
        {
            return _canvas;
        }
    }    
}
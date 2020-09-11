using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;

namespace Casimodo.Lib.Designer
{
    public class DesignerCanvas : Canvas
    {
        AdornerManager _manager;
        Point? _rubberbandStart = null;

        public DesignerCanvas(AdornerManager manager)
        {
            this._manager = manager;
            this.ClipToBounds = true;
            this.AllowDrop = true;
            this.SnapsToDevicePixels = true;

            // We need the transparent backtround in order to receive mouse events.
            this.Background = Brushes.Transparent;
        }

        public AdornerManager Designer
        {
            get { return _manager; }
        }

        static int counter;

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            Console.WriteLine(">> Canvas Pre Down, Source: " + e.Source.ToString() + " " + (++counter).ToString());

            FrameworkElement source = e.Source as FrameworkElement;
            if (source == null)
            {
                base.OnPreviewMouseDown(e);
                return;
            }

            if (source != this)
            {
                FrameworkElement adorner = TryGetAdorner(source);
                if (adorner != null)
                {
                    if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                    {
                        e.Handled = true;
                        // Remove the adorner.
                        _manager.RemoveAdorner(adorner);
                    }
                }
                else if (adorner == null)
                {
                    e.Handled = true;
                    // TODO: How to know the source is the top-level Visual of a
                    //   compound control ?                    
                    if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                    {
                        // If Shift or Ctrl is pressed, then add an adorner without
                        // deselecting any existing adorners.
                        _manager.AdornElement(source);
                    }
                    else
                    {
                        // Remove all adorners & adorn the selected element.
                        _manager.AdornSingleElement(source);
                    }
                }
            }

            base.OnPreviewMouseDown(e);
        }

        FrameworkElement TryGetAdorner(FrameworkElement item)
        {
            while (item != null)
            {
                if (item is IDesignAdorner)
                    return item as FrameworkElement;

                item = VisualTreeHelper.GetParent(item) as FrameworkElement;
            }
            return null;
        }

        #region Rubberband

        //protected sealed override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        //{
        //    return new PointHitTestResult(this, hitTestParameters.HitPoint);
        //}

        bool _isSelecting;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // NOTE: It's important to skip if the event was already
            // handled by an other element.
            if (e.Handled)
                return;

            Console.WriteLine(">> Canvas Normal Mouse Down " + (++counter).ToString());

            base.OnMouseLeftButtonDown(e);

            if (e.Source == this)
            {
                _rubberbandStart = new Point?(e.GetPosition(this));
                e.Handled = true;
            }            
        }

        FrameworkElement _curMovedAdorner;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Handled)
                return;
            Console.WriteLine(">> Canvas Normal Mouse Move " + (++counter).ToString());

            base.OnMouseMove(e);

            if ((e.LeftButton == MouseButtonState.Pressed) &&
                (_rubberbandStart != null))
            {
                Console.WriteLine(">> NEW Rubber " + (++counter).ToString());
                // Create rubberband adorner.
                RubberbandSelector rub = new RubberbandSelector(_manager, _rubberbandStart);
                _manager.AddAdorner(rub);
                rub.CaptureMouse();
            }

            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            base.OnMouseLeftButtonUp(e);
        }

        //protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        //{
        //    base.OnMouseLeftButtonUp(e);

        //    if (_rubberbandStart != null)
        //    {
        //        HandleLeftButtonUp(e);
        //        _rubberband = null;

        //        e.Handled = true;
        //    }

        //    e.Handled = true;
        //}

        //void HandleLeftButtonUp(MouseButtonEventArgs e)
        //{
        //    _manager.RemoveAdorner(_rubberband);
        //}

        #endregion

    }

}

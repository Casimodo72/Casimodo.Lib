using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System;

namespace Casimodo.Lib.Designer
{    
    sealed class RubberbandSelector : FrameworkElement, IDesignAdorner
    {
        Point? _startPoint;
        Point? _endPoint;
        static readonly Pen _pen;

        private IDesignerManager _manager;

        static RubberbandSelector()
        {
            _pen = new Pen(Brushes.LightSlateGray, 1);
            _pen.DashStyle = new DashStyle(new double[] { 2 }, 1);
        }

        public RubberbandSelector(IDesignerManager manager, Point? dragStart)
        {
            this._manager = manager;
            this._startPoint = dragStart;   
        }

        public void Initialize()
        {
            // NOP.
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!IsMouseCaptured)
                    CaptureMouse();

                _endPoint = e.GetPosition(this);
                UpdateSelection();
                InvalidateVisual();
            }
            else
            {
                if (IsMouseCaptured)
                    ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {            
            if (IsMouseCaptured)
                ReleaseMouseCapture();

            // Remove the rubberband.
            _manager.RemoveAdorner(this);

            e.Handled = true;

            Console.WriteLine(">>> DELETE Rubber");
        }

        protected override void OnRender(DrawingContext dc)
        {
            // We need a background for the OnMouseMove to be fired.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

            if (_startPoint.HasValue && _endPoint.HasValue)
                dc.DrawRectangle(Brushes.Transparent, _pen,
                    new Rect(this._startPoint.Value, this._endPoint.Value));
        }

        private void UpdateSelection()
        {
#if (false)
            foreach (ISelectable item in designerCanvas.SelectedItems)
                item.IsSelected = false;
            designerCanvas.SelectedItems.Clear();

            Rect rubberBand = new Rect(startPoint.Value, endPoint.Value);
            foreach (FrameworkElement item in designerCanvas.Children)
            {
                Rect itemRect = VisualTreeHelper.GetDescendantBounds(item);
                Rect itemBounds = item.TransformToAncestor(designerCanvas).TransformBounds(itemRect);

                if (rubberBand.Contains(itemBounds) && item is ISelectable)
                {
                    ISelectable selectableItem = item as ISelectable;
                    selectableItem.IsSelected = true;
                    designerCanvas.SelectedItems.Add(selectableItem);
                }
            }
#endif
        }
    }
}

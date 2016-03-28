using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Documents;
using System.ComponentModel;
using System.Linq;

namespace Casimodo.Lib.Designer
{
    public sealed class AdornerPane : Adorner
    {
        FrameworkElement _child;
        
        public AdornerPane(FrameworkElement adornedElement, Canvas canvas)
            : base(adornedElement)
        {
            this.IsHitTestVisible = true;
            this.ClipToBounds = false;

            _child = canvas;            

            AddLogicalChild(_child);
            AddVisualChild(_child);

            // TODO: REVISIT: We *need* to leave the pane with Canvas.Left & Canvas.Top at NaN.
            //   Otherwise, the visual child's position will be altered. Why?
        }

        protected sealed override Visual GetVisualChild(int index)
        {
            return _child;
        }

        protected sealed override int VisualChildrenCount
        {
            get { return 1; }
        }        

        protected sealed override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        protected sealed override Size ArrangeOverride(Size finalSize)
        {                        
            _child.Arrange(new Rect(new Point(0, 0), finalSize));
            return finalSize;
        }
    }
}
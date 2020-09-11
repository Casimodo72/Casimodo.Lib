using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

using System.Windows.Media.Animation; // Storyboard ect.
using System.Windows.Media.Effects; // bitmap effects
// using MindPic.Controls;

namespace Casimodo.Lib.Designer
{
    /// <summary>
    /// Manages and displays various extra items in the designer like reference lines
    /// to attached labels, data-states of input control, etc.
    /// </summary>
    public class DecorationManager
    {
        DesignerCanvas _canvas;
        List<Link> _links = new List<Link>();

        public DecorationManager(DesignerCanvas canvas)
        {
            if (canvas == null)
                throw new ArgumentNullException("canvas");

            _canvas = canvas;
        }

        public void AddLink(MindBaseControl item1, MindBaseControl item2)
        {
            Link link = new Link(_canvas, item1, item2);
            _links.Add(link);

            //item1.Unloaded += delegate(object sender, RoutedEventArgs a)
            //{
            //    if (!link.Removed)
            //    {
            //        link.Remove(_canvas);
            //        _links.Remove(link);
            //        link.Removed = true;
            //    }
            //};

            //item2.Unloaded += delegate(object sender, RoutedEventArgs a)
            //{
            //    if (!link.Removed)
            //    {
            //        link.Remove(_canvas);
            //        _links.Remove(link);
            //        link.Removed = true;
            //    }
            //};
        }


        /// <summary>
        /// Links two mind-controls with a line.
        /// </summary>
        public class Link
        {
            MindBaseControl _item1;
            MindBaseControl _item2;
            Line _line;
            Binding _bindX1;
            Binding _bindY1;
            Binding _bindX2;
            Binding _bindY2;
            public bool Removed;

            Binding Bind(MindBaseControl source, DependencyProperty dp)
            {
                Binding bind = new Binding();
                bind.Source = source;
                bind.Path = new PropertyPath(dp);

                return bind;
            }

            public Link(Canvas canvas, MindBaseControl item1, MindBaseControl item2)
            {
                if ((item1 == null) || (item2 == null))
                    throw new ArgumentNullException();                
                _item1 = item1;
                _item2 = item2;

                // Create bindings.
                _bindX1 = Bind(item1, MindBaseControl.LinkPointXProperty);
                _bindY1 = Bind(item1, MindBaseControl.LinkPointYProperty);

                _bindX2 = Bind(item2, MindBaseControl.LinkPointXProperty);
                _bindY2 = Bind(item2, MindBaseControl.LinkPointYProperty);

                // Create the line.
                _line = new Line();
                _line.Stroke = Brushes.DarkBlue;
                _line.StrokeThickness = 1f;
                _line.StrokeDashArray = new DoubleCollection(new double[] { .5, .5 });
                
                canvas.Children.Insert(0, _line);
                //Canvas.SetZIndex(_line, 0);

                //int zidx = Canvas.GetZIndex(_line);

                // Set bindings.
                _line.SetBinding(Line.X1Property, _bindX1);
                _line.SetBinding(Line.Y1Property, _bindY1);
                _line.SetBinding(Line.X2Property, _bindX2);
                _line.SetBinding(Line.Y2Property, _bindY2);                
            }

            public void Remove(Canvas canvas)
            {
                canvas.Children.Remove(_line);
                _line = null;
            }
        }
    }
}

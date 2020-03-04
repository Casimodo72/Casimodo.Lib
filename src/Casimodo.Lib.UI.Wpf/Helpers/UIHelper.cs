using System.Windows;
using System.Windows.Media;
using System;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Input;
#if (!SILVERLIGHT)
using System.Runtime.InteropServices;
using System.Windows.Controls;
#endif

namespace Casimodo.Lib.Presentation
{
    //public class ObsoleteMefPartCreationPolicyNonSharedAttribute : Attribute
    //{
    //    // PartCreationPolicy(CreationPolicy.NonShared)
    //}

    public static class UIHelper
    {
        public static string ToCssRgba(string htmlColor, decimal alpha)
        {
            var color = System.Drawing.ColorTranslator.FromHtml(htmlColor);
            return string.Format("rgba({0}, {1}, {2}, {3})", color.R, color.G, color.B, alpha);
        }

        public static void Try01(int r, int g, int b)
        {
            //Color.FromArgb
            string val = String.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
            val = "rgba(168, 96, 96, 1)";
            var color = System.Drawing.ColorTranslator.FromHtml(val);
            
            //return System.Drawing.ColorTranslator.FromHtml(val).Name.Remove(0, 2);
        }
        // Visual tree ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public static TDescendant FindVisualDecendant<TDescendant>(this DependencyObject obj)
           where TDescendant : DependencyObject
        {
            return FindVisualDecendantCore(obj, typeof(TDescendant), false, null) as TDescendant;
        }

        public static TDescendant FindVisualDecendantElement<TDescendant>(this DependencyObject obj, string name)
           where TDescendant : FrameworkElement
        {
            return FindVisualDecendantCore(obj, typeof(TDescendant), false, name) as TDescendant;
        }

        public static TDescendant FindVisualDecendantElementExact<TDescendant>(this DependencyObject obj, string name)
           where TDescendant : FrameworkElement
        {
            return FindVisualDecendantCore(obj, typeof(TDescendant), true, name) as TDescendant;
        }

        static DependencyObject FindVisualDecendantCore(DependencyObject obj, Type type, bool exact, string name)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject c = VisualTreeHelper.GetChild(obj, i);
                if (c == null)
                    continue;

                if (exact)
                {
                    if (c.GetType().Equals(type))
                    {
                        // Eval name.
                        if (name != null)
                        {
                            if ((c is FrameworkElement) && ((FrameworkElement)c).Name == name)
                                return c;
                        }
                        else
                            return c;
                    }
                }
                else
                {
                    if (type.IsAssignableFrom(c.GetType()))
                    {
                        // Eval name.
                        if (name != null)
                        {
                            if ((c is FrameworkElement) && ((FrameworkElement)c).Name == name)
                                return c;
                        }
                        else
                            return c;
                    }
                }

                c = FindVisualDecendantCore(c, type, exact, name);
                if (c != null)
                    return c;
            }

            return null;
        }

#if (!SILVERLIGHT)
        // Position related stuff ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public static Point GetPointRelativeToElement(UIElement element, Point point)
        {
            return element.TranslatePoint(point, element);
        }

        public static Point GetPointRelativeToElement(UIElement contextElement, UIElement relativeElement)
        {
            Point context = GetAbsolutePositionOfElement(contextElement);
            Point target = GetAbsolutePositionOfElement(relativeElement);

            return new Point(target.X - context.X, target.Y - context.Y);
        }

        public static Point GetAbsolutePositionOfElement(UIElement element)
        {
            return element.PointToScreen(new Point(0, 0));
        }

        public static Point GetAbsoluteMidPointOfElement(FrameworkElement element)
        {
            Point midPoint = element.TranslatePoint(new Point(element.Width / 2, element.Height / 2), element);
            Point absolute = element.PointToScreen(midPoint);
            return absolute;
        }

        public static Point GetAbsoluteMousePosition(FrameworkElement element)
        {            
            return element.PointToScreen(Mouse.GetPosition(element));
        }

        public static Point GetMousePositionRelativeToElement(UIElement element)
        {
            return Mouse.GetPosition(element);
        }

        // Mouse position ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        // Source: http://www.switchonthecode.com/tutorials/wpf-snippet-reliably-getting-the-mouse-position
        // Note that this will raise an exception if the visual is not connected
        //   to a presentation source, so check that beforehand:
        //   See http://stackoverflow.com/questions/2154211/in-wpf-under-what-circumstances-does-visual-pointfromscreen-throw-invalidoperati
        public static Point GetCorrectMousePosition(Visual relativeTo)
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return relativeTo.PointFromScreen(new Point(w32Mouse.X, w32Mouse.Y));
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        

        // Image processing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        // Source: http://social.msdn.microsoft.com/forums/en-US/wpf/thread/3dd34758-5998-4195-a3db-5280828471bf
        public static BitmapSource CaptureScreen(Visual target, double dpiX, double dpiY)
        {
            if (target == null)
                return null;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);

            RenderTargetBitmap rtb =
                new RenderTargetBitmap(
                    (int)(bounds.Width * dpiX / 96.0),
                    (int)(bounds.Height * dpiY / 96.0),
                    dpiX,
                    dpiY,
                    PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(target);
                ctx.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
            }

            rtb.Render(dv);

            return rtb;
        }

        // Source: http://stackoverflow.com/questions/2977385/wpf-screenshot-jpg-from-uielement-with-c
        public static byte[] GetJpgImage(UIElement source, double scale, int quality)
        {
            double actualHeight = source.RenderSize.Height;
            double actualWidth = source.RenderSize.Width;

            double renderHeight = actualHeight * scale;
            double renderWidth = actualWidth * scale;

            RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)renderWidth, (int)renderHeight, 96, 96, PixelFormats.Pbgra32);
            VisualBrush sourceBrush = new VisualBrush(source);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            using (drawingContext)
            {
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
            }
            renderTarget.Render(drawingVisual);

            JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder();
            jpgEncoder.QualityLevel = quality;
            jpgEncoder.Frames.Add(BitmapFrame.Create(renderTarget));

            Byte[] _imageArray;

            using (MemoryStream outputStream = new MemoryStream())
            {
                jpgEncoder.Save(outputStream);
                _imageArray = outputStream.ToArray();
            }

            return _imageArray;
        }

        // Interop ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll")]
        static extern int FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);

        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_CLOSE = 0xF060;

        public static void CloseOnscreenKeyboard()
        {
            // retrieve the handler of the window  
            int iHandle = FindWindow("IPTIP_Main_Window", "");
            if (iHandle > 0)
            {
                // close the window using API        
                SendMessage(iHandle, WM_SYSCOMMAND, SC_CLOSE, 0);
            }
        }
#endif
    }
}
using System.Windows;
using System.Windows.Media;

namespace Casimodo.Lib.Presentation
{
    public static class DependencyObjectExtensions
    {
        public static T ParentOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            if (element == null)
                return null;

            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (parent == null)
                return null;

            if (typeof(T).IsAssignableFrom(parent.GetType()))
                return parent as T;

            return ParentOfType<T>(parent);
        }

        public static T GetChildObject<T>(this DependencyObject obj, string name) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject c = VisualTreeHelper.GetChild(obj, i);
                if (c.GetType().Equals(typeof(T)) && ((FrameworkElement)c).Name == name)
                {
                    return (T)c;
                }
                DependencyObject gc = GetChildObject<T>(c, name);
                if (gc != null)
                    return (T)gc;
            }
            return null;
        }
    }
}
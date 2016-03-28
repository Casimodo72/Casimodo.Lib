using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Casimodo.Lib.Presentation.Controls
{
    public class ListBoxButton : Button
    {
        public ListBoxButton()
        {}

        protected override void OnClick()
        {
            var item = this.FindVisualAncestor<ListBoxItem>();
            if (item != null)
            {
                item.IsSelected = true;
            }

            base.OnClick();
        }
    }

    public static class PresentationExtensions
    {
        public static TAncestor FindVisualAncestor<TAncestor>(this DependencyObject source, Predicate<TAncestor> predicate)
           where TAncestor : DependencyObject
        {
            DependencyObject cur = source;
            while ((cur = VisualTreeHelper.GetParent(cur)) != null)
            {
                if (typeof(TAncestor).IsAssignableFrom(cur.GetType()) && predicate((TAncestor)cur))
                    return (TAncestor)cur;
            }

            return null;
        }

        public static TAncestor FindVisualAncestor<TAncestor>(this DependencyObject source)
           where TAncestor : DependencyObject
        {
            DependencyObject cur = source;
            while ((cur = VisualTreeHelper.GetParent(cur)) != null)
            {
                if (typeof(TAncestor).IsAssignableFrom(cur.GetType()))
                    return (TAncestor)cur;
            }

            return null;
        }
    }
}
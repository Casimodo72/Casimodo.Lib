
using Casimodo.Lib.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Casimodo.Lib.Presentation
{
    public static class MoCommand
    {
        public static object GetParameter(DependencyObject obj)
        {
            return obj.GetValue(ParameterProperty);
        }

        public static void SetParameter(DependencyObject obj, object value)
        {
            obj.SetValue(ParameterProperty, value);
        }

        public static readonly DependencyProperty ParameterProperty = DependencyProperty.RegisterAttached("Parameter", typeof(object), typeof(MoCommand), new UIPropertyMetadata(null, OnParameterChanged));

        static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as ButtonBase;
            if (button == null)
                return;

            button.CommandParameter = e.NewValue;
            var cmd = button.Command as ICommandEx;
            if (cmd != null)
            {
                cmd.RaiseCanExecuteChanged();
            }
        }
    }
}
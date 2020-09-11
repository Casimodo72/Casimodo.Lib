using System.Windows;

namespace Casimodo.Lib.Presentation
{
    // Source: http://vspivak.wordpress.com/2011/04/12/problem-with-microsoft-expression-interactivity-core-datatrigger/
    public class XDataTrigger : Microsoft.Expression.Interactivity.Core.DataTrigger
    {
        public bool RespectLoadedEvent
        {
            get { return (bool)GetValue(RespectLoadedEventProperty); }
            set { SetValue(RespectLoadedEventProperty, value); }
        }

        public static readonly DependencyProperty RespectLoadedEventProperty =
            DependencyProperty.Register("RespectLoadedEvent", typeof(bool), typeof(XDataTrigger), new PropertyMetadata(true, OnRespectLoadedEventChanged));

        private static void OnRespectLoadedEventChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        protected override void OnAttached()
        {
            if (AssociatedObject is FrameworkElement && RespectLoadedEvent)
                ((FrameworkElement)AssociatedObject).Loaded += XDataTriggerLoaded;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject is FrameworkElement)
                ((FrameworkElement)AssociatedObject).Loaded -= XDataTriggerLoaded;

        }

        void XDataTriggerLoaded(object sender, RoutedEventArgs e)
        {
            EvaluateBindingChange(null);
        }
    }
}

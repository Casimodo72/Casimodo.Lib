using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Casimodo.Lib.Presentation
{
    public static class GlobalKeyboardConfig
    {
        public static bool IsOnscreenKeyboardSuppressed = false;
    }

    public class NoOnscreenKeyboardBehaviorCore : IDisposable
    {
        TextBox _textBox;

        public NoOnscreenKeyboardBehaviorCore(TextBox element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (!GlobalKeyboardConfig.IsOnscreenKeyboardSuppressed)
                return;

            _textBox = element;

            Attach();

            //_textBox.Loaded += (s, e) =>
            //{
            //    Attach();
            //};

            //_textBox.Unloaded += (s, e) =>
            //{
            //    Dispose();
            //};
        }

        void Attach()
        {
            if (_textBox == null)
                return;

            _textBox.PreviewGotKeyboardFocus += OnPreviewGotKeyboardFocus;
            _textBox.PreviewTouchUp += OnPreviewTouchUp;
        }

        public void Detach()
        {
            if (_textBox == null)
                return;

            _textBox.PreviewGotKeyboardFocus -= OnPreviewGotKeyboardFocus;
            _textBox.PreviewTouchUp -= OnPreviewTouchUp;
        }

        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            Detach();

            _textBox = null;
        }

        private void OnPreviewTouchUp(object sender, TouchEventArgs e)
        {
            UIHelper.CloseOnscreenKeyboard();
        }

        private void OnPreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            UIHelper.CloseOnscreenKeyboard();
        }
    }
}

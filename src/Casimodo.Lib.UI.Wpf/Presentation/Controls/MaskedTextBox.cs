// Copyright (c) 2010 Kasimier Buchcik

// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Reflection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Casimodo.Lib.ComponentModel;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Windows.Media;
using Casimodo.Lib.Presentation.Input;

namespace Casimodo.Lib.Presentation.Controls
{
    public class MaskedTextBox : TextBox
    {
        public MaskedTextBox()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, null, CancelCommand));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, CancelCommand));

            // Note that the MaskedTextProvides always expects the english decimal separator
            // in the mask string, regardless of what the current culture is.
            InputMask = "0.00";

            Loaded += (s, e) => OnLoaded();

            AddHandler(PreviewMouseLeftButtonDownEvent,
             new MouseButtonEventHandler(SelectivelyIgnoreMouseButton), true);
            AddHandler(GotKeyboardFocusEvent,
              new RoutedEventHandler(SelectAllText), true);
            AddHandler(MouseDoubleClickEvent,
              new RoutedEventHandler(SelectAllText), true);
        }

        static void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            // Find the TextBox
            DependencyObject parent = e.OriginalSource as UIElement;
            while (parent != null && !(parent is TextBox))
                parent = VisualTreeHelper.GetParent(parent);

            if (parent != null)
            {
                var textBox = (TextBox)parent;
                if (!textBox.IsKeyboardFocusWithin)
                {
                    // If the text box is not yet focussed, give it the focus and
                    // stop further processing of this click event.
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        static void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null)
                textBox.SelectAll();
        }

        // DP UnmaskedText ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public string UnmaskedText
        {
            get { return (string)GetValue(UnmaskedTextProperty); }
            set { SetValue(UnmaskedTextProperty, value); }
        }
        public static readonly DependencyProperty UnmaskedTextProperty =
            DependencyProperty.Register("UnmaskedText", typeof(string), typeof(MaskedTextBox),
                new UIPropertyMetadata(string.Empty, (d, e) => ((MaskedTextBox)d).OnUnmaskedTextChanged(e)));

        void OnUnmaskedTextChanged(DependencyPropertyChangedEventArgs e)
        {
            Provider.Set((string)e.NewValue);
            Text = Provider.ToDisplayString();
        }

        // DP InputMask ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~      

        /// <summary>
        /// The "InputMask" property (DP).
        /// <summary>        
        public string InputMask
        {
            get { return (string)GetValue(InputMaskProperty); }
            set { SetValue(InputMaskProperty, value); }
        }

        /// <summary>
        /// The "InputMaskProperty" dependency property.
        /// <summary>
        public static readonly DependencyProperty InputMaskProperty =
            DependencyProperty.Register("InputMask", typeof(string), typeof(MaskedTextBox),
                new PropertyMetadata("0000", (d, e) => ((MaskedTextBox)d).OnInputMaskPropertyChanged(e)));

        void OnInputMaskPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Reset();
        }

        // DP PromptChar ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>
        /// The "PromptChar" property (DP).
        /// <summary>        
        public char PromptChar
        {
            get { return (char)GetValue(PromptCharProperty); }
            set { SetValue(PromptCharProperty, value); }
        }

        /// <summary>
        /// The "PromptCharProperty" dependency property.
        /// <summary>
        public static readonly DependencyProperty PromptCharProperty =
            DependencyProperty.Register("PromptChar", typeof(char), typeof(MaskedTextBox),
                new PropertyMetadata('_', (d, e) => ((MaskedTextBox)d).OnPromptCharPropertyChanged(e)));

        void OnPromptCharPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Reset();
        }

        /// <summary>
        /// Gets the MaskTextProvider for the specified Mask
        /// </summary>
        public MaskedTextProvider Provider
        {
            get
            {
                if (_maskProvider == null)
                {
                    // Initialize the masked text provider.
                    _maskProvider = CreateMaskedTextProvider(InputMask);

                    _maskProvider.PromptChar = PromptChar;

                    if (String.IsNullOrWhiteSpace(UnmaskedText))
                        _maskProvider.Set(String.Empty);
                    else
                        _maskProvider.Set(UnmaskedText);
                }
                return _maskProvider;
            }
        }
        MaskedTextProvider _maskProvider;

        static MaskedTextProvider CreateMaskedTextProvider(string mask)
        {
            return new MaskedTextProvider(mask, Thread.CurrentThread.CurrentUICulture);
        }

        void OnLoaded()
        {
            Reset();

#if (false)
            var textProp = DependencyPropertyDescriptor.FromProperty(MaskedTextBox.TextProperty, typeof(MaskedTextBox));
            if (textProp != null)
            {
                textProp.AddValueChanged(this, (s, args) => UpdateText());
            }
            DataObject.AddPastingHandler(this, Pasting);
#endif
        }

        void Reset()
        {
            _maskProvider = null;
            SelectionStart = 0;
            SelectionLength = 0;
            Text = Provider.ToDisplayString();

            Focus();
            SelectAll();
        }

        /// <summary>
        /// override this method to replace the characters enetered with the mask
        /// </summary>
        /// <param name="e">Arguments for event</param>
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            if (e.Handled)
                return;

            //if the text is readonly do not add the text
            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            TreatSelectedText();

            var position = GetNextCharacterPosition(SelectionStart, true);

            // Note that we'll handle all input as if in overtype mode.
            if (Provider.Replace(e.Text, position))
                position++;

#if (false)
            if (Keyboard.IsKeyToggled(Key.Insert))
            {
                if (Provider.Replace(e.Text, position))
                    position++;
            }
            else
            {
                if (Provider.InsertAt(e.Text, position))
                    position++;
            }
#endif

            position = GetNextCharacterPosition(position, true);

            RefreshText(position);

            e.Handled = true;
        }

        /// <summary>
        /// override the key down to handle delete of a character
        /// </summary>
        /// <param name="e">Arguments for the event</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Handled)
                return;

            int position = SelectionStart;

            if (e.Key == Key.Delete && position < Text.Length)
            {
                if (Provider.RemoveAt(position))
                    RefreshText(position);

                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                if (Provider.InsertAt(" ", position))
                    RefreshText(position);

                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                if (position > 0)
                {
                    MaskedTextResultHint hint;
                    int testPosition;

                    while (position > 0)
                    {
                        position--;

                        if (Provider.RemoveAt(position, position, out testPosition, out hint))
                        {
                            if (hint == MaskedTextResultHint.Success)
                            {
                                RefreshText(position);
                                break;
                            }
                            else if (hint == MaskedTextResultHint.NoEffect)
                            {
                                // If the position is not editable, then we'll just move the caret.
                                SelectionStart = position;
                            }
                            else
                                break;
                        }
                        else
                            break;
                    }
                }

                e.Handled = true;
            }
        }

#if (false)
        private void UpdateText()
        {
            if (Provider.ToDisplayString().Equals(Text))
                return;

            var success = Provider.Set(Text);

            SetText(success ? Provider.ToDisplayString() : Text, Provider.ToString(false, false));
        }
#endif

        private bool TreatSelectedText()
        {
            if (SelectionLength > 0)
            {
                return Provider.RemoveAt(SelectionStart, SelectionStart + SelectionLength - 1);
            }
            return false;
        }

        private void RefreshText(int position)
        {
            // First evaluate if there was any input (using @includeLiterals == false).
            // E.g. the mask is "0.00"; we need to know whether the zeroes carry any input,
            //   so we must ignore the decimal separator.
            string txt = Provider.ToString(false, false);

            if (string.IsNullOrWhiteSpace(txt))
            {
                // There was no input.
                txt = null;
            }
            else
            {
                // There was some input.
                // Get the effective input (@includeLiterals == true).
                // E.g. the mask is "0.00" and Text is "_.1_" the effective input will be ".1"
                txt = Provider.ToString(false, true);
            }

            UnmaskedText = txt;
            SelectionStart = position;
        }

#if (false)
        private void SetText(string text, string unmaskedText)
        {
            UnmaskedText = String.IsNullOrWhiteSpace(unmaskedText) ? null : unmaskedText;
            Text = String.IsNullOrWhiteSpace(text) ? null : text;
        }
#endif

        private int GetNextCharacterPosition(int startPosition, bool goForward)
        {
            var position = Provider.FindEditPositionFrom(startPosition, goForward);

            if (position == -1)
                return startPosition;
            else
                return position;
        }

        private static void CancelCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

    }

#if (false)

    public class MaskedTextBox5 : TextBox
    {
    #region DependencyProperties

        public string UnmaskedText
        {
            get { return (string)GetValue(UnmaskedTextProperty); }
            set
            {
                SetValue(UnmaskedTextProperty, value);
            }
        }

        public static readonly DependencyProperty UnmaskedTextProperty =
        DependencyProperty.Register("UnmaskedText", typeof(string),
        typeof(MaskedTextBox5), new UIPropertyMetadata(""));

        public static readonly DependencyProperty InputMaskProperty =
        DependencyProperty.Register("InputMask", typeof(string), typeof(MaskedTextBox5), null);

        public string InputMask
        {
            get { return (string)GetValue(InputMaskProperty); }
            set { SetValue(InputMaskProperty, value); }
        }

        public static readonly DependencyProperty PromptCharProperty =
        DependencyProperty.Register("PromptChar", typeof(char), typeof(MaskedTextBox5),
        new PropertyMetadata('_'));

        public char PromptChar
        {
            get { return (char)GetValue(PromptCharProperty); }
            set { SetValue(PromptCharProperty, value); }
        }

    #endregion

        private MaskedTextProvider Provider;

        public MaskedTextBox5()
        {
            Loaded += new RoutedEventHandler(MaskedTextBox_Loaded);
            PreviewTextInput += new TextCompositionEventHandler(MaskedTextBox_PreviewTextInput);
            PreviewKeyDown += new KeyEventHandler(MaskedTextBox_PreviewKeyDown);
        }

        void MaskedTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int position = SelectionStart;

            if (e.Key == Key.Space)
            {
                TreatSelectedText();

                position = GetNextCharacterPosition(SelectionStart, true);

                if (Provider.InsertAt(" ", position))
                    RefreshText(position);

                e.Handled = true;
            }

            if (e.Key == Key.Back)
            {
                TreatSelectedText();

                e.Handled = true;

                if (SelectionStart == 0)
                {
                    if (Provider.RemoveAt(position))
                    {
                        position = GetNextCharacterPosition(position, false);
                    }
                }

                RefreshText(position);

                e.Handled = true;
            }

            if (e.Key == Key.Delete)
            {
                if (TreatSelectedText())
                {
                    RefreshText(SelectionStart);
                }
                else
                {

                    if (Provider.RemoveAt(SelectionStart))
                        RefreshText(SelectionStart);

                }

                e.Handled = true;
            }
        }

        void MaskedTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TreatSelectedText();

            var position = GetNextCharacterPosition(SelectionStart, true);

            if (Keyboard.IsKeyToggled(Key.Insert))
            {
                if (Provider.Replace(e.Text, position))
                    position++;
            }
            else
            {
                if (Provider.InsertAt(e.Text, position))
                    position++;
            }

            position = GetNextCharacterPosition(position, true);

            RefreshText(position);

            e.Handled = true;
        }

        void MaskedTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            Provider = new MaskedTextProvider(InputMask, CultureInfo.CurrentCulture);

            if (String.IsNullOrWhiteSpace(UnmaskedText))
                Provider.Set(String.Empty);
            else
                Provider.Set(UnmaskedText);

            Provider.PromptChar = PromptChar;
            Text = Provider.ToDisplayString();

            var textProp = DependencyPropertyDescriptor.FromProperty(MaskedTextBox.TextProperty, typeof(MaskedTextBox));
            if (textProp != null)
            {
                textProp.AddValueChanged(this, (s, args) => UpdateText());
            }
            DataObject.AddPastingHandler(this, Pasting);
        }

        private void Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var pastedText = (string)e.DataObject.GetData(typeof(string));

                TreatSelectedText();

                var position = GetNextCharacterPosition(SelectionStart, true);

                if (Provider.InsertAt(pastedText, position))
                {
                    RefreshText(position);
                }
            }

            e.CancelCommand();
        }

        private void UpdateText()
        {
            if (Provider.ToDisplayString().Equals(Text))
                return;

            var success = Provider.Set(Text);

            SetText(success ? Provider.ToDisplayString() : Text, Provider.ToString(false, false));
        }

        private bool TreatSelectedText()
        {
            if (SelectionLength > 0)
            {
                return Provider.RemoveAt(SelectionStart, SelectionStart + SelectionLength - 1);
            }
            return false;
        }

        private void RefreshText(int position)
        {
            SetText(Provider.ToDisplayString(), Provider.ToString(false, false));
            SelectionStart = position;
        }

        private void SetText(string text, string unmaskedText)
        {
            UnmaskedText = String.IsNullOrWhiteSpace(unmaskedText) ? null : unmaskedText;
            Text = String.IsNullOrWhiteSpace(text) ? null : text;
        }

        private int GetNextCharacterPosition(int startPosition, bool goForward)
        {
            var position = Provider.FindEditPositionFrom(startPosition, goForward);

            if (position == -1)
                return startPosition;
            else
                return position;
        }
    }



    /// <summary>
    /// Source: http://avaloncontrolslib.codeplex.com
    /// </summary>
    public class MaskedTextBox6 : TextBox
    {
        public string UnmaskedText
        {
            get { return (string)GetValue(UnmaskedTextProperty); }
            set
            {
                SetValue(UnmaskedTextProperty, value);
            }
        }

        public static readonly DependencyProperty UnmaskedTextProperty =
        DependencyProperty.Register("UnmaskedText", typeof(string),
        typeof(MaskedTextBox), new UIPropertyMetadata(""));

        /// <summary>
        /// Gets the MaskTextProvider for the specified Mask
        /// </summary>
        public MaskedTextProvider MaskProvider
        {
            get
            {
                if (_maskProvider == null)
                {
                    _maskProvider = CreateMaskedTextProvider(Mask);
                    _maskProvider.Set(Text);
                }
                return _maskProvider;
            }
        }

        MaskedTextProvider _maskProvider;

        static MaskedTextProvider CreateMaskedTextProvider(string mask)
        {
            return new MaskedTextProvider(mask, Thread.CurrentThread.CurrentUICulture);
        }

        public string Mask { get; set; }

#if (false)
        /// <summary>
        /// Gets or sets the mask to apply to the textbox
        /// </summary>
        public string Mask
        {
            get { return (string)GetValue(MaskProperty); }
            set { SetValue(MaskProperty, value); }
        }

        /// <summary>
        /// Dependency property to store the mask to apply to the textbox
        /// </summary>
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register("Mask", typeof(string), typeof(MaskedTextBox), new UIPropertyMetadata(null, OnMaskChanged));

        //callback for when the Mask property is changed
        static void OnMaskChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            //make sure to update the text if the mask changes
            MaskedTextBox textBox = (MaskedTextBox)sender;
            textBox.RefreshText(textBox.MaskProvider, 0);
        }
        

        /// <summary>
        /// Static Constructor
        /// </summary>
        static MaskedTextBox()
        {
            //override the meta data for the Text Proeprty of the textbox 
            FrameworkPropertyMetadata metaData = new FrameworkPropertyMetadata();
            metaData.CoerceValueCallback = ForceText;
            TextProperty.OverrideMetadata(typeof(MaskedTextBox), metaData);
        }  

        // Force the text of the control to use the mask.
        private static object ForceText(DependencyObject sender, object value)
        {
            MaskedTextBox textBox = (MaskedTextBox)sender;
            if (textBox.Mask != null)
            {
                MaskedTextProvider provider = CreateMaskedTextProvider(textBox.Mask);
                provider.Set((string)value);
                return provider.ToDisplayString();
            }
            else
            {
                return value;
            }
        }
#endif

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!isRefreshingText)
            {
                e.Handled = false;
                bool replaced = MaskProvider.Replace(Text, 0);
                isRefreshingText = true;
                try
                {
                    Text = MaskProvider.ToDisplayString();
                }
                finally
                {
                    isRefreshingText = false;
                }
            }
            else
            {
                base.OnTextChanged(e);
            }
        }

        ///<summary>
        /// Default  constructor
        ///</summary>
        public MaskedTextBox6()
        {
            //cancel the paste and cut command
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, null, CancelCommand));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, CancelCommand));

            Mask = "0.00"; // +NumberFormatInfo.CurrentInfo.NumberDecimalSeparator + "00"; 

            Loaded += (s, e) => OnLoaded();
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~

        void OnLoaded()
        {
            //if (string.IsNullOrWhiteSpace(UnmaskedText))
            //    MaskProvider.Set(string.Empty);
            //else
            //    MaskProvider.Set(UnmaskedText);

            //Text = MaskProvider.ToDisplayString();

            //var textProp = DependencyPropertyDescriptor.FromProperty(MaskedTextBox.TextProperty, typeof(MaskedTextBox));
            //if (textProp != null)
            //{
            //    textProp.AddValueChanged(this, (s, args) => UpdateText());
            //}

            //DataObject.AddPastingHandler(this, Pasting);
        }


        // ~~~~~~~~~~~~~~~~~~~~~~~


        //cancel the command
        private static void CancelCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

    #region Overrides

        /// <summary>
        /// override this method to replace the characters enetered with the mask
        /// </summary>
        /// <param name="e">Arguments for event</param>
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            //if the text is readonly do not add the text
            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            int position = SelectionStart;
            if (position < Text.Length)
            {
                position = GetNextCharacterPosition(position);

                // Note that we'll handle all input as if in overtype mode.
                if (MaskProvider.Replace(e.Text, position))
                    position++;
#if (false)
                if (Keyboard.IsKeyToggled(Key.Insert))
                {
                    if (MaskProvider.Replace(e.Text, position))
                        position++;
                }
                else
                {
                    if (MaskProvider.InsertAt(e.Text, position))
                        position++;
                }
#endif

                position = GetNextCharacterPosition(position);
            }

            RefreshText(position);
            e.Handled = true;

            base.OnPreviewTextInput(e);
        }

        /// <summary>
        /// override the key down to handle delete of a character
        /// </summary>
        /// <param name="e">Arguments for the event</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            int position = SelectionStart;
            if (e.Key == Key.Delete && position < Text.Length)//handle the delete key
            {
                if (MaskProvider.RemoveAt(position))
                    RefreshText(position);

                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                if (MaskProvider.InsertAt(" ", position))
                    RefreshText(position);
                e.Handled = true;
            }

            else if (e.Key == Key.Back)//handle the back space
            {
                if (position > 0)
                {
                    MaskedTextResultHint hint;
                    int testPosition;

                    while (position > 0)
                    {
                        position--;

                        if (MaskProvider.RemoveAt(position, position, out testPosition, out hint))
                        {
                            if (hint == MaskedTextResultHint.Success)
                            {
                                RefreshText(position);
                                break;
                            }
                            else if (hint == MaskedTextResultHint.NoEffect)
                            {
                                // If the position is not editable, then we'll just move the caret.
                                SelectionStart = position;
                            }
                            else
                                break;
                        }
                        else
                            break;
                    }
                }
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }
    #endregion


        bool isRefreshingText;

        // Refreshes the text of the textbox.
        private void RefreshText(int position)
        {
            isRefreshingText = true;
            try
            {
                Text = MaskProvider.ToDisplayString();
                SelectionStart = position;
            }
            finally
            {
                isRefreshingText = false;
            }
        }

        // gets the next position in the textbox to move
        private int GetNextCharacterPosition(int startPosition)
        {
            int position = MaskProvider.FindEditPositionFrom(startPosition, true);
            if (position == -1)
                return startPosition;
            else
                return position;
        }
    }

    public class MaskedTextBox3 : TextBox
    {
        ///<summary>
        /// Default  constructor
        ///</summary>
        public MaskedTextBox3()
        {
            //CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, null, CancelCommand));            
            //CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, CancelCommand));

            Loaded += new RoutedEventHandler(MaskedTextBox_Loaded);
        }

        void MaskedTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            string format = "0" + NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator[0] + "00";

            _mask = new MaskedTextProvider(format, Thread.CurrentThread.CurrentUICulture);
            Text = _mask.ToDisplayString();
        }

        MaskedTextProvider _mask;

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
        }

        /// <summary>
        /// Override the key down to handle deletion of a character.
        /// </summary>        
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            MaskedTextResultHint hint;
            int testPosition;
            bool valid = true;
            string text = e.Text;

            if (text.Length == 1)
                valid = _mask.VerifyChar(text[0], CaretIndex, out hint);
            else
                valid = _mask.VerifyString(text, out testPosition, out hint);

            string previousText = Text;

            if (valid)
            {
                base.OnTextInput(e);

                if (!_mask.VerifyString(Text))
                {
                    Text = previousText;
                }

                while
                    ((!_mask.IsEditPosition(CaretIndex) &&
                     (_mask.Length > CaretIndex)))
                {
                    CaretIndex++;
                }

            }
            else
            {
                e.Handled = false;
                base.OnTextInput(e);
            }
        }

        // Cancel the command.
        private static void CancelCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        private bool PreviousInsertState = false;
        private bool _InsertIsON = false;
        private bool _stayInFocusUntilValid = true;

        /// <summary>
        /// When the TextBox takes the focus we make sure that the Insert is set to Replace
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            if (!_InsertIsON)
            {
                PressKey(Key.Insert);
                _InsertIsON = true;
            }
        }

        /// <summary>
        /// When the textbox looses the keyboard focus we may want to verify (based on the StayInFocusUntilValid) whether
        /// the control has a valid value (fully complete)
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            //if (StayInFocusUntilValid)
            //{
            //    _mprovider.Clear();
            //    _mprovider.Add(Text);
            //    if (!_mprovider.MaskFull)
            //        e.Handled = true;
            //}

            base.OnPreviewLostKeyboardFocus(e);
        }

        /// <summary>
        /// When the textbox looses its focus we need to return the Insert Key state to its previous state
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (PreviousInsertState != Keyboard.PrimaryDevice.IsKeyToggled(Key.Insert))
                PressKey(Key.Insert);
        }

        /// <summary>
        /// Simulates pressing a key
        /// </summary>
        /// <param name="key">The key to be pressed</param>
        private void PressKey(Key key)
        {
            KeyboardDevice keyboard = Keyboard.PrimaryDevice;
            //PresentationSource inputSource = Keyboard.PrimaryDevice.ActiveSource;

            KeyEventArgs args =
                new KeyEventArgs(
                    keyboard,
                    PresentationSource.FromVisual(this as Visual),
                    0,
                    key);

            args.RoutedEvent = UIElement.KeyDownEvent; // Keyboard.KeyDownEvent;
            InputManager.Current.ProcessInput(args);

            //KeyEventArgs eInsertBack = new KeyEventArgs(Keyboard.PrimaryDevice,
            //                                            Keyboard.PrimaryDevice.ActiveSource,
            //                                            0, key);
            //eInsertBack.RoutedEvent = KeyDownEvent;
            //InputManager.Current.ProcessInput(eInsertBack);
        }
    }
    /// <summary>
    /// The MaskedTextBox uses ValidationAttribute in order to mask the text box.
    /// Well, it doesn't actually mask the text box (yet ?), but tries to disallow invalid content.
    /// </summary>
    public class MaskedTextBox2 : TextBox
    {
        ///<summary>
        /// Default  constructor
        ///</summary>
        public MaskedTextBox2()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, null, CancelCommand));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, CancelCommand));

            Loaded += new RoutedEventHandler(MaskedTextBox_Loaded);
        }

        void MaskedTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            BuildValidationDescription();
        }

        // Cancel the command.
        private static void CancelCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == TextBox.TextProperty)
            {
                BuildValidationDescription();
            }
            else if (e.Property == FrameworkElement.DataContextProperty)
            {
                BuildValidationDescription();
            }
        }

        /// <summary>
        /// Override this method to replace the characters entered with the mask.
        /// 
        ///  Invoked when an unhandled System.Windows.Input.TextCompositionManager.PreviewTextInput attached
        ///     event reaches an element in its route that is derived from this class. Implement
        ///     this method to add class handling for this event.
        ///
        /// Parameters:
        ///   e:
        ///     The System.Windows.Input.TextCompositionEventArgs that contains the event data.
        /// </summary>        
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            bool isValid = true;

            if (FloatTypes.Contains(_validation.PropertyType))
            {
                string text = (Text != null) ? Text : string.Empty;

                MathPrecisionValidationAttribute precision = _validation.FloatValidation;
                MathSignValidationAttribute sign = _validation.SignValidation;

                if (precision != null)
                {
                    // Restrict length of input.

                    int max = 0;

                    if (precision != null)
                        max += precision.MaxIntegerDigits + precision.MaxFractionalDigits;

                    if (sign != null || (precision != null && precision.MaxFractionalDigits > 0))
                    {
                        // Plus one for the decimal separator.
                        max++;
                    }

                    if (text.Length >= max)
                        isValid = false;

                    if (isValid)
                    {
                        char decSep = NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator[0];
                        bool hasDecimalSeparator = text.Contains(decSep);

                        // Disallow decimal separators:
                        // 1) if duplicate
                        // 2) if no fractional digits are allowed
                        if ((hasDecimalSeparator || precision.MinFractionalDigits <= 0) &&
                            e.Text == decSep.ToString())
                        {
                            isValid = false;
                        }
                    }
                }


                // isValid = ValidateFloat(Text);
            }

            e.Handled = !isValid;

            base.OnTextInput(e);
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
        }

        static readonly ReadOnlyCollection<Type> FloatTypes = new ReadOnlyCollection<Type>(
            new Type[] {
                typeof(double), typeof(double?), typeof(Single), typeof(Single?), typeof(decimal), typeof(decimal?)
            });



        /// <summary>
        /// Override the key down to handle deletion of a character.
        /// </summary>        
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
        }

        void BuildValidationDescription()
        {
            if (_validation != null)
                return;

            _validation = ValidationDescriptor.Parse(this, false, _validatedProperties);
        }

        ValidationDescription _validation;
        static readonly string[] _validatedProperties = new string[1] { "TextProperty" };
    }

    public class ValidationDescriptor
    {
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.RegisterAttached("Description",
                typeof(ValidationDescription), typeof(ValidationDescriptor), null);

        public static ValidationDescription GetValidationDescription(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            return (ValidationDescription)element.GetValue(DescriptionProperty);
        }

        public static void SetValidationDescription(DependencyObject element, ValidationDescription value)
        {
            if (element == null)
                throw new ArgumentNullException("target");

            element.SetValue(DescriptionProperty, value);
        }

        public static ValidationDescription Parse(FrameworkElement element, bool forceUpdate, string[] names)
        {
            const BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static;

            if (element == null)
                return null;

            ValidationDescription validation = null;

            if (!forceUpdate)
            {
                // Use cached description if available
                validation = (ValidationDescription)element.GetValue(DescriptionProperty);
                if (validation != null)
                    return validation;
            }

            // Find binding information on the given properties.

            IEnumerable<FieldInfo> infos =
                element.GetType()
                .GetFields(flags)
                .Where((field) => names.Contains(field.Name));

            BindingExpression expr = null;
            BindingExpression resultExpr = null;
            object bindingSource = null;
            string bindingPath = null;

            foreach (FieldInfo field in infos)
            {
                // We need a DependencyProperty.
                if (field.FieldType != typeof(DependencyProperty))
                    continue;

                expr = element.GetBindingExpression((DependencyProperty)field.GetValue(null));
                if (expr == null)
                    continue;

                // Get the Binding.
                if (expr.ParentBinding == null)
                    continue;

                // Get the Binding Path.
                // TODO: Do we really need a path?
                if (expr.ParentBinding.Path == null)
                    continue;

                // Get the Binding Source.
                bindingSource = (expr.DataItem != null) ? expr.DataItem : element.DataContext;
                if (bindingSource == null)
                    continue;

                resultExpr = expr;
                bindingPath = expr.ParentBinding.Path.Path;

                break;
            }

            if (resultExpr == null)
                return null;

            // Find the target property.
            PropertyInfo prop = BindingHelper.GetPropertyInfo(bindingSource, bindingPath);
            if (prop == null)
                return null;

            // Build the validaton description.

            validation = new ValidationDescription();

            validation.PropertyType = prop.PropertyType;

            DisplayAttribute display;
            MathPrecisionValidationAttribute @float;
            foreach (object attr in prop.GetCustomAttributes(false))
            {
                if (attr is ValidationAttribute)
                {
                    // Gather all validation attributes.
                    validation.Attributes.Add((ValidationAttribute)attr);
                }

                if (attr is RequiredAttribute)
                {
                    validation.IsRequired = true;
                }
                else if ((display = (attr as DisplayAttribute)) != null)
                {
                    validation.Description = display.GetDescription();
                    validation.Caption = display.GetName();
                }
                else if ((@float = (attr as MathPrecisionValidationAttribute)) != null)
                {
                    validation.FloatValidation = @float;
                    //validation.FractionalDigits = @float.FractionalDigits;
                }
            }

            // Fallback if there's no display information: we'll use the property's Name.
            if (validation.Caption == null)
                validation.Caption = prop.Name;

            // Cache the validation description.
            element.SetValue(DescriptionProperty, validation);

            return validation;
        }

        
    }

    public class ValidationDescription : ObservableObject
    {
        public ValidationDescription()
        {
            Attributes = new Collection<ValidationAttribute>();
        }

        public Type PropertyType { get; set; }

        public ICollection<ValidationAttribute> Attributes { get; private set; }

        public MathPrecisionValidationAttribute FloatValidation { get; set; }
        public MathSignValidationAttribute SignValidation { get; set; }

        public string Caption
        {
            get { return _caption; }
            set { SetProperty(CaptionChangedArgs, ref _caption, value); }
        }
        string _caption;
        public static readonly PropertyChangedEventArgs CaptionChangedArgs = new PropertyChangedEventArgs("Caption");

        public string Description
        {
            get { return _description; }
            set { SetProperty(DescriptionChangedArgs, ref _description, value); }
        }
        string _description;
        public static readonly PropertyChangedEventArgs DescriptionChangedArgs = new PropertyChangedEventArgs("Description");

        public bool IsRequired
        {
            get { return _isRequired; }
            set { SetValueTypeProperty(IsRequiredChangedArgs, ref _isRequired, value); }
        }
        bool _isRequired;
        public static readonly PropertyChangedEventArgs IsRequiredChangedArgs = new PropertyChangedEventArgs("IsRequired");


        // Source: http://www.codeguru.com/forum/showthread.php?p=1928942
        bool AreAllValidNumericChars(string str)
        {
            bool ret = true;
            if (str == System.Globalization.NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.CurrencyGroupSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.CurrencySymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.NegativeSign |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.NegativeInfinitySymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PercentDecimalSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PercentGroupSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PercentSymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PerMilleSymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PositiveInfinitySymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PositiveSign)
                return ret;

            int l = str.Length;
            for (int i = 0; i < l; i++)
            {
                char ch = str[i];
                ret &= Char.IsDigit(ch);
            }

            return ret;
        }
    }

#endif

}

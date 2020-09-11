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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Casimodo.Lib.Presentation.Input
{
#if (!SILVERLIGHT)
    public static class KeyboardHelper
    {
        public static void PressKey(Key key)
        {
            PresentationSource inputSource = null;
            IInputElement target = Keyboard.FocusedElement;
            if (target != null)
            {
                inputSource = PresentationSource.FromVisual(((object)target) as Visual);
            }
            else
            {
                inputSource = Keyboard.PrimaryDevice.ActiveSource;
            }         

            KeyEventArgs args =
                new KeyEventArgs(
                    Keyboard.PrimaryDevice,
                    inputSource,                    
                    0,
                    key);
           
            args.RoutedEvent = Keyboard.PreviewKeyDownEvent; // Keyboard.KeyDownEvent;
            InputManager.Current.ProcessInput(args);
        }

        public static void SimulateKey2(Key key)
        {
            PresentationSource inputSource = null;
            IInputElement target = Keyboard.FocusedElement;           
            if (target != null)
            {
                inputSource = PresentationSource.FromVisual(((object)target) as Visual);
            }
            else
            {
                inputSource = Keyboard.PrimaryDevice.ActiveSource;
            }         

            KeyEventArgs args =
                new KeyEventArgs(
                    Keyboard.PrimaryDevice,
                    inputSource,             
                    0,
                    key);               

            args.RoutedEvent = Keyboard.KeyDownEvent;
            InputManager.Current.ProcessInput(args);

            //args = Clone(args);
            //args.RoutedEvent = Keyboard.KeyDownEvent;
            //InputManager.Current.ProcessInput(args);

            //args = Clone(args);
            //args.RoutedEvent = Keyboard.PreviewKeyUpEvent;
            //InputManager.Current.ProcessInput(args);

            //args = Clone(args);
            //args.RoutedEvent = Keyboard.KeyUpEvent;
            //InputManager.Current.ProcessInput(args);
        }

        static KeyEventArgs Clone(KeyEventArgs args)
        {
            KeyEventArgs clone =
                   new KeyEventArgs(
                       args.KeyboardDevice,
                       args.InputSource,               
                       args.Timestamp +1,
                       args.Key);

            return clone;
        }
    }
#endif
}
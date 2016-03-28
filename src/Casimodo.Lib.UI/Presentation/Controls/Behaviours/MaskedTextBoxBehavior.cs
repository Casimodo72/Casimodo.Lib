using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Casimodo.Lib.Presentation
{    
    public class MaskedTextBoxBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            
            base.OnAttached();
            //AssociatedObject.Loaded += AssociatedObjectLoaded;
            //AssociatedObject.PreviewTextInput += AssociatedObjectPreviewTextInput;
            //AssociatedObject.PreviewKeyDown += AssociatedObjectPreviewKeyDown;

            //DataObject.AddPastingHandler(AssociatedObject, Pasting);
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            //AssociatedObject.Loaded -= AssociatedObjectLoaded;
            //AssociatedObject.PreviewTextInput -= AssociatedObjectPreviewTextInput;
            //AssociatedObject.PreviewKeyDown -= AssociatedObjectPreviewKeyDown;

            //DataObject.RemovePastingHandler(AssociatedObject, Pasting);
        }
    }
}
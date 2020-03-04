using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

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
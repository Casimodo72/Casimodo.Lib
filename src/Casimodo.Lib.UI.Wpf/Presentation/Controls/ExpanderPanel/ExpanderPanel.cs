
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Casimodo.Lib.Presentation.Controls
{
    [TemplatePart(Name = "PART_Content", Type = typeof(ContentControl))]
    public class ExpanderPanel : ContentControl
    {
        /// <summary>
        /// The "IsExpanded" property (DP).
        /// <summary>        
        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        /// <summary>
        /// The "IsExpandedProperty" dependency property.
        /// <summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool), typeof(ExpanderPanel),
                new PropertyMetadata(true, (d, e) => ((ExpanderPanel)d).OnIsExpandedPropertyChanged(e)));

        void OnIsExpandedPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            //if ((bool)e.NewValue == false)
            //{
            //    VisualStateManager.GoToState(this, "Collapsed", true);
            //}
            //else
            //{
            //    VisualStateManager.GoToState(this, "Expanded", true);
            //}


            //(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.Y)            

            if ((bool)e.NewValue == false)
            {
                _origHeight = this.ActualHeight;

                StartAni(-_origHeight);

                //FrameworkElement elem = Content as FrameworkElement;
                //if (elem != null)
                //{
                //    var paddingAni = new ThicknessAnimation();
                //    paddingAni.From = new Thickness(0);
                //    paddingAni.To = new Thickness(0, _origHeight * -1, 0, 0);
                //    paddingAni.Duration = new Duration(TimeSpan.FromSeconds(0.5d));

                //    Storyboard.SetTarget(paddingAni, this);
                //    Storyboard.SetTargetProperty(paddingAni, new PropertyPath(Control.PaddingProperty));
                //    story.Children.Add(paddingAni);
                //}               
            }
            else
            {
                StartAni(0);
            }
        }

        void StartAni(double to)
        {
            ContentPresenter pane = GetTemplateChild("PART_Content") as ContentPresenter;

            Storyboard story = new Storyboard();
            story.Completed += OnStoryCompleted;

            DoubleAnimation ani = new DoubleAnimation();
            ani.To = to;
            ani.Duration = new Duration(TimeSpan.FromSeconds(0.3d));
            Storyboard.SetTarget(ani, pane);
            Storyboard.SetTargetProperty(ani, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.Y)"));

            story.Children.Add(ani);

            BeginStoryboard(story, HandoffBehavior.SnapshotAndReplace, false);
        }

        void OnStoryCompleted(object sender, EventArgs e)
        {
            var clock = (ClockGroup)sender;
            if (clock.CurrentState == ClockState.Filling)
            {
                _isCollapsing = false;
            }
        }
#pragma warning disable
        bool _isCollapsing;
#pragma warning restore
        double _origHeight;

        //protected override void OnChildDesiredSizeChanged(UIElement child)
        //{
        //    base.OnChildDesiredSizeChanged(child);

        //    desiredChildSize = child.DesiredSize;
        //}

        //protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        //{
        //    base.OnRenderSizeChanged(sizeInfo);

        //    if (_isCollapsing)
        //    {
        //        FrameworkElement elem = Content as FrameworkElement;
        //        if (elem != null)
        //        {
        //            double deltaY = _origHeight - sizeInfo.NewSize.Height;
        //            elem.Margin = new Thickness(0, -deltaY, 0, 0);
        //        }
        //    }
        //}

        //Size measuredSize = Size.Empty;
        //Size desiredChildSize = Size.Empty;

        //protected override Size MeasureOverride(Size constraint)
        //{
        //    measuredSize = base.MeasureOverride(constraint);

        //    return measuredSize;
        //}

        //protected override Size MeasureOverride(Size constraint)
        //{
        //    //if (this.Child != null)
        //    //    this.Child.Measure(constraint);

        //    Size size =
        //        new Size(
        //            double.IsInfinity(constraint.Width) ? 1 : constraint.Width,
        //            double.IsInfinity(constraint.Height) ? 1 : constraint.Height);

        //    return size;
        //}

        //protected override Size ArrangeOverride(Size arrangeSize)
        //{
        //    if (Content as UIElement == null)
        //    {
        //        return base.ArrangeOverride(arrangeSize);
        //    }

        //    UIElement elem = (UIElement)Content;

        //    elem.Measure(arrangeSize);
        //    elem.Arrange(new Rect(arrangeSize));

        //    return arrangeSize;
        //}
    }
}
using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Casimodo.Lib.Presentation
{
    internal class UIElementFadeAnimation : AnimationTimeline
    {
        static UIElementFadeAnimation()
        {
            FromProperty = DependencyProperty.Register("From", typeof(GridLength),
                typeof(UIElementFadeAnimation));

            ToProperty = DependencyProperty.Register("To", typeof(GridLength),
                typeof(UIElementFadeAnimation));
        }

        public override Type TargetPropertyType
        {
            get { return typeof(GridLength); }
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new UIElementFadeAnimation();
        }

        public static readonly DependencyProperty FromProperty;
        public GridLength From
        {
            get
            {
                return (GridLength)GetValue(UIElementFadeAnimation.FromProperty);
            }
            set
            {
                SetValue(UIElementFadeAnimation.FromProperty, value);
            }
        }

        public static readonly DependencyProperty ToProperty;
        public GridLength To
        {
            get
            {
                return (GridLength)GetValue(UIElementFadeAnimation.ToProperty);
            }
            set
            {
                SetValue(UIElementFadeAnimation.ToProperty, value);
            }
        }

        public override object GetCurrentValue(object defaultOriginValue,
            object defaultDestinationValue, AnimationClock animationClock)
        {
            double fromVal = ((GridLength)GetValue(UIElementFadeAnimation.FromProperty)).Value;
            double toVal = ((GridLength)GetValue(UIElementFadeAnimation.ToProperty)).Value;

            if (fromVal > toVal)
            {
                return new GridLength((1 - animationClock.CurrentProgress.Value) * (fromVal - toVal) + toVal, GridUnitType.Star);
            }
            else
                return new GridLength(animationClock.CurrentProgress.Value * (toVal - fromVal) + fromVal, GridUnitType.Star);
        }
    }
}
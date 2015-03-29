using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace JMC_Photo_Gallery
{
    public class FadeInPanel : Panel
    {
        public static TimeSpan FadeTime { get; set; }

        public FadeInPanel()
            : base()
        {
        }

        // Still using animation, because compose is required to animate properly
        private void FadeIn(DependencyObject fadeObj, TimeSpan duration)
        {
            Storyboard myFadeStoryboard = new Storyboard();
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = 0;
            myDoubleAnimation.To = 1;
            myDoubleAnimation.BeginTime = TimeSpan.FromSeconds(0);
            myDoubleAnimation.Duration = new Duration(duration);
            Storyboard.SetTargetProperty(myDoubleAnimation, new PropertyPath(Border.OpacityProperty));
            myFadeStoryboard.Children.Add(myDoubleAnimation);
            myFadeStoryboard.Begin((FrameworkElement)fadeObj, HandoffBehavior.Compose);
        }

        // Override the default Measure method of Panel
        // <Snippet2>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size childSize = availableSize;
            foreach (UIElement child in InternalChildren)
            {
                child.Measure(childSize);
            }
            return availableSize;
        }

        //</Snippet2>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in InternalChildren)
            {
                child.Arrange(new Rect(new Point(0, 0), child.DesiredSize));
            }
            return finalSize; // Returns the final Arranged size
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            // Add animation
            if (visualAdded != null)
                FadeIn(visualAdded, FadeTime);

            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }
    }
}
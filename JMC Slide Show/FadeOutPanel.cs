using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace JMC_Photo_Gallery
{
    // This will fade out newly added elements.
    // Will not remove elements that are completely faded out.
    // You will need to clear elements yourself.

    public class FadeOutPanel : Panel
    {
        public static TimeSpan FadeTime { get; set; }

        public FadeOutPanel()
            : base()
        {
        }

        // Still using animation, because compose is required to animate properly
        private void FadeOut(DependencyObject fadeObj, TimeSpan duration)
        {
            Storyboard myFadeStoryboard = new Storyboard();
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = 1;
            myDoubleAnimation.To = 0;
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
                FadeOut(visualAdded, FadeTime);

            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }
    }
}
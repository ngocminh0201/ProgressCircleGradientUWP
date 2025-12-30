using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ProgressCircleGradientUWP.Controls.ProgressCircle
{
    public abstract class ProgressCircle : Control
    {
        #region Protected Variables
        internal IDictionary<ProgressCircleIndeterminateOrientation, string> _progressCircleTextAlingmentDictionary = new Dictionary<ProgressCircleIndeterminateOrientation, string>
        {
            { ProgressCircleIndeterminateOrientation.Vertical, "TextVerticalAlignment" },
            { ProgressCircleIndeterminateOrientation.Horizontal, "TextHorizontalAlignment" }
        };

        private long _tokenOnFlowDirectionPropertyChanged;
        #endregion

        #region Size Property
        public ProgressCircleSize Size
        {
            get => (ProgressCircleSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(ProgressCircleSize), typeof(ProgressCircle), 
                new PropertyMetadata(ProgressCircleSize.Small, OnSizePropertyChangedInternal));

        private static void OnSizePropertyChangedInternal(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircle progressCircle)
                progressCircle.OnSizePropertyChanged(d, e);
        }

        protected abstract void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e);
        #endregion

        #region Text Property
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ProgressCircle), 
                new PropertyMetadata(null));
        #endregion

        protected internal void ForceLeftToRightFlowDirection()
        {
            if (FlowDirection != FlowDirection.LeftToRight)
                FlowDirection = FlowDirection.LeftToRight;
        }

        protected ProgressCircle()
        {
            FlowDirection = FlowDirection.LeftToRight;
            Unloaded += ProgressCircle_Unloaded;
            _tokenOnFlowDirectionPropertyChanged = RegisterPropertyChangedCallback(FlowDirectionProperty, OnFlowDirectionPropertyChanged);
        }

        private void ProgressCircle_Unloaded(object sender, RoutedEventArgs e)
        {
            UnregisterPropertyChangedCallback(FlowDirectionProperty, _tokenOnFlowDirectionPropertyChanged);
        }

        private void OnFlowDirectionPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            ForceLeftToRightFlowDirection();
        }
    }
}

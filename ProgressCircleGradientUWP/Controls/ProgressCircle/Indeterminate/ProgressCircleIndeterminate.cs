using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using ProgressCircleGradientUWP.Brushes;
using ProgressCircleGradientUWP.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgressCircleGradientUWP.Controls.ProgressCircle
{
    public partial class ProgressCircleIndeterminate : ProgressCircle
    {
        #region Constants
        private const string PART_ROOT_GRID_NAME = "PART_RootGrid";
        private const string PART_TEXT_NAME = "PART_text";
        private const string PART_STORYBOARD_NAME = "RotateAnimation";

        private const string PART_ELLIPSEPOINT = "PART_EllipsePoint";
        private const string PART_ELLIPSE01 = "PART_Ellipse01";
        private const string PART_ELLIPSE02 = "PART_Ellipse02";
        private const string PART_ELLIPSE03 = "PART_Ellipse03";

        private const string ELLIPSE_INDETERMINATE_KEY = "#387AFF";
        private const string VARIANT_ELLIPSE_INDETERMINATE_KEY = "#3DCC87";

        private const double ELLIPSE_BASE_SIZE = 4.5;
        private const double ELLIPE_BASE_MIN_OFFSET = 1.5;
        private const double ELLIPE_BASE_MAX_OFFSET = 6.5;
        private const double ELLIPE_BASE_DISPLACEMENT = 5;
        private const double ELLIPE_BASE_DISPLACEMENT_REVERSE = -4;

        private const int GRIDSIZE_XL = 90;
        private const int GRIDSIZE_LG = 60;
        private const int GRIDSIZE_MD = 48;
        private const int GRIDSIZE_SM = 24;
        private const int GRIDSIZE_ST = 16;
        #endregion

        #region Variables
        private Windows.UI.Xaml.FrameworkElement _rootGrid;
        private TextBlock _text;
        private Storyboard _rotateAnimation;
        private Windows.UI.Xaml.Shapes.Shape _ellipsePoint, _ellipse01, _ellipse02, _ellipse03;

        private Brush _ellipseIndeterminateBrush = ColorsHelpers.ConvertColorHex(ELLIPSE_INDETERMINATE_KEY);
        private Brush _variantEllipseIndeterminateBrush = ColorsHelpers.ConvertColorHex(VARIANT_ELLIPSE_INDETERMINATE_KEY);

        private long _visibilityPropertyRegisterToken;

        private bool _isAngularMode;
        private bool _isRenderingSubscribed;

        private SolidColorBrush _AngularDotBrush01;
        private SolidColorBrush _AngularDotBrushPoint;
        private SolidColorBrush _AngularDotBrush02;
        private SolidColorBrush _AngularDotBrush03;

        private readonly List<ProgressCircleIndeterminateModel> _progressCircleIndeterminateModels = new List<ProgressCircleIndeterminateModel>
        {
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.XLarge, Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 3.75, GridSize = 90 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Large,  Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 2.5,  GridSize = 60 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Medium, Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 2.0,  GridSize = 48 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Small,  Orientation = ProgressCircleIndeterminateOrientation.Horizontal, Scale = 1.0,  GridSize = 24 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.SmallTitle, Orientation = ProgressCircleIndeterminateOrientation.Horizontal, Scale = 0.67, GridSize = 16 },
        };
        #endregion

        #region Dependency Properties
        public new Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly new DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(ProgressCircleIndeterminate),
                new PropertyMetadata(new SolidColorBrush(Colors.Transparent), OnForegroundPropertyChanged));

        public Brush PointForeground
        {
            get => (Brush)GetValue(PointForegroundProperty);
            set => SetValue(PointForegroundProperty, value);
        }

        public static readonly DependencyProperty PointForegroundProperty =
            DependencyProperty.Register(nameof(PointForeground), typeof(Brush), typeof(ProgressCircleIndeterminate),
                new PropertyMetadata(new SolidColorBrush(Colors.Transparent), OnPointForegroundPropertyChanged));

        internal double EllipseDiameter
        {
            get => (double)GetValue(EllipseDiameterProperty);
            set => SetValue(EllipseDiameterProperty, value);
        }

        internal static readonly DependencyProperty EllipseDiameterProperty =
            DependencyProperty.Register(nameof(EllipseDiameter), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        internal double EllipseDisplacementPosition
        {
            get => (double)GetValue(EllipseDisplacementPositionProperty);
            set => SetValue(EllipseDisplacementPositionProperty, value);
        }

        internal static readonly DependencyProperty EllipseDisplacementPositionProperty =
            DependencyProperty.Register(nameof(EllipseDisplacementPosition), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        internal double EllipseNegativeDisplacement
        {
            get => (double)GetValue(EllipseNegativeDisplacementProperty);
            set => SetValue(EllipseNegativeDisplacementProperty, value);
        }

        internal static readonly DependencyProperty EllipseNegativeDisplacementProperty =
            DependencyProperty.Register(nameof(EllipseNegativeDisplacement), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        internal double EllipseMinOffset
        {
            get => (double)GetValue(EllipseMinOffsetProperty);
            set => SetValue(EllipseMinOffsetProperty, value);
        }

        internal static readonly DependencyProperty EllipseMinOffsetProperty =
            DependencyProperty.Register(nameof(EllipseMinOffset), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        internal double EllipseMaxOffset
        {
            get => (double)GetValue(EllipseMaxOffsetProperty);
            set => SetValue(EllipseMaxOffsetProperty, value);
        }

        internal static readonly DependencyProperty EllipseMaxOffsetProperty =
            DependencyProperty.Register(nameof(EllipseMaxOffset), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));
        #endregion

        #region Constructors
        public ProgressCircleIndeterminate() : base()
        {
            DefaultStyleKey = typeof(ProgressCircleIndeterminate);
            Loaded += ProgressCircleIndeterminate_Loaded;
            Unloaded += ProgressCircleIndeterminate_Unloaded;
        }
        #endregion

        #region Override Methods
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _rootGrid = (Grid)GetTemplateChild(PART_ROOT_GRID_NAME);
            _text = (TextBlock)GetTemplateChild(PART_TEXT_NAME);

            _ellipsePoint = (Ellipse)GetTemplateChild(PART_ELLIPSEPOINT);
            _ellipse01 = (Ellipse)GetTemplateChild(PART_ELLIPSE01);
            _ellipse02 = (Ellipse)GetTemplateChild(PART_ELLIPSE02);
            _ellipse03 = (Ellipse)GetTemplateChild(PART_ELLIPSE03);

            _rotateAnimation = (Storyboard)GetTemplateChild(PART_STORYBOARD_NAME);

            UpdateProgressCircleLayout();
            UpdateCircleScale();

            UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
        }

        protected override void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateCircleScale();
            UpdateProgressCircleLayout();

            UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
        }
        #endregion

        #region Event Handlers
        private void ProgressCircleIndeterminate_Loaded(object sender, RoutedEventArgs e)
        {
            if (_visibilityPropertyRegisterToken == 0)
            {
                _visibilityPropertyRegisterToken = RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityPropertyChanged);
            }

            UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
        }

        private void ProgressCircleIndeterminate_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_visibilityPropertyRegisterToken != 0)
            {
                UnregisterPropertyChangedCallback(VisibilityProperty, _visibilityPropertyRegisterToken);
                _visibilityPropertyRegisterToken = 0;
            }

            StopAngularRendering();

            _rotateAnimation?.Stop();
        }

        private static void OnForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleIndeterminate self)
            {
                self._ellipseIndeterminateBrush = (Brush)e.NewValue;
                self.UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
            }
        }

        private static void OnPointForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleIndeterminate self)
            {
                self._variantEllipseIndeterminateBrush = (Brush)e.NewValue;
                self.UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
            }
        }

        private static void OnVisibilityPropertyChanged(DependencyObject d, DependencyProperty dp)
        {
            if (d is ProgressCircleIndeterminate self)
            {
                if (self.Visibility == Visibility.Collapsed)
                {
                    self.StopAngularRendering();
                    self._rotateAnimation?.Stop();
                }
                else
                {
                    self.UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
                }
            }
        }
        #endregion

        #region Private Methods
        private void UpdateCircleScale()
        {
            var progressDefinition = _progressCircleIndeterminateModels.FirstOrDefault(x => x.Size == Size);
            if (progressDefinition == null)
                return;

            SetTextAlignment(progressDefinition.Orientation);

            EllipseDiameter = ELLIPSE_BASE_SIZE * progressDefinition.Scale;
            EllipseMinOffset = ELLIPE_BASE_MIN_OFFSET * progressDefinition.Scale;
            EllipseMaxOffset = ELLIPE_BASE_MAX_OFFSET * progressDefinition.Scale;
            EllipseDisplacementPosition = ELLIPE_BASE_DISPLACEMENT * progressDefinition.Scale;
            EllipseNegativeDisplacement = ELLIPE_BASE_DISPLACEMENT_REVERSE * progressDefinition.Scale;
        }

        private void SetTextAlignment(ProgressCircleIndeterminateOrientation orientation)
        {
            if (_progressCircleTextAlingmentDictionary.TryGetValue(orientation, out string alignment))
            {
                VisualStateManager.GoToState(this, alignment, true);
            }
        }

        private void UpdateProgressCircleLayout()
        {
            var progressDefinition = _progressCircleIndeterminateModels.FirstOrDefault(x => x.Size == Size);
            if (progressDefinition == null)
                return;

            UpdateRootGridSize(progressDefinition);
            UpdateFontSizeMessage(progressDefinition);
        }

        private void UpdateRootGridSize(ProgressCircleIndeterminateModel progressDefinition)
        {
            if (_rootGrid == null)
                return;

            _rootGrid.Width = progressDefinition.GridSize;
            _rootGrid.Height = progressDefinition.GridSize;
        }

        private void UpdateFontSizeMessage(ProgressCircleIndeterminateModel progressDefinition)
        {
            if (_text == null)
                return;

            _text.FontSize = progressDefinition.GridSize switch
            {
                GRIDSIZE_XL => 13,
                GRIDSIZE_LG => 13,
                GRIDSIZE_MD => 12,
                GRIDSIZE_SM => 11,
                GRIDSIZE_ST => 11,
                _ => 13
            };
        }

        private void BeginAnimationIfVisible()
        {
            if (Visibility != Visibility.Collapsed)
            {
                _rotateAnimation?.Begin();
            }
        }

        private void ResetAnimationToInitialFrame()
        {
            if (_rootGrid?.RenderTransform is RotateTransform rt)
            {
                rt.Angle = 0;
            }

            if (_ellipse01?.RenderTransform is TranslateTransform t1)
            {
                t1.X = 0;
                t1.Y = 0;
            }
            if (_ellipsePoint?.RenderTransform is TranslateTransform t2)
            {
                t2.X = 0;
                t2.Y = 0;
            }
            if (_ellipse02?.RenderTransform is TranslateTransform t3)
            {
                t3.X = 0;
                t3.Y = 0;
            }
            if (_ellipse03?.RenderTransform is TranslateTransform t4)
            {
                t4.X = 0;
                t4.Y = 0;
            }
        }

        private void UpdateDotBrushesAndMaybeRestartAnimation(bool restartAnimation)
        {
            if (_ellipse01 == null || _ellipse02 == null || _ellipse03 == null || _ellipsePoint == null)
                return;

            if (restartAnimation)
            {
                _rotateAnimation?.Stop();
                ResetAnimationToInitialFrame();
            }

            if (GetAngularBrushSourceOrNull() != null)
            {
                EnsureAngularDotBrushesAssigned();
                UpdateAngularDotColors();
                StartAngularRendering();
                _isAngularMode = true;
            }
            else
            {
                _isAngularMode = false;
                StopAngularRendering();
                ApplyNormalDotBrushes();
            }

            if (restartAnimation)
            {
                BeginAnimationIfVisible();
            }
        }

        private AngularGradientBrush GetAngularBrushSourceOrNull()
        {
            if (PointForeground is AngularGradientBrush c1)
                return c1;
            if (Foreground is AngularGradientBrush c2)
                return c2;
            return null;
        }

        private void EnsureAngularDotBrushesAssigned()
        {
            if (_AngularDotBrush01 == null) _AngularDotBrush01 = new SolidColorBrush(Colors.Transparent);
            if (_AngularDotBrushPoint == null) _AngularDotBrushPoint = new SolidColorBrush(Colors.Transparent);
            if (_AngularDotBrush02 == null) _AngularDotBrush02 = new SolidColorBrush(Colors.Transparent);
            if (_AngularDotBrush03 == null) _AngularDotBrush03 = new SolidColorBrush(Colors.Transparent);

            if (_ellipse01 != null) _ellipse01.Fill = _AngularDotBrush01;
            if (_ellipsePoint != null) _ellipsePoint.Fill = _AngularDotBrushPoint;
            if (_ellipse02 != null) _ellipse02.Fill = _AngularDotBrush02;
            if (_ellipse03 != null) _ellipse03.Fill = _AngularDotBrush03;
        }

        private void StartAngularRendering()
        {
            if (_isRenderingSubscribed)
                return;

            if (Visibility == Visibility.Collapsed)
                return;

            Windows.UI.Xaml.Media.CompositionTarget.Rendering += OnCompositionTargetRendering;
            _isRenderingSubscribed = true;
        }

        private void StopAngularRendering()
        {
            if (!_isRenderingSubscribed)
                return;

            Windows.UI.Xaml.Media.CompositionTarget.Rendering -= OnCompositionTargetRendering;
            _isRenderingSubscribed = false;
        }

        private void OnCompositionTargetRendering(object sender, object e)
        {
            if (!_isAngularMode)
                return;

            if (Visibility == Visibility.Collapsed)
                return;

            UpdateAngularDotColors();
        }

        private void UpdateAngularDotColors()
        {
            if (_rootGrid == null || _ellipse01 == null || _ellipse02 == null || _ellipse03 == null || _ellipsePoint == null)
                return;

            double w = (_rootGrid.ActualWidth > 0) ? _rootGrid.ActualWidth : _rootGrid.Width;
            double h = (_rootGrid.ActualHeight > 0) ? _rootGrid.ActualHeight : _rootGrid.Height;

            if (w <= 0 || h <= 0)
            {
                var def = _progressCircleIndeterminateModels.FirstOrDefault(x => x.Size == Size);
                if (def != null)
                {
                    w = def.GridSize;
                    h = def.GridSize;
                }
                else
                {
                    w = h = 24;
                }
            }

            Windows.Foundation.Point center;
            try
            {
                center = _rootGrid.TransformToVisual(this).TransformPoint(new Windows.Foundation.Point(w * 0.5, h * 0.5));
            }
            catch
            {
                center = new Windows.Foundation.Point(w * 0.5, h * 0.5);
            }

            UpdateAngularDotColor(_ellipse01, _AngularDotBrush01, center);
            UpdateAngularDotColor(_ellipsePoint, _AngularDotBrushPoint, center);
            UpdateAngularDotColor(_ellipse02, _AngularDotBrush02, center);
            UpdateAngularDotColor(_ellipse03, _AngularDotBrush03, center);
        }

        private void UpdateAngularDotColor(Windows.UI.Xaml.Shapes.Shape ellipse, SolidColorBrush brush, Windows.Foundation.Point center)
        {
            if (ellipse == null || brush == null)
                return;

            double ew = (ellipse.ActualWidth > 0) ? ellipse.ActualWidth : EllipseDiameter;
            double eh = (ellipse.ActualHeight > 0) ? ellipse.ActualHeight : EllipseDiameter;

            Windows.Foundation.Point p;
            try
            {
                p = ellipse.TransformToVisual(this).TransformPoint(new Windows.Foundation.Point(ew * 0.5, eh * 0.5));
            }
            catch
            {
                p = new Windows.Foundation.Point(0, 0);
            }

            brush.Color = AngularGradientBrush.SampleColorAtPoint(p, center.X, center.Y);
        }

        private void ApplyNormalDotBrushes()
        {
            Brush normalBrush = ResolveBrushOrDefault(_ellipseIndeterminateBrush, ELLIPSE_INDETERMINATE_KEY);
            Brush pointBrush = ResolveBrushOrDefault(_variantEllipseIndeterminateBrush, VARIANT_ELLIPSE_INDETERMINATE_KEY);

            if (_ellipse01 != null) _ellipse01.Fill = normalBrush;
            if (_ellipse02 != null) _ellipse02.Fill = normalBrush;
            if (_ellipse03 != null) _ellipse03.Fill = normalBrush;

            if (_ellipsePoint != null) _ellipsePoint.Fill = pointBrush;
        }

        private static Brush ResolveBrushOrDefault(Brush brush, string defaultHex)
        {
            if (brush is SolidColorBrush scb && scb.Color == Colors.Transparent)
            {
                return ColorsHelpers.ConvertColorHex(defaultHex);
            }

            return brush ?? ColorsHelpers.ConvertColorHex(defaultHex);
        }
        #endregion
    }
}

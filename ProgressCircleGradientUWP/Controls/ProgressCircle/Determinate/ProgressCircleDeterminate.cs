using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;

using ProgressCircleGradientUWP.Controls.ProgressCircle.Determinate;
using ProgressCircleGradientUWP.Helpers;

namespace ProgressCircleGradientUWP.Controls.ProgressCircle
{
    public partial class ProgressCircleDeterminate : ProgressCircle
    {
        #region Constants
        private const double RADIANS = Math.PI / 180.0;

        private const string PART_COLOR_GRID_NAME = "PART_ColorGrid";
        private const string PART_TEXT_NAME = "PART_text";
        private const string PART_CANVAS = "PART_canvas";

        private const string PART_OUTER_PATH = "PART_OuterPath";
        private const string PART_OUTER_PATH_FIGURE = "PART_OuterPathFigure";
        private const string PART_OUTER_ARC_SEGMENT = "PART_OuterArcSegment";

        private const string PART_GRADIENT_LAYER = "PART_GradientLayer";

        private const string PART_INNER_PATH = "PART_InnerPath";
        private const string PART_INNER_PATH_FIGURE = "PART_InnerPathFigure";
        private const string PART_INNER_ARC_SEGMENT = "PART_InnerArcSegment";
        private const string PART_START_ELLIPSE = "PART_startEllipse";
        private const string PART_END_ELLIPSE = "PART_endEllipse";

        private const string RESOURCE_COLOR_ARC_OUTER = "#17171A";
        private const string RESOURCE_COLOR_ARC_INNER = "#387AFF";
        private const double CIRCLE_CENTER_TO_BORDER_CORRECTION_FACTOR = 0.98;

        private const int GRIDSIZE_XL = 45;
        private const int GRIDSIZE_LG = 30;
        private const int GRIDSIZE_MD = 24;
        private const int GRIDSIZE_SM = 12;
        private const int GRIDSIZE_ST = 8;
        #endregion

        #region Variables
        private Canvas _canvas;

        private Path _outerPath;
        private PathFigure _outerPathFigure;
        private ArcSegment _outerArc;

        private Rectangle _gradientLayer;
        private Visual _gradientVisual;
        private Compositor _compositor;
        private CompositionPathGeometry _clipPathGeometry;
        private CompositionGeometricClip _geometricClip;

        // Keep the current clip geometry alive while the compositor uses it.
        private CanvasGeometry? _clipCanvasGeometry;
        private CanvasStrokeStyle _clipStrokeStyle;
        private float _lastClipAngle = -1f;
        private float _lastClipRadius = -1f;
        private float _lastClipThickness = -1f;

        private Path _innerPath;
        private PathFigure _innerPathFigure;
        private ArcSegment _innerArc;
        private EllipseGeometry _startEllipse;
        private EllipseGeometry _endEllipse;

        private TextBlock _text;

        private readonly List<ProgressCircleDeterminateModel> _progressCircleDeterminateModels =
        [
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.XLarge, Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 30, Thickness = 10, Margin = new Thickness(7) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.Large,  Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 21, Thickness = 8,  Margin = new Thickness(5) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.Medium, Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 17, Thickness = 6,  Margin = new Thickness(4) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.Small,  Orientation = ProgressCircleIndeterminateOrientation.Horizontal, RadiusSize = 8.5, Thickness = 3, Margin = new Thickness(2) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.SmallTitle, Orientation = ProgressCircleIndeterminateOrientation.Horizontal, RadiusSize = 7, Thickness = 2, Margin = new Thickness(1.6) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate2, Size = ProgressCircleSize.SmallTitle, Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 31, Thickness = 6, Margin = new Thickness(7) },
        ];
        #endregion

        #region Dependency Properties
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(0.0, OnPercentValuePropertyChanged));

        public new Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly new DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(ColorsHelpers.ConvertHexToColor(RESOURCE_COLOR_ARC_INNER), OnForegroundPropertyChanged));

        public new Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly new DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(ColorsHelpers.ConvertHexToColor(RESOURCE_COLOR_ARC_OUTER, 10), OnBackgroundPropertyChanged));

        public ProgressCircleDeterminateType Type
        {
            get => (ProgressCircleDeterminateType)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type), typeof(ProgressCircleDeterminateType), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(ProgressCircleDeterminateType.Determinate1, OnTypePropertyChanged));

        internal double Radius
        {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register(nameof(Radius), typeof(double), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(50.0, OnRadiusOrThicknessPropertyChanged));

        internal double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(2.0, OnRadiusOrThicknessPropertyChanged));
        #endregion

        #region Constructors
        public ProgressCircleDeterminate()
        {
            DefaultStyleKey = typeof(ProgressCircleDeterminate);
            Unloaded += ProgressCircleDeterminate_Unloaded;
        }
        #endregion

        #region Override Methods
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _canvas = (Canvas)GetTemplateChild(PART_CANVAS);

            _outerPath = GetTemplateChild(PART_OUTER_PATH) as Path;
            _outerPathFigure = GetTemplateChild(PART_OUTER_PATH_FIGURE) as PathFigure;
            _outerArc = GetTemplateChild(PART_OUTER_ARC_SEGMENT) as ArcSegment;

            _gradientLayer = GetTemplateChild(PART_GRADIENT_LAYER) as Rectangle;

            _innerPath = GetTemplateChild(PART_INNER_PATH) as Path;
            _innerPathFigure = GetTemplateChild(PART_INNER_PATH_FIGURE) as PathFigure;
            _innerArc = GetTemplateChild(PART_INNER_ARC_SEGMENT) as ArcSegment;
            _startEllipse = GetTemplateChild(PART_START_ELLIPSE) as EllipseGeometry;
            _endEllipse = GetTemplateChild(PART_END_ELLIPSE) as EllipseGeometry;
            _text = (TextBlock)GetTemplateChild(PART_TEXT_NAME);

            // Reset cached clip so the first Draw() always builds a correct path for the new template instance.
            _lastClipAngle = _lastClipRadius = _lastClipThickness = -1f;

            EnsureGradientClip();
            SetControlSize();
            Draw();

            if (_outerPath != null) _outerPath.Stroke = Background;
            if (_innerPath != null) _innerPath.Stroke = Foreground;
            if (_gradientLayer != null) _gradientLayer.Fill = Foreground;
        }

        protected override void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateSize();
        }
        #endregion

        #region Event Handlers
        private static void OnRadiusOrThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConfigureProgress(d);
        }

        private static void OnPercentValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConfigureProgress(d);
        }

        private static void ConfigureProgress(DependencyObject d)
        {
            if (d is not ProgressCircleDeterminate control) return;
            control.SetControlSize();
            control.Draw();
        }

        private static void OnForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ProgressCircleDeterminate pc) return;

            if (pc._gradientLayer != null)
                pc._gradientLayer.Fill = (Brush)e.NewValue;

            if (pc._innerPath != null)
                pc._innerPath.Stroke = (Brush)e.NewValue;
        }

        private static void OnBackgroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ProgressCircleDeterminate pc) return;

            if (pc._outerPath != null)
                pc._outerPath.Stroke = (Brush)e.NewValue;
        }

        private static void OnTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleDeterminate pc)
                pc.UpdateSize();
        }

        private void ProgressCircleDeterminate_Unloaded(object sender, RoutedEventArgs e)
        {
            DisposeClipGeometry();
        }
        #endregion

        #region Private Methods
        private void EnsureGradientClip()
        {
            if (_gradientLayer == null)
                return;

            if (_gradientVisual == null)
                _gradientVisual = ElementCompositionPreview.GetElementVisual(_gradientLayer);

            _compositor ??= _gradientVisual.Compositor;
            _clipPathGeometry ??= _compositor.CreatePathGeometry();
            _geometricClip ??= _compositor.CreateGeometricClip(_clipPathGeometry);

            _gradientVisual.Clip = _geometricClip;
        }

        private void DisposeClipGeometry()
        {
            try
            {
                _clipCanvasGeometry?.Dispose();
            }
            catch
            {
                // ignore; we don't want teardown to crash the app
            }
            finally
            {
                _clipCanvasGeometry = null;
            }
        }

        private void Draw()
        {
            if (_canvas == null)
                return;

            var center = GetCenterPoint();
            var angle = GetAngle();

            UpdateTrackGeometry(center, Radius);

            // If the template contains PART_GradientLayer, we render the "progress" via:
            // (1) fill the rectangle with Foreground (e.g. AngularGradientBrush),
            // (2) clip it to the arc segment that corresponds to Value.
            if (_gradientLayer != null)
            {
                UpdateGradientClip(center, Radius, angle);

                if (_innerPath != null)
                    _innerPath.Visibility = Visibility.Collapsed;

                return;
            }

            // Fallback for templates without gradient layer
            if (_innerPath == null || _innerPathFigure == null || _innerArc == null)
                return;

            _innerPath.Visibility = (Value <= 0 || angle <= 0.001) ? Visibility.Collapsed : Visibility.Visible;
            UpdateLegacyInnerArc(center, Radius, angle);
        }

        private void SetControlSize()
        {
            if (_canvas == null)
                return;

            double side = Radius * 2 + Thickness;

            _canvas.Width = side;
            _canvas.Height = side;

            if (_gradientLayer != null)
            {
                _gradientLayer.Width = side;
                _gradientLayer.Height = side;
            }

            if (_outerPath != null)
            {
                _outerPath.Width = side;
                _outerPath.Height = side;
                _outerPath.StrokeThickness = Thickness;
            }

            if (_innerPath != null)
            {
                _innerPath.Width = side;
                _innerPath.Height = side;
                _innerPath.StrokeThickness = Thickness;
            }
        }

        private Point GetCenterPoint()
        {
            return new Point(Radius + Thickness / 2, Radius + Thickness / 2);
        }

        private double GetAngle()
        {
            double v = Math.Clamp(Value, 0, 100);

            if (v <= 0)
                return 0;

            if (v >= 100)
                return 360.0;

            double angle = v * CIRCLE_CENTER_TO_BORDER_CORRECTION_FACTOR / 100.0 * 360.0;
            return Math.Min(angle, 359.999);
        }

        private void UpdateTrackGeometry(Point centerPoint, double radius)
        {
            if (_outerPath == null || _outerPathFigure == null || _outerArc == null)
                return;

            var circleStart = new Point(centerPoint.X, centerPoint.Y - radius);

            _outerPathFigure.StartPoint = circleStart;
            _outerPathFigure.IsClosed = false;

            _outerArc.IsLargeArc = true;
            _outerArc.Point = ScaleUnitCirclePoint(centerPoint, 359.999, radius);
            _outerArc.Size = new Size(radius, radius);
            _outerArc.SweepDirection = SweepDirection.Clockwise;
        }

        private void UpdateGradientClip(Point centerPoint, double radius, double angle)
        {
            EnsureGradientClip();
            if (_clipPathGeometry == null || _gradientLayer == null)
                return;

            if (Value <= 0 || angle <= 0.001)
            {
                _gradientLayer.Visibility = Visibility.Collapsed;
                _clipPathGeometry.Path = null;

                DisposeClipGeometry();
                _lastClipAngle = _lastClipRadius = _lastClipThickness = -1f;
                return;
            }

            _gradientLayer.Visibility = Visibility.Visible;

            float angleF = (float)angle;
            float radiusF = (float)radius;
            float thicknessF = (float)Thickness;

            // Avoid re-allocating COM geometries if nothing changed.
            if (_clipCanvasGeometry != null &&
                Math.Abs(angleF - _lastClipAngle) < 0.0001f &&
                Math.Abs(radiusF - _lastClipRadius) < 0.0001f &&
                Math.Abs(thicknessF - _lastClipThickness) < 0.0001f)
            {
                return;
            }

            _lastClipAngle = angleF;
            _lastClipRadius = radiusF;
            _lastClipThickness = thicknessF;

            DisposeClipGeometry();

            var device = CanvasDevice.GetSharedDevice();

            // Build center-line geometry (open arc), then "Stroke" it to get a filled outline for Composition clipping.
            CanvasGeometry? centerLine = null;

            try
            {
                if (angle >= 359.999)
                {
                    // Full circle case (Value ~ 100): use a true circle so there is no end-cap discontinuity.
                    centerLine = CanvasGeometry.CreateCircle(device, ToVector2(centerPoint), radiusF);
                }
                else
                {
                    var start = new Point(centerPoint.X, centerPoint.Y - radius);
                    var end = ScaleUnitCirclePoint(centerPoint, angle, radius);

                    using (var pb = new CanvasPathBuilder(device))
                    {
                        pb.BeginFigure(ToVector2(start));
                        pb.AddArc(
                            ToVector2(end),
                            radiusF,
                            radiusF,
                            0f,
                            CanvasSweepDirection.Clockwise,
                            angle > 180.0 ? CanvasArcSize.Large : CanvasArcSize.Small);
                        pb.EndFigure(CanvasFigureLoop.Open);

                        centerLine = CanvasGeometry.CreatePath(pb);
                    }
                }

                _clipStrokeStyle ??= new CanvasStrokeStyle
                {
                    StartCap = CanvasCapStyle.Round,
                    EndCap = CanvasCapStyle.Round,
                    DashCap = CanvasCapStyle.Round,
                    LineJoin = CanvasLineJoin.Round,
                };

                _clipCanvasGeometry = centerLine.Stroke(thicknessF, _clipStrokeStyle);
                _clipPathGeometry.Path = new CompositionPath(_clipCanvasGeometry);
            }
            finally
            {
                // centerLine is only an intermediate geometry; release it.
                centerLine?.Dispose();
            }
        }

        private void UpdateLegacyInnerArc(Point centerPoint, double radius, double angle)
        {
            var circleStart = new Point(centerPoint.X, centerPoint.Y - radius);

            _innerPathFigure.StartPoint = circleStart;
            _innerPathFigure.IsClosed = false;

            _innerArc.IsLargeArc = angle > 180.0;
            _innerArc.Point = ScaleUnitCirclePoint(centerPoint, angle, radius);
            _innerArc.Size = new Size(radius, radius);
            _innerArc.SweepDirection = SweepDirection.Clockwise;

            // If you keep the ellipse geometries in the template, make them work as round end-caps.
            double cap = Math.Max(0.0, Thickness / 2.0);

            if (_startEllipse != null)
            {
                _startEllipse.Center = circleStart;
                _startEllipse.RadiusX = cap;
                _startEllipse.RadiusY = cap;
            }

            if (_endEllipse != null)
            {
                _endEllipse.Center = _innerArc.Point;
                _endEllipse.RadiusX = cap;
                _endEllipse.RadiusY = cap;
            }

            // Optional: make the path itself round-capped as well (doesn't hurt).
            if (_innerPath != null)
            {
                _innerPath.StrokeStartLineCap = PenLineCap.Round;
                _innerPath.StrokeEndLineCap = PenLineCap.Round;
                _innerPath.StrokeLineJoin = PenLineJoin.Round;
            }
        }

        private static Point ScaleUnitCirclePoint(Point origin, double angle, double radius)
        {
            return new Point(
                origin.X + Math.Sin(RADIANS * angle) * radius,
                origin.Y - Math.Cos(RADIANS * angle) * radius);
        }

        private static Vector2 ToVector2(Point p) => new((float)p.X, (float)p.Y);

        private void UpdateSize()
        {
            ProgressCircleDeterminateModel progress;

            if (Type == ProgressCircleDeterminateType.Determinate2)
                progress = _progressCircleDeterminateModels.FirstOrDefault(x => x.Type == Type);
            else
                progress = _progressCircleDeterminateModels.FirstOrDefault(x => x.Type == Type && x.Size == Size);

            if (progress == null)
                return;

            Radius = progress.RadiusSize;
            Thickness = progress.Thickness;

            SetCanvasMargin(progress.Margin);
            SetTextAlignment(progress.Orientation);
            UpdateFontSizeMessage(progress);

            SetControlSize();
            Draw();
        }

        private void UpdateFontSizeMessage(ProgressCircleDeterminateModel progressDefinition)
        {
            if (_text == null)
                return;

            if (progressDefinition.Type == ProgressCircleDeterminateType.Determinate2)
            {
                _text.FontSize = 13;
                return;
            }

            _text.FontSize = progressDefinition.RadiusSize switch
            {
                GRIDSIZE_XL => 13,
                GRIDSIZE_LG => 13,
                GRIDSIZE_MD => 12,
                GRIDSIZE_SM => 11,
                GRIDSIZE_ST => 11,
                _ => 13
            };
        }

        private void SetCanvasMargin(Thickness margin)
        {
            if (_canvas != null)
                _canvas.Margin = margin;
        }

        private void SetTextAlignment(ProgressCircleIndeterminateOrientation orientation)
        {
            if (_progressCircleTextAlingmentDictionary.TryGetValue(orientation, out string alignment))
                VisualStateManager.GoToState(this, alignment, true);
        }
        #endregion
    }
}

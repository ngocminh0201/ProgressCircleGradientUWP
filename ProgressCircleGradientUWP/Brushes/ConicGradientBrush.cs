using System;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;

namespace ProgressCircleGradient.Brushes
{
    public sealed partial class ConicGradientBrush : XamlCompositionBrushBase
    {
        private const int FixedResolution = 2048;

        private const float InitialAngleOffset = -46.2f;

        private Compositor? _compositor;
        private CompositionGraphicsDevice? _graphicsDevice;
        private CompositionDrawingSurface? _surface;
        private CompositionSurfaceBrush? _surfaceBrush;

        private struct Stop(float angleDeg, byte a, byte r, byte g, byte b)
        {
            public float AngleDeg = angleDeg;
            public byte A = a, R = r, G = g, B = b;
        }

        private static readonly Stop[] Stops =
        [
            new(25.2f, 0x99, 0x38, 0x7A, 0xFF),
            new(72.0f, 0xE6, 0x3C, 0xB9, 0xA2),
            new(136.8f, 0xE6, 0x3D, 0xCC, 0x87),
            new(208.8f, 0xE6, 0x38, 0x7A, 0xFF),
            new(306.0f, 0x99, 0x3B, 0xA3, 0xC3),
            new(345.6f, 0x99, 0x3D, 0xCC, 0x87),
        ];

        protected override void OnConnected()
        {
            if (CompositionBrush != null)
                return;

            _compositor = GetCompositorForUwp();
            if (_compositor == null)
                return;

            var canvasDevice = CanvasDevice.GetSharedDevice();
            _graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, canvasDevice);

            CreateSurface();
            Redraw();

            _surfaceBrush = _compositor.CreateSurfaceBrush(_surface);
            _surfaceBrush.Stretch = CompositionStretch.Fill;

            CompositionBrush = _surfaceBrush;
        }

        protected override void OnDisconnected()
        {
            CompositionBrush = null;

            if (_surfaceBrush != null)
            {
                _surfaceBrush.Dispose();
                _surfaceBrush = null;
            }

            _surface = null;

            if (_graphicsDevice != null)
            {
                _graphicsDevice.Dispose();
                _graphicsDevice = null;
            }

            _compositor = null;
        }

        private static Compositor GetCompositorForUwp()
        {
            // UWP có Window.Current.Compositor (đúng chuẩn cho XamlCompositionBrushBase)
            // :contentReference[oaicite:2]{index=2}
            var w = Window.Current;
            if (w != null)
                return w.Compositor;

            // Fallback (hiếm khi cần)
            var root = w?.Content as UIElement;
            if (root != null)
                return ElementCompositionPreview.GetElementVisual(root).Compositor;

            return null;
        }

        private void CreateSurface()
        {
            _surface = _graphicsDevice.CreateDrawingSurface(
                new Size(FixedResolution, FixedResolution),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);
        }

        private void Redraw()
        {
            if (_surface == null)
                return;

            byte[] bytes = BuildConicGradientPixelsPremultipliedBGRA(FixedResolution, FixedResolution);

            var device = CanvasDevice.GetSharedDevice();

            using var bitmap = CanvasBitmap.CreateFromBytes(
                device,
                bytes,
                FixedResolution,
                FixedResolution,
                DirectXPixelFormat.B8G8R8A8UIntNormalized);
            using var ds = CanvasComposition.CreateDrawingSession(_surface);
            ds.Clear(Colors.Transparent);
            ds.DrawImage(bitmap);
        }

        /// <summary>
        /// Sample màu tại 1 điểm (phục vụ logic “xuyên xuống brush” / hit-test / mapping value->màu).
        /// </summary>
        public static Color SampleColorAtPoint(Point point, double centerX, double centerY)
        {
            float px = (float)(point.X - centerX);
            float py = (float)(point.Y - centerY);

            float rad = (float)Math.Atan2(-px, py);
            if (rad < 0f) rad += (float)(Math.PI * 2.0);

            float angleDeg = rad * (180f / (float)Math.PI);
            angleDeg = Mod360(angleDeg + InitialAngleOffset);

            EvaluateColorAtAnglePremultiplied(angleDeg, out byte a, out byte rP, out byte gP, out byte bP);

            if (a == 0)
                return Colors.Transparent;

            float af = a / 255f;

            // un-premultiply
            byte r = ClampToByte((int)Math.Round(rP / af));
            byte g = ClampToByte((int)Math.Round(gP / af));
            byte b = ClampToByte((int)Math.Round(bP / af));

            return Color.FromArgb(a, r, g, b);
        }

        private static byte[] BuildConicGradientPixelsPremultipliedBGRA(int width, int height)
        {
            var buffer = new byte[width * height * 4];

            float cx = width * 0.5f;
            float cy = height * 0.5f;

            int idx = 0;
            for (int y = 0; y < height; y++)
            {
                float py = (y + 0.5f) - cy;

                for (int x = 0; x < width; x++)
                {
                    float px = (x + 0.5f) - cx;

                    float rad = (float)Math.Atan2(-px, py);
                    if (rad < 0f) rad += (float)(Math.PI * 2.0);

                    float angleDeg = rad * (180f / (float)Math.PI);
                    angleDeg = Mod360(angleDeg + InitialAngleOffset);

                    byte a, rP, gP, bP;
                    EvaluateColorAtAnglePremultiplied(angleDeg, out a, out rP, out gP, out bP);

                    // BGRA premultiplied
                    buffer[idx++] = bP;
                    buffer[idx++] = gP;
                    buffer[idx++] = rP;
                    buffer[idx++] = a;
                }
            }

            return buffer;
        }

        private static void EvaluateColorAtAnglePremultiplied(float angleDeg, out byte aOut, out byte rPOut, out byte gPOut, out byte bPOut)
        {
            angleDeg = Mod360(angleDeg);

            Stop prev, next;
            float prevAngle, nextAngle;

            // wrap-around
            if (angleDeg < Stops[0].AngleDeg)
            {
                prev = Stops[Stops.Length - 1];
                next = Stops[0];
                prevAngle = prev.AngleDeg - 360f;
                nextAngle = next.AngleDeg;
            }
            else if (angleDeg >= Stops[Stops.Length - 1].AngleDeg)
            {
                prev = Stops[Stops.Length - 1];
                next = Stops[0];
                prevAngle = prev.AngleDeg;
                nextAngle = next.AngleDeg + 360f;
            }
            else
            {
                int i = 0;
                for (; i < Stops.Length - 1; i++)
                {
                    if (Stops[i].AngleDeg <= angleDeg && angleDeg < Stops[i + 1].AngleDeg)
                        break;
                }
                prev = Stops[i];
                next = Stops[i + 1];
                prevAngle = prev.AngleDeg;
                nextAngle = next.AngleDeg;
            }

            float t = (angleDeg - prevAngle) / (nextAngle - prevAngle);
            t = Clamp01(t);

            float ap0 = prev.A / 255f;
            float ap1 = next.A / 255f;

            // premultiply
            float r0 = (prev.R / 255f) * ap0;
            float g0 = (prev.G / 255f) * ap0;
            float b0 = (prev.B / 255f) * ap0;

            float r1 = (next.R / 255f) * ap1;
            float g1 = (next.G / 255f) * ap1;
            float b1 = (next.B / 255f) * ap1;

            float a = Lerp(ap0, ap1, t);
            float rP = Lerp(r0, r1, t);
            float gP = Lerp(g0, g1, t);
            float bP = Lerp(b0, b1, t);

            aOut = ClampToByte((int)Math.Round(a * 255f));
            rPOut = ClampToByte((int)Math.Round(rP * 255f));
            gPOut = ClampToByte((int)Math.Round(gP * 255f));
            bPOut = ClampToByte((int)Math.Round(bP * 255f));
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        private static byte ClampToByte(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)v;
        }

        private static float Mod360(float deg)
        {
            deg %= 360f;
            if (deg < 0f) deg += 360f;
            return deg;
        }
    }
}

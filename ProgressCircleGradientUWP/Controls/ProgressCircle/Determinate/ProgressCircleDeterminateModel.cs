using Windows.UI.Xaml;
using ProgressCircleGradientUWP.Controls.ProgressCircle;

namespace ProgressCircleGradientUWP.Controls.ProgressCircle.Determinate
{
    internal class ProgressCircleDeterminateModel
    {
        public ProgressCircleDeterminateType Type { get; set; }
        public ProgressCircleIndeterminateOrientation Orientation { get; set; }
        public ProgressCircleSize Size { get; set; }
        public double RadiusSize { get; set; }
        public double Thickness { get; set; }
        public Thickness Margin { get; set; }
    }
}

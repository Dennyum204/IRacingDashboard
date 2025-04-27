using IRacingDashboard.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Resources.Converters
{
    public class TrackPointsToPolylineConverter : IValueConverter
    {
        private const double CanvasWidth = 250;
        private const double CanvasHeight = 250;
        private double XEnhanceFactor = 10;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IEnumerable<TrackPoint> points) return null;
            if (!points.Any()) return null;

            var polyPoints = new PointCollection();

            double minX = points.Min(p => p.X);
            double maxX = points.Max(p => p.X);
            double minZ = points.Min(p => p.Z);
            double maxZ = points.Max(p => p.Z);

            double trackWidth = (maxX - minX) * XEnhanceFactor; // 🔥 Enhance width
            double trackHeight = maxZ - minZ;

            double scaleX = CanvasWidth / trackWidth;
            double scaleY = CanvasHeight / trackHeight;
            double finalScale = Math.Min(scaleX, scaleY) * 0.9;

            double offsetX = (CanvasWidth - (maxX - minX) * finalScale * XEnhanceFactor) / 2;
            double offsetY = (CanvasHeight - trackHeight * finalScale) / 2;

            foreach (var pt in points)
            {
                double scaledX = (pt.X - minX) * finalScale * XEnhanceFactor + offsetX;
                double scaledY = (pt.Z - minZ) * finalScale + offsetY;

                scaledY = CanvasHeight - scaledY; // Invert Y axis

                polyPoints.Add(new Point(scaledX, scaledY));
            }

            return polyPoints;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using IRacingDashboard.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Resources.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using IRacingDashboard.Models;

    public class LapYawToPolylineConverter : IValueConverter
    {
        private const double CanvasWidth = 250;
        private const double CanvasHeight = 250;
        private const double TrackLength = 5000; // estimated real track length in meters

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IEnumerable<TrackPoint> points || !points.Any())
                return null;

            var pointList = points.ToList();
            double x = 0, y = 0;
            var path = new List<Point> { new Point(x, y) };

            for (int i = 1; i < pointList.Count; i++)
            {
                double pctDelta = pointList[i].X - pointList[i - 1].X;
                if (pctDelta < 0) continue; // skip if lap restarted

                double distance = pctDelta * TrackLength;
                double yawRad = pointList[i].Z;

                x += distance * Math.Cos(yawRad);
                y += distance * Math.Sin(yawRad);

                path.Add(new Point(x, y));
            }

            // Center and scale the track to fit the canvas
            double minX = path.Min(p => p.X);
            double maxX = path.Max(p => p.X);
            double minY = path.Min(p => p.Y);
            double maxY = path.Max(p => p.Y);

            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;
            double width = maxX - minX;
            double height = maxY - minY;
            double scale = 0.9 * Math.Min(CanvasWidth / width, CanvasHeight / height);

            var polyPoints = new PointCollection();

            foreach (var pt in path)
            {
                double screenX = CanvasWidth / 2 - (pt.X - centerX) * scale; // 🔁 flipped X
                double screenY = CanvasHeight / 2 + (pt.Y - centerY) * scale;

                polyPoints.Add(new Point(screenX, screenY));
            }

            return polyPoints;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


}

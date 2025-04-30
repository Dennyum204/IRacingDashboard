using IRacingDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace IRacingDashboard.Models
{
    public class CarViewModel : BaseViewModel
    {
        public int Position { get; set; } // 1 = first, etc.
        public string DriverName { get; set; }
        public Point3D Position3D { get; set; }

        public GeometryModel3D CarModel { get; set; }
    }
    public class TrackPoint
    {
        public double X { get; set; }
        public double Z { get; set; }
    }
    public class LapTimeEntry
    {
        public int LapNumber { get; set; }
        public double LapTimeSeconds { get; set; }
        public bool IsValidLap { get; set; }


        public double DeltaToBest { get; set; } // in seconds

        public string DeltaToBestFormatted => !IsValidLap
            ? "-"
            : DeltaToBest == 0
                ? "Best Lap"
                : $"{(DeltaToBest > 0 ? "+" : "")}{DeltaToBest:F3}s";

        public string LapTimeFormatted => FormatLapTime(LapTimeSeconds);
        private string FormatLapTime(double seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{time.Minutes}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
        }
    }


    public class DashboardItem
    {
        public string Name { get; set; }
        public Type ViewModelType { get; set; }
    }
}

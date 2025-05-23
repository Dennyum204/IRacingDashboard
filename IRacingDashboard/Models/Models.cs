﻿using IRacingDashboard.Enums;
using IRacingDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace IRacingDashboard.Models
{

    public class UIRenderedSector
    {
        public int SectorNumber { get; set; }       // 1-based for user readability
        public double DeltaToBest { get; set; }

        public string DeltaFormatted
        {
            get
            {
                if (double.IsNaN(DeltaToBest)) return "--";
                return DeltaToBest switch
                {
                    > 0 => $"+{DeltaToBest:F2}",
                    < 0 => $"{DeltaToBest:F2}",
                    _ => "±0.00"
                };
            }
        }
        public Brush BackgroundColor =>
            DeltaToBest > 0 ? Brushes.Goldenrod : DeltaToBest < 0 ? Brushes.MediumPurple : Brushes.Gray;
        public SectorState State { get; set; }
    }

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

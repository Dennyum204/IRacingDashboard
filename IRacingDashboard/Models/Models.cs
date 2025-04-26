using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRacingDashboard.Models
{
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
}

using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using IRacingDashboard.Services;
using IRacingDashboard.Models;
using irsdkSharp.Serialization.Enums.Fastest;
using irsdkSharp.Serialization.Models.Fastest;

namespace IRacingDashboard.ViewModels
{
    class TelemetryDashboardViewModel : BaseViewModel
    {
        private readonly TelemetryService _telemetryService;

        public ObservableCollection<ISeries> Series { get; set; }
        public List<Axis> XAxes { get; set; }
        public List<Axis> YAxes { get; set; }

        private ObservableCollection<ObservablePoint> _throttlePoints = new();
        private ObservableCollection<ObservablePoint> _brakePoints = new();

       
        private double _lastLapTime = 0;
    
        public ObservableCollection<LapTimeEntry> LapTimes { get; set; } = new();


        public TelemetryDashboardViewModel()
        {

            _telemetryService = TelemetryService.Instance; // 🔥 Singleton access

            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservablePoint>
                {
                    Name = "Throttle",
                    Values = _throttlePoints,
                    GeometrySize = 0,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.Blue, 1), // 🔥 thickness 3
                    LineSmoothness = 0 // 🔥 straight lines,

                },
                new LineSeries<ObservablePoint>
                {
                    Name = "Brake",
                    Fill = null,
                    Values = _brakePoints,
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(SKColors.Red, 1), // 🔥 thickness 3
                    LineSmoothness = 0 // 🔥 straight lines
                }
            };

            XAxes = new List<Axis>
            {
                new Axis
                {
                    Name = "Time (s)",
                    Labeler = FormatSecondsAsMinutes, // 🔥 HERE
                    MinLimit = 0, // start from 0
                    MaxLimit = 10, // initial (small) — we'll update it dynamically
                    LabelsPaint = new SolidColorPaint(SKColors.White) // 🔥 set label color to white

                }
            };
            YAxes = new List<Axis> { new Axis { Name = "Input", 
                MinLimit = 0, 
                MaxLimit = 1,
                ShowSeparatorLines = true,
                SeparatorsPaint = null,
                Labeler = value => value == 0 ? "0" : value == 1 ? "1" : "",
                LabelsPaint = new SolidColorPaint(SKColors.White) // 🔥 set label color to white

            } };


            _telemetryService.TelemetryUpdated += OnTelemetryUpdated;
            _telemetryService.Start();

        }

        private static string FormatSecondsAsMinutes(double seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{time.Minutes}:{time.Seconds:D2}";
        }

        private void OnTelemetryUpdated(Data telemetry)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                double lapTime = telemetry.LapCurrentLapTime;

                // 🔥 Detect new lap
                if (lapTime < _lastLapTime)
                {
                    _throttlePoints.Clear();
                    _brakePoints.Clear();
                    XAxes[0].MaxLimit = 10; // or 5 if you want to start small and grow again
                }

                _lastLapTime = lapTime;

                _throttlePoints.Add(new ObservablePoint(lapTime, telemetry.ThrottleRaw));
                _brakePoints.Add(new ObservablePoint(lapTime, 1.0 - telemetry.BrakeRaw));

                // 🔁 Extend X axis if needed
                if (lapTime > (XAxes[0].MaxLimit - 1))
                {
                    XAxes[0].MaxLimit = lapTime + 1;
                }
              
            });
        }

      





    }
}

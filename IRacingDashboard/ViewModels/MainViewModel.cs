using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using IRacingDashboard.Services;
using irsdkSharp.Serialization.Models.Fastest;
using System.Collections.Generic;
using System.Windows.Media;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;
using IRacingDashboard.Models;
using irsdkSharp.Serialization.Enums.Fastest; // ✅ for ObservablePoint

namespace IRacingDashboard.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly TelemetryService _telemetryService;
        public string ConnectionStatus => IsConnected ? "Connected to iRacing" : "Waiting for iRacing...";
        public Brush ConnectionColor => IsConnected ? Brushes.Green : Brushes.Red;
        public ObservableCollection<ISeries> Series { get; set; }
        public List<Axis> XAxes { get; set; }
        public List<Axis> YAxes { get; set; }

        private ObservableCollection<ObservablePoint> _throttlePoints = new();
        private ObservableCollection<ObservablePoint> _brakePoints = new();
        private double? _lapStartTime = null;
        private double _lastLapTime = 0;
        private int _lastLapCompleted = -1;
        public ObservableCollection<LapTimeEntry> LapTimes { get; set; } = new();

        private int _time = 0;
        private int _lastLapLogged = -1;

        private int _lastLap = -1;
        bool lapUpdated = false;



        private int _lastIncidentCount = 0;
        private bool _wentOffTrackDuringLap = false;
        public MainViewModel()
        {
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
                    MaxLimit = 10 // initial (small) — we'll update it dynamically
                }
            };
            YAxes = new List<Axis> { new Axis { Name = "Input", MinLimit = 0, MaxLimit = 1 } };

            _telemetryService = new TelemetryService();
            _telemetryService.TelemetryUpdated += OnTelemetryUpdated;
            _telemetryService.ConnectionChanged += OnConnectionChanged; // ✅ Listen for connection changes
            _telemetryService.Start();
        }
        private static string FormatSecondsAsMinutes(double seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{time.Minutes}:{time.Seconds:D2}";
        }
        private void OnConnectionChanged(bool connected)
        {
            IsConnected = connected;
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


                // 🛑 Track if player goes off track during the lap
                if ((TrackSurface)telemetry.PlayerTrackSurface == TrackSurface.OffTrack)
                {
                    _wentOffTrackDuringLap = true;
                }

                //Laps
                // ✅ Detect new lap by Lap number increasing
                if (telemetry.Lap > _lastLapLogged)
                {
                    _ = TryAddLapEntryAsync(telemetry); // 🔥 launch safe delayed check
                }
            });
        }

        private async Task TryAddLapEntryAsync(Data telemetry)
        {
            await Task.Delay(1000); // 🔥 wait a little bit to be safe

            App.Current.Dispatcher.Invoke(() =>
            {
                if (telemetry.Lap > _lastLapLogged )
                {
                    double lapTime = telemetry.LapLastLapTime;

                    bool incidentIncreased = telemetry.PlayerCarMyIncidentCount > _lastIncidentCount;
                    bool isValidLap = !incidentIncreased && !_wentOffTrackDuringLap && lapTime > 0; ;

                    double deltaToBest = (telemetry.LapBestLapTime > 0 && lapTime>0)
                        ? lapTime - telemetry.LapBestLapTime
                        : 0;

                    LapTimes.Insert(0, new LapTimeEntry
                    {
                        LapNumber = telemetry.Lap - 1,
                        LapTimeSeconds = lapTime,
                        IsValidLap = isValidLap,
                        DeltaToBest = deltaToBest
                    });

                    _lastLapLogged = telemetry.Lap;
                    _lastIncidentCount = telemetry.PlayerCarMyIncidentCount;
                    _wentOffTrackDuringLap = false; // 🔥 reset tracking for next lap
                }
            });
        }

        #region Properties
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); 
                OnPropertyChanged(nameof(ConnectionStatus));
                OnPropertyChanged(nameof(ConnectionColor));
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

using Adapters;
using IRacingDashboard.Models;
using IRacingDashboard.Services;
using irsdkSharp.Serialization.Models.Fastest;
using irsdkSharp.Serialization.Models.Session;
using irsdkSharp.Serialization.Models.Session.WeekendInfo;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;

namespace IRacingDashboard.ViewModels
{
    public class TrackMapDashboardViewModel : BaseViewModel
    {
        private readonly TelemetryService _telemetryService;

        private double _ghostX = 0;
        private double _ghostZ = 0;
        private double _ghostHeading = 0; // 0 = facing up (Z+)
        private DateTime _lastUpdateTime = DateTime.UtcNow;
        private bool _isRecording = false;
        private int _startingLap = -1;
        string _trackName = "";
        private bool _recordedOneLap = false;


    
        public TrackMapDashboardViewModel()
        {
            _telemetryService = TelemetryService.Instance;
            _telemetryService.TelemetryUpdated += OnTelemetryUpdated;
            _telemetryService.WeekendInfoUpdated += _telemetryService_WeekendInfoUpdated; ;
            _telemetryService.Start();

        }

        private void _telemetryService_WeekendInfoUpdated(IRacingSessionModel sessionInfo)
        {
            if (!string.IsNullOrEmpty(_trackName)) return;

            _trackName = sessionInfo.WeekendInfo.TrackName;
            TryLoadTrack();
        }
        private bool TryLoadTrack()
        {
            if (string.IsNullOrEmpty(_trackName)) return false;

            string filename = $"{_trackName}.json";

            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                var loadedPoints = JsonConvert.DeserializeObject<ObservableCollection<TrackPoint>>(json);

                if (loadedPoints != null && loadedPoints.Count > 0)
                {
                    RecordedPoints = loadedPoints; // 🔥 triggers PropertyChanged now
                    _recordedOneLap = true;
                    DrawRecordedTrack();
                    return true;
                }
            }

            return false;
        }

        private void StartRecording()
        {
            RecordedPoints.Clear();
            _ghostX = 0;
            _ghostZ = 0;
            _ghostHeading = 0;
            _lastUpdateTime = DateTime.UtcNow;
            _isRecording = true;
        }

        private void StopRecording()
        {
            _isRecording = false;
            _recordedOneLap = true;
            DrawRecordedTrack();
            SaveToJson();

        }

        private void DrawRecordedTrack()
        {
           // throw new NotImplementedException();
        }

        private void SaveToJson()
        {
      
            var json = JsonConvert.SerializeObject(RecordedPoints, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_trackName+".json", json);
        }

        private ObservableCollection<TrackPoint> _recordedPoints = new();
        public ObservableCollection<TrackPoint> RecordedPoints
        {
            get => _recordedPoints;
            set
            {
                _recordedPoints = value;
                OnPropertyChanged(nameof(RecordedPoints)); // 🔥 trigger UI refresh
            }
        }
        private double _lastLapDistPct = -1;

        private void OnTelemetryUpdated(Data telemetry)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                double steering = telemetry.SteeringWheelAngle;
                double lapDistPct = telemetry.LapDistPct;
                int currentLap = telemetry.Lap;

                if (_startingLap == -1)
                {
                    _startingLap = currentLap;
                    _lastLapDistPct = lapDistPct;
                    return;
                }

                if (!_isRecording && !_recordedOneLap && currentLap > _startingLap)
                {
                    StartRecording();
                }

                if (_isRecording && currentLap > _startingLap + 1)
                {
                    StopRecording();
                }

                if (_isRecording)
                {
                    double deltaLapDist = lapDistPct - _lastLapDistPct;
                    _lastLapDistPct = lapDistPct;

                    if (deltaLapDist < 0) return; // Skip if LapDistPct reset

                    double forwardDistance = deltaLapDist * 5000; // 🔥 5000 = total arbitrary "track units"

                    // Apply a small base curve to naturally close track
                    double baseTurnRate = 360.0 / 500.0; // 360 degrees per 5000 units
                    double steeringSensitivity = 0.05;

                    _ghostHeading += baseTurnRate + steering * steeringSensitivity * forwardDistance;
                    double headingRad = _ghostHeading * Math.PI / 180.0;

                    _ghostX += forwardDistance * Math.Sin(headingRad);
                    _ghostZ += forwardDistance * Math.Cos(headingRad);

                    if (RecordedPoints.Count == 0 ||
                        GetDistance(RecordedPoints[^1], _ghostX, _ghostZ) > 2)
                    {
                        RecordedPoints.Add(new TrackPoint { X = lapDistPct, Z = steering });
                    }
                }
            });
        }

        private double GetDistance(TrackPoint last, double x, double z)
        {
            double dx = last.X - x;
            double dz = last.Z - z;
            return Math.Sqrt(dx * dx + dz * dz);
        }
    }
}

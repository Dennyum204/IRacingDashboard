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
        public ObservableCollection<TrackPoint> RecordedPoints { get; set; } = new();

    
        public TrackMapDashboardViewModel()
        {
            _telemetryService = TelemetryService.Instance;
            _telemetryService.TelemetryUpdated += OnTelemetryUpdated;
            _telemetryService.WeekendInfoUpdated += _telemetryService_WeekendInfoUpdated; ;


        }

        private void _telemetryService_WeekendInfoUpdated(IRacingSessionModel sessionInfo)
        {
            if (!string.IsNullOrEmpty(_trackName)) return;

            _trackName = sessionInfo.WeekendInfo.TrackName;
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
            SaveToJson();

        }
        private void SaveToJson()
        {
      
            var json = JsonConvert.SerializeObject(RecordedPoints, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_trackName+".json", json);
        }



        private void OnTelemetryUpdated(Data telemetry)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var now = DateTime.UtcNow;
                var deltaTime = (now - _lastUpdateTime).TotalSeconds;
                _lastUpdateTime = now;
                
                double speed = telemetry.Speed;
                double steering = telemetry.SteeringWheelAngle;
                int currentLap = telemetry.Lap;

                if (_startingLap == -1)
                {
                    _startingLap = currentLap; // 🔥 remember starting lap
                    return; // don't record yet
                }

                if (!_isRecording && currentLap > _startingLap)
                {
                    StartRecording();
                }

                if (_isRecording && currentLap > _startingLap + 1)
                {
                    StopRecording();
                }

                if (_isRecording)
                {
                    double distanceMoved = speed * deltaTime;
                    _ghostHeading += steering * deltaTime * 0.5;
                    double headingRad = _ghostHeading * Math.PI / 180.0;

                    _ghostX += distanceMoved * Math.Sin(headingRad);
                    _ghostZ += distanceMoved * Math.Cos(headingRad);

                    if (RecordedPoints.Count == 0 ||
                        GetDistance(RecordedPoints[^1], _ghostX, _ghostZ) > 0.5)
                    {
                        RecordedPoints.Add(new TrackPoint { X = _ghostX, Z = _ghostZ });
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

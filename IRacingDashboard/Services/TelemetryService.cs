using System;
using System.Timers;
using irsdkSharp;
using irsdkSharp.Serialization;
using irsdkSharp.Serialization.Models.Data;
using irsdkSharp.Serialization.Models.Fastest;

namespace IRacingDashboard.Services
{
    public class TelemetryService
    {
        private static TelemetryService _instance;
        public static TelemetryService Instance => _instance ??= new TelemetryService();


        private readonly IRacingSDK _irsdk;
        private readonly System.Timers.Timer _pollingTimer;
        private readonly double _pollingIntervalMs = 16.6;

        public event Action<Data> TelemetryUpdated;
        public event Action<bool> ConnectionChanged;


        public TelemetryService()
        {
            _irsdk = new IRacingSDK();
            _pollingTimer = new System.Timers.Timer(_pollingIntervalMs);
            _pollingTimer.Elapsed += PollingTimerElapsed;
        }

        public void Start()
        {
            _pollingTimer.Start();
        }

        public void Stop()
        {
            _pollingTimer.Stop();
        }

        private void PollingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            bool connected = _irsdk.IsConnected();

            ConnectionChanged?.Invoke(connected); // ✅ Tell the ViewModel the connection status


            if (connected)
            {
                var telemetry = _irsdk.GetData();
                if (telemetry != null)
                {
                    TelemetryUpdated?.Invoke(telemetry);
                }
            }
        }
    }
}

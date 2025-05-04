using System;
using System.Timers;
using irsdkSharp;
using irsdkSharp.Serialization;
using irsdkSharp.Serialization.Models.Data;
using irsdkSharp.Serialization.Models.Fastest;
using irsdkSharp.Serialization.Models.Session;
using irsdkSharp.Serialization.Models.Session.SessionInfo;
using irsdkSharp.Serialization.Models.Session.SplitTimeInfo;
using irsdkSharp.Serialization.Models.Session.WeekendInfo;

namespace IRacingDashboard.Services
{
    public class TelemetryService
    {
        private static TelemetryService _instance;
        public static TelemetryService Instance => _instance ??= new TelemetryService();


        private readonly IRacingSDK _irsdk;
        private readonly System.Timers.Timer _pollingTimer;
        private readonly double _pollingIntervalMs = 16;

        public event Action<Data> TelemetryUpdated;
        public event Action<IRacingSessionModel> WeekendInfoUpdated;
        public event Action<SectorModel> SectorModelUpdated;
        public event Action<SplitTimeInfoModel> SplitTimeInfoUpdated;

        public event Action<bool> ConnectionChanged;
        public event Action<int> SectorsChanged;
        public event Action<SplitTimeInfoModel> SplitTimeInfoChanged;


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
                var sessionInfo = _irsdk.GetSerializedSessionInfo();
                //var sectorInfo = _irsdk.getsec();

                
                if (telemetry != null)
                {
                    int NSectors = sessionInfo.SplitTimeInfo.Sectors.Count();
                    SplitTimeInfoModel SplitTimeInfoModel = sessionInfo.SplitTimeInfo;
             
                    
                    SplitTimeInfoChanged?.Invoke(SplitTimeInfoModel); // ✅ Tell the ViewModel the connection status
                    SectorsChanged?.Invoke(NSectors); // ✅ Tell the ViewModel the connection status
                    
                    TelemetryUpdated?.Invoke(telemetry);
                    WeekendInfoUpdated?.Invoke(sessionInfo);
                    //SectorModelUpdated?.Invoke(sectorInfo);
                }
            }
        }
    }
}

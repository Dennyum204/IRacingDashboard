using IRacingDashboard.Models;
using IRacingDashboard.Services;
using irsdkSharp.Serialization.Enums.Fastest;
using irsdkSharp.Serialization.Models.Fastest;
using LiveChartsCore.Defaults;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRacingDashboard.ViewModels
{
    class PersonalLapsDashboardViewModel : BaseViewModel
    {
        private readonly TelemetryService _telemetryService;


        private double? _lapStartTime = null;
        private double _lastLapTime = 0;
        private int _lastLapCompleted = -1;
        public ObservableCollection<LapTimeEntry> LapTimes { get; set; } = new();

        private int _time = 0;
        private int _lastLapLogged = -1;

        bool lapUpdated = false;



        private int _lastIncidentCount = 0;
        private bool _wentOffTrackDuringLap = false;
        public PersonalLapsDashboardViewModel()
        {
            _telemetryService = TelemetryService.Instance; // 🔥 Singleton access


            _telemetryService.TelemetryUpdated += OnTelemetryUpdated;
            _telemetryService.Start(); 

        }

        private void OnTelemetryUpdated(Data telemetry)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                double lapTime = telemetry.LapCurrentLapTime;

              
                _lastLapTime = lapTime;

              
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
                if (telemetry.Lap > _lastLapLogged)
                {
                    double lapTime = telemetry.LapLastLapTime;

                    bool incidentIncreased = telemetry.PlayerCarMyIncidentCount > _lastIncidentCount;
                    bool isValidLap = !incidentIncreased && !_wentOffTrackDuringLap && lapTime > 0; ;

                    double deltaToBest = (telemetry.LapBestLapTime > 0 && lapTime > 0)
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
    }
}

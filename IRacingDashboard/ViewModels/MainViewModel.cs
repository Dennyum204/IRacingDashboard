using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using IRacingDashboard.Services;
using irsdkSharp.Serialization.Models.Fastest;
using System.Collections.Generic;
using System.Windows.Media;

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

        private List<double> _throttleValues = new();
        private List<double> _brakeValues = new();
        private int _time = 0;

        public MainViewModel()
        {
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<double> { Name = "Throttle", Values = _throttleValues },
                new LineSeries<double> { Name = "Brake", Values = _brakeValues }
            };

            XAxes = new List<Axis> { new Axis { Name = "Time (s)" } };
            YAxes = new List<Axis> { new Axis { Name = "Input", MinLimit = 0, MaxLimit = 1 } };

            _telemetryService = new TelemetryService();
            _telemetryService.TelemetryUpdated += OnTelemetryUpdated;
            _telemetryService.ConnectionChanged += OnConnectionChanged; // ✅ Listen for connection changes
            _telemetryService.Start();
        }

        private void OnConnectionChanged(bool connected)
        {
            IsConnected = connected;
        }
        private void OnTelemetryUpdated(Data telemetry)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _time++;

                if (_throttleValues.Count > 100)
                {
                    _throttleValues.RemoveAt(0);
                    _brakeValues.RemoveAt(0);
                }

                _throttleValues.Add(telemetry.Throttle);
                _brakeValues.Add(telemetry.Brake);
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

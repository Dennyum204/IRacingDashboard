using IRacingDashboard.Services;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRacingDashboard.Models;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;

namespace IRacingDashboard.ViewModels
{
    class SingleDashboardsModelView :  BaseViewModel
    {
        private readonly TelemetryService _telemetryService;
        public string ConnectionStatus => IsConnected ? "Connected to iRacing" : "Waiting for iRacing...";
        public Brush ConnectionColor => IsConnected ? Brushes.Green : Brushes.Red;


        public ObservableCollection<DashboardItem> Dashboards { get; set; } = new();

        public SingleDashboardsModelView()
        {
            _telemetryService = new TelemetryService();
            _telemetryService.ConnectionChanged += OnConnectionChanged; // ✅ Listen for connection changes
            _telemetryService.Start();


            Dashboards.Add(new DashboardItem { Name = "Map Track", ViewModelType = typeof(TrackMapDashboardViewModel) });
            Dashboards.Add(new DashboardItem { Name = "Telemetry Dashboard", ViewModelType = typeof(TelemetryDashboardViewModel) });
            Dashboards.Add(new DashboardItem { Name = "Personal Lap Times", ViewModelType = typeof(PersonalLapsDashboardViewModel) });

        }
        private void OnConnectionChanged(bool connected)
        {
            IsConnected = connected;
        }

        #region Commands
        private DashboardItem _selectedDashboard;
        public DashboardItem SelectedDashboard
        {
            get => _selectedDashboard;
            set
            {
                _selectedDashboard = value;
                OnPropertyChanged(nameof(SelectedDashboard));

                if (value != null)
                {
                    OpenDashboard(value);
                }
            }
        }

        private void OpenDashboard(DashboardItem dashboard)
        {
            if (dashboard == null) return;

            Window window = null;

            if (dashboard.ViewModelType == typeof(TelemetryDashboardViewModel))
            {
                window = new Views.TelemetryDashboard(); // View already sets its ViewModel internally
            }
            else if (dashboard.ViewModelType == typeof(PersonalLapsDashboardViewModel))
            {
                window = new Views.PersonalLapsDashboard();
            }
            else if (dashboard.ViewModelType == typeof(TrackMapDashboardViewModel))
            {
                window = new Views.TrackMapDashboardView();
            }

            if (window == null) return;

            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }



        #endregion

        #region Properties
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value; 
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(ConnectionStatus));
                OnPropertyChanged(nameof(ConnectionColor));
            }
        }

        #endregion
    }
}

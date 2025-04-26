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
using irsdkSharp.Serialization.Enums.Fastest;
using System.Windows.Input;
using Adapters; // ✅ for ObservablePoint

namespace IRacingDashboard.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        private readonly SingleDashboardsModelView _SingleDashboardsModelView;


      
        public MainViewModel()
        {

            OpenSingleDashboardsCommand = new ViewModelCommands(OpenSingleDashboardsExecuted);
            _SingleDashboardsModelView = new SingleDashboardsModelView();

          
        }
    


        #region Commands
        public ICommand OpenSingleDashboardsCommand { get; }
        private void OpenSingleDashboardsExecuted(object obj)
        {
            CurrentViewModel = _SingleDashboardsModelView;
        }

        #endregion

        #region Properties

        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            get
            {
                return _currentViewModel;
            }
            set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));

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

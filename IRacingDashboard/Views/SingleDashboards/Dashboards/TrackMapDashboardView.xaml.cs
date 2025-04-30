using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IRacingDashboard.Views
{
    /// <summary>
    /// Interaction logic for TrackMapDashboardView.xaml
    /// </summary>
    public partial class TrackMapDashboardView : Window
    {
        public TrackMapDashboardView()
        {
            InitializeComponent();
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void UpdateCamara()
        {
            //// Get the camera properties
            var camera = TrackCamera;

            // Update TextBlocks with the camera's Position, LookDirection, and UpDirection
            PositionText.Text = $"Positio0n: {camera.Position.X:F2}, {camera.Position.Y:F2}, {camera.Position.Z:F2}";
            LookDirectionText.Text = $"LookDirection: {camera.LookDirection.X:F2}, {camera.LookDirection.Y:F2}, {camera.LookDirection.Z:F2}";
            UpDirectionText.Text = $"UpDirection: {camera.UpDirection.X:F2}, {camera.UpDirection.Y:F2}, {camera.UpDirection.Z:F2}";
        }

        private void UpdateCameraBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateCamara();
        }

    }
}

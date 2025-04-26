using IRacingDashboard.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IRacingDashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private double _prevWidth, _prevHeight, _prevTop, _prevLeft;
    private bool _isMaximized = false;

    public MainWindow()
    {
        InitializeComponent();
        SaveWindowState();
        //ToggleMaximize();
    }
    #region ButtonsHeader
    private void SaveWindowState()
    {
        _prevWidth = this.Width;
        _prevHeight = this.Height;
        _prevTop = this.Top;
        _prevLeft = this.Left;
    }

    public void ToggleMaximize()
    {
        if (_isMaximized)
        {
            // Restore Normal Window Size
            this.Width = _prevWidth;
            this.Height = _prevHeight;
            this.Top = _prevTop;
            this.Left = _prevLeft;
        }
        else
        {
            // Save the current window state before maximizing
            SaveWindowState();

            // Set the window to taskbar-friendly maximized mode
            this.Top = SystemParameters.WorkArea.Top;
            this.Left = SystemParameters.WorkArea.Left;
            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.WorkArea.Height;
        }

        _isMaximized = !_isMaximized;
    }


    private void btnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void btnMaximize_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();

    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else if (e.LeftButton == MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }
    #endregion
}
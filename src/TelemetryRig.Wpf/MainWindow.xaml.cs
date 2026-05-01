using System.Windows;
using TelemetryRig.Wpf.ViewModels;

namespace TelemetryRig.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Simple composition root for this learning project.
        // Larger apps normally use dependency injection.
        DataContext = new MainViewModel();
    }
}

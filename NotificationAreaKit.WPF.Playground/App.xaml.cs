using System.Windows;

using NotificationAreaKit.Wpf.Internal;

namespace NotificationAreaKit.WPF.Playground;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        // Cleanly shut down the shared TrayIconManager
        // when the application exits.
        TrayIconManager.Shutdown();
        base.OnExit(e);
    }
}
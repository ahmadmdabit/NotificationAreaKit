using System.Configuration;
using System.Data;
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
        // CRITICAL FIX: Cleanly shut down the shared TrayIconManager
        // when the application exits.
        TrayIconManager.Shutdown();
        base.OnExit(e);
    }
}

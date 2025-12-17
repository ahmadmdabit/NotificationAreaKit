using System.IO;

using NotificationAreaKit.Core.Notifications;

namespace NotificationAreaKit.Wpf.Internal.Notifications;

/// <summary>
/// STRATEGY 3: Proactive First-Run Handling
/// This service robustly handles the first-run AUMID registration race condition
/// by intentionally using balloon tips on the very first launch, giving the OS
/// time to index the required shortcut. On all subsequent launches, it uses
/// modern toast notifications. This prevents the 'ERROR_NOT_FOUND' COMException.
/// </summary>
internal sealed class FirstRunNotificationService : INotificationService
{
    // A simple flag file to track if the application has run at least once.
    private static readonly string FlagFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MyTrayApp", // Use a dedicated folder for your app's data
        "firstrun.flag");

    private readonly INotificationService _toastService;
    private readonly INotificationService _balloonService;

    public FirstRunNotificationService(INotificationService toastService, INotificationService balloonService)
    {
        _toastService = toastService;
        _balloonService = balloonService;
    }

    public void Notify(string title, string message)
    {
        if (!File.Exists(FlagFilePath))
        {
            // On the first run, use the reliable balloon service.
            _balloonService.Notify(title, message);

            // After the first notification, create the flag file so that
            // subsequent runs will be treated as normal.
            EnsureFlagFileExists();
        }
        else
        {
            // On all subsequent runs, the AUMID is guaranteed to be registered.
            // We can now safely use the modern toast service.
            _toastService.Notify(title, message);
        }
    }

    private static void EnsureFlagFileExists()
    {
        try
        {
            // Check again to be safe in case of multiple notifications on first run.
            if (!File.Exists(FlagFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath)!);
                // Create the file and immediately close the handle.
                File.Create(FlagFilePath).Dispose();
            }
        }
        catch
        {
            // If we fail to write the flag file (e.g., permissions issue),
            // we will gracefully degrade and continue using balloon tips.
            // This is a safe failure mode.
        }
    }
}
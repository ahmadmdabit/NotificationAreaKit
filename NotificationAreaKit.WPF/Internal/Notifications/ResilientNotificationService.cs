using System.Runtime.InteropServices;

using NotificationAreaKit.Core.Notifications;

namespace NotificationAreaKit.Wpf.Internal.Notifications;

/// <summary>
/// Provides a resilient notification strategy for Windows 10+.
/// It attempts to use modern Toast notifications but gracefully falls back
/// to legacy Balloon tips if the Toast system fails (e.g., on first run).
/// </summary>
internal sealed class ResilientNotificationService : INotificationService
{
    private readonly INotificationService toastService;
    private readonly INotificationService balloonService;

    public ResilientNotificationService(TrayIconManager manager, uint iconId, string appId)
    {
        toastService = new ToastNotificationService(appId);
        balloonService = new BalloonNotificationService(manager, iconId);
    }

    public void Notify(string title, string message)
    {
        try
        {
            toastService.Notify(title, message);
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80070490)) // ERROR_NOT_FOUND
        {
            // This specific error occurs on the first run when the Start Menu shortcut
            // has not yet been indexed by the OS. Fallback to the balloon tip.
            balloonService.Notify(title, message);
        }
    }
}
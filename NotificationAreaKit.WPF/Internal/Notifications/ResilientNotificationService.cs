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
    private readonly INotificationService _toastService;
    private readonly INotificationService _balloonService;

    public ResilientNotificationService(TrayIconManager manager, uint iconId, string appId)
    {
        _toastService = new ToastNotificationService(appId);
        _balloonService = new BalloonNotificationService(manager, iconId);
    }

    public void Notify(string title, string message)
    {
        try
        {
            _toastService.Notify(title, message);
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80070490)) // ERROR_NOT_FOUND
        {
            // This specific error occurs on the first run when the Start Menu shortcut
            // has not yet been indexed by the shell. Fallback to the balloon tip.
            _balloonService.Notify(title, message);
        }
    }
}
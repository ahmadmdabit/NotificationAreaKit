using NotificationAreaKit.Core.Notifications;

namespace NotificationAreaKit.Wpf.Internal;

internal static class NotificationFactory
{
    public static INotificationService Create(TrayIconManager manager, uint iconId, string appId)
    {
        // Check for Windows 10 Build 10240 or later
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
        {
            return new Notifications.ResilientNotificationService(manager, iconId, appId);
        }

        return new Notifications.BalloonNotificationService(manager, iconId);
    }
}
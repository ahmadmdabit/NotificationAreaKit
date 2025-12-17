using NotificationAreaKit.Core.Notifications;

namespace NotificationAreaKit.Wpf.Internal.Notifications;

internal sealed class BalloonNotificationService : INotificationService
{
    private readonly TrayIconManager manager;
    private readonly uint iconId;

    public BalloonNotificationService(TrayIconManager manager, uint iconId)
    {
        this.manager = manager;
        this.iconId = iconId;
    }

    public void Notify(string title, string message)
    {
        manager.ShowBalloon(iconId, title, message);
    }
}
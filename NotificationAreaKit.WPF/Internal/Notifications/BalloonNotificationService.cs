using NotificationAreaKit.Core.Notifications;

namespace NotificationAreaKit.Wpf.Internal.Notifications;

internal sealed class BalloonNotificationService : INotificationService
{
    private readonly TrayIconManager _manager;
    private readonly uint _iconId;

    public BalloonNotificationService(TrayIconManager manager, uint iconId)
    {
        _manager = manager;
        _iconId = iconId;
    }

    public void Notify(string title, string message)
    {
        _manager.ShowBalloon(_iconId, title, message);
    }
}
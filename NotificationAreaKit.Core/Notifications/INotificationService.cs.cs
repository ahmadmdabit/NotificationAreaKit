namespace NotificationAreaKit.Core.Notifications;

/// <summary>
/// Defines the internal contract for a service that can display a notification.
/// This abstraction allows for different notification strategies (e.g., Toast, Balloon).
/// </summary>
internal interface INotificationService
{
    /// <summary>
    /// Displays a notification to the user.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The main message body of the notification.</param>
    void Notify(string title, string message);
}
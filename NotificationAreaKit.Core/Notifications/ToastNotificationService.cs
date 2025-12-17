using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace NotificationAreaKit.Core.Notifications;

/// <summary>
/// Provides modern Toast Notification functionality for Windows 10 and newer.
/// This service is internal and consumed by the WPF-specific library.
/// </summary>
internal sealed class ToastNotificationService : INotificationService
{
    private readonly string _appId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastNotificationService"/> class.
    /// </summary>
    /// <param name="appId">The Application User Model ID (AUMID) required to show toasts.</param>
    public ToastNotificationService(string appId)
    {
        // Pre-conditions are essential for robust libraries.
        if (string.IsNullOrWhiteSpace(appId))
        {
            throw new ArgumentException("Application ID (AUMID) cannot be null or whitespace.", nameof(appId));
        }
        _appId = appId;
    }

    /// <inheritdoc/>
    public void Notify(string title, string message)
    {
        // This is the most efficient way to construct the XML for a simple toast.
        // It avoids loading external XML files and is self-contained.
        XmlDocument xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

        XmlNodeList textNodes = xml.GetElementsByTagName("text");
        textNodes[0]!.InnerText = title;
        textNodes[1]!.InnerText = message;

        var toast = new ToastNotification(xml);

        // CRITICAL: Use the overload that accepts the AUMID. This is the key to making
        // toasts reliable in unpackaged desktop apps, removing OS ambiguity.
        ToastNotificationManager.CreateToastNotifier(_appId).Show(toast);
    }
}
using NotificationAreaKit.Core;
using NotificationAreaKit.Core.Interop;

namespace NotificationAreaKit.Wpf.Internal;

/// <summary>
/// Handles one-time, process-wide initialization for the tray application.
/// This is thread-safe and ensures setup logic runs only once.
/// </summary>
internal static class TrayAppInitializer
{
    private static readonly Lazy<(string AppId, string AppName)> initializer = new(InitializeInternal);
    private static string? appId;
    private static string? appName;

    public static void Initialize(string appId, string appName)
    {
        TrayAppInitializer.appId = appId;
        TrayAppInitializer.appName = appName;
        // Accessing the Value property triggers the factory method if it hasn't run.
        _ = initializer.Value;
    }

    private static (string AppId, string AppName) InitializeInternal()
    {
        if (appId is null || appName is null)
        {
            throw new InvalidOperationException("Initialization failed: AppId and AppName must be set.");
        }

        // 1. Register AUMID (Critical for Toasts)
        SystemPrimitives.SetCurrentProcessExplicitAppUserModelID(appId);

        // 2. Ensure Shortcut exists (Critical for Toasts)
        ShortcutHelper.EnsureShortcutExists(appId, appName);

        return (appId, appName);
    }
}
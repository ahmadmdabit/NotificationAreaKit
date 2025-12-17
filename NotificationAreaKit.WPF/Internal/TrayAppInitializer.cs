using NotificationAreaKit.Core;
using NotificationAreaKit.Core.Interop;

namespace NotificationAreaKit.Wpf.Internal;

/// <summary>
/// Handles one-time, process-wide initialization for the tray application.
/// This is thread-safe and ensures setup logic runs only once.
/// </summary>
internal static class TrayAppInitializer
{
    private static readonly Lazy<(string AppId, string AppName)> _initializer = new(InitializeInternal);
    private static string? _appId;
    private static string? _appName;

    public static void Initialize(string appId, string appName)
    {
        _appId = appId;
        _appName = appName;
        // Accessing the Value property triggers the factory method if it hasn't run.
        _ = _initializer.Value;
    }

    private static (string AppId, string AppName) InitializeInternal()
    {
        if (_appId is null || _appName is null)
        {
            throw new InvalidOperationException("Initialization failed: AppId and AppName must be set.");
        }

        // 1. Register AUMID (Critical for Toasts)
        NativeMethods.SetCurrentProcessExplicitAppUserModelID(_appId);

        // 2. Ensure Shortcut exists (Critical for Toasts)
        ShortcutHelper.EnsureShortcutExists(_appId, _appName);

        return (_appId, _appName);
    }
}
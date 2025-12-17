using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

using NotificationAreaKit.Core.Notifications;
using NotificationAreaKit.Wpf.Internal;
using NotificationAreaKit.Wpf.Internal.Interop;

namespace NotificationAreaKit.Wpf;

/// <summary>
/// Represents a single, managed icon in the Windows Notification Area (System Tray).
/// This class is the primary entry point for the NotificationAreaKit.WPF library.
/// </summary>
public sealed class WpfTrayIcon : IDisposable
{
    // This now holds a reference to the SHARED singleton instance.
    private readonly TrayIconManager trayManager;

    private readonly INotificationService notificationService;
    private readonly System.Drawing.Icon icon;
    private readonly uint iconId;
    private bool disposed;

    // State machine components for hover functionality
    private HoverPopupWindow? hoverPopup;

    private DispatcherTimer? hoverDelayTimer;
    private DispatcherTimer? hideTimer;
    private DispatcherTimer? mouseLeavePollTimer;

    /// <summary>
    /// Raised when the user performs a single left-click on the tray icon.
    /// </summary>
    public event EventHandler? LeftClick;

    /// <summary>
    /// Raised when the user performs a right-click on the tray icon.
    /// </summary>
    public event EventHandler? RightClick;

    /// <summary>
    /// Raised when the user performs a double-click on the tray icon.
    /// </summary>
    public event EventHandler? DoubleClick;

    /// <summary>
    /// Gets or sets the WPF UIElement to display when the user hovers over the tray icon.
    /// Set to null to disable the hover popup.
    /// </summary>
    public UIElement? HoverContent { get; set; }

    /// <summary>
    /// Gets or sets the delay before the hover popup appears.
    /// Default is 200 milliseconds.
    /// </summary>
    public TimeSpan HoverDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfTrayIcon"/> class.
    /// </summary>
    /// <param name="iconResourcePath">The WPF Pack URI path to the .ico resource (e.g., "pack://application:,,,/Resources/icon.ico").</param>
    /// <param name="tooltip">The tooltip text to display when hovering over the icon.</param>
    /// <param name="appId">The Application User Model ID (AUMID). Must be unique to your app.</param>
    /// <param name="appName">The application name, used for the Start Menu shortcut.</param>
    public WpfTrayIcon(string iconResourcePath, string tooltip, string appId, string appName)
    {
        TrayAppInitializer.Initialize(appId, appName);

        var iconUri = new Uri(iconResourcePath);
        var streamInfo = Application.GetResourceStream(iconUri)
            ?? throw new ArgumentException($"Icon resource not found at path: {iconResourcePath}", nameof(iconResourcePath));

        icon = new System.Drawing.Icon(streamInfo.Stream);

        // Get the singleton instance instead of creating a new one.
        trayManager = TrayIconManager.Instance;
        iconId = trayManager.AddIcon(icon.Handle, tooltip);

        notificationService = NotificationFactory.Create(trayManager, iconId, appId);

        trayManager.IconLeftClicked += OnIconLeftClicked;
        trayManager.IconRightClicked += OnIconRightClicked;
        trayManager.IconDoubleClicked += OnIconDoubleClicked;
        trayManager.IconMouseMove += OnIconMouseMove;
    }

    /// <summary>
    /// Displays a notification. This will automatically use a modern Toast on supported
    /// OS versions or a legacy Balloon tip as a fallback.
    /// </summary>
    public void ShowNotification(string title, string message) => notificationService.Notify(title, message);

    /// <summary>
    /// Explicitly displays a legacy-style Balloon notification.
    /// </summary>
    public void ShowBalloon(string title, string message) => trayManager.ShowBalloon(iconId, title, message);

    private void OnIconLeftClicked(uint id)
    {
        if (id != iconId) return;
        LeftClick?.Invoke(this, EventArgs.Empty);
    }

    private void OnIconRightClicked(uint id)
    {
        if (id != iconId) return;
        RightClick?.Invoke(this, EventArgs.Empty);
    }

    private void OnIconDoubleClicked(uint id)
    {
        if (id != iconId) return;
        DoubleClick?.Invoke(this, EventArgs.Empty);
    }

    #region Hover State Machine Logic

    private void OnIconMouseMove(uint id)
    {
        if (id != iconId || HoverContent is null) return;

        hideTimer?.Stop();

        // DO NOT start the polling timer here. This was the source of the race condition.

        if (hoverPopup is { IsVisible: true }) return;

        if (hoverDelayTimer is null)
        {
            hoverDelayTimer = new DispatcherTimer { Interval = HoverDelay };
            hoverDelayTimer.Tick += (s, e) => ShowHoverPopup();
        }
        hoverDelayTimer.Stop();
        hoverDelayTimer.Start();
    }

    private void OnMouseLeavePollTimerTick(object? sender, EventArgs e)
    {
        var identifier = new SystemPrimitives.NotifyIconIdentifier
        {
            cbSize = (uint)Marshal.SizeOf<SystemPrimitives.NotifyIconIdentifier>(),
            hWnd = trayManager.MessageWindowHandle,
            uID = iconId
        };

        if (SystemPrimitives.NotifyIconGetRect(ref identifier, out var iconRect) == 0)
        {
            SystemPrimitives.GetCursorPos(out var cursorPoint);
            var wpfRect = new Rect(iconRect.Left, iconRect.Top, iconRect.Right - iconRect.Left, iconRect.Bottom - iconRect.Top);

            // If the cursor is outside OUR icon's rectangle, we have a "leave" event.
            if (!wpfRect.Contains(new Point((double)cursorPoint.X, (double)cursorPoint.Y)))
            {
                mouseLeavePollTimer?.Stop(); // Stop polling to save resources.
                StartHideTimer();
            }
        }
        else
        {
            // If we can't get the rect for any reason, stop polling to be safe.
            mouseLeavePollTimer?.Stop();
        }
    }

    private void ShowHoverPopup()
    {
        hoverDelayTimer?.Stop();
        if (HoverContent is null) return;

        if (hoverPopup is null)
        {
            hoverPopup = new HoverPopupWindow();
            hoverPopup.MouseLeave += (s, e) => StartHideTimer();
            hoverPopup.MouseEnter += (s, e) => hideTimer?.Stop();
        }

        hoverPopup.Child = HoverContent;
        hoverPopup.ShowAtCursor();

        // Start polling for "mouse leave" ONLY AFTER the popup is shown.
        if (mouseLeavePollTimer is null)
        {
            mouseLeavePollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            mouseLeavePollTimer.Tick += OnMouseLeavePollTimerTick;
        }
        mouseLeavePollTimer.Start();
    }

    private void StartHideTimer()
    {
        hoverDelayTimer?.Stop();
        // Also stop polling when we start the hide process.
        mouseLeavePollTimer?.Stop();

        if (hideTimer is null)
        {
            hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            hideTimer.Tick += (s, e) => HideHoverPopup();
        }
        hideTimer.Start();
    }

    private void HideHoverPopup()
    {
        hideTimer?.Stop();
        mouseLeavePollTimer?.Stop(); // Ensure polling is stopped when hidden.
        hoverPopup?.Hide();
    }

    #endregion Hover State Machine Logic

    /// <summary>
    /// Removes the icon from the notification area and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;

        // Unsubscribe from the shared manager's events.
        trayManager.IconLeftClicked -= OnIconLeftClicked;
        trayManager.IconRightClicked -= OnIconRightClicked;
        trayManager.IconDoubleClicked -= OnIconDoubleClicked;
        trayManager.IconMouseMove -= OnIconMouseMove;

        // Stop any timers associated with this instance.
        hoverDelayTimer?.Stop();
        hideTimer?.Stop();
        mouseLeavePollTimer?.Stop();

        hoverPopup?.Close();

        // Only remove this specific icon. Do NOT dispose the shared manager.
        trayManager.RemoveIcon(iconId);
        icon.Dispose();

        disposed = true;
        GC.SuppressFinalize(this);
    }
}
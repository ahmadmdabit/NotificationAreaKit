using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using NotificationAreaKit.Wpf.Internal;
using NotificationAreaKit.Wpf.Internal.Interop;
using NotificationAreaKit.Core.Notifications;

namespace NotificationAreaKit.Wpf;

/// <summary>
/// Represents a single, managed icon in the Windows Notification Area (System Tray).
/// This class is the primary entry point for the NotificationAreaKit.WPF library.
/// </summary>
public sealed class WpfTrayIcon : IDisposable
{
    // This now holds a reference to the SHARED singleton instance.
    private readonly TrayIconManager _trayManager;
    private readonly INotificationService _notificationService;
    private readonly System.Drawing.Icon _icon;
    private readonly uint _iconId;
    private bool _disposed;

    // State machine components for hover functionality
    private HoverPopupWindow? _hoverPopup;
    private DispatcherTimer? _hoverDelayTimer;
    private DispatcherTimer? _hideTimer;
    private DispatcherTimer? _mouseLeavePollTimer;

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

        _icon = new System.Drawing.Icon(streamInfo.Stream);

        // CRITICAL FIX: Get the singleton instance instead of creating a new one.
        _trayManager = TrayIconManager.Instance;
        _iconId = _trayManager.AddIcon(_icon.Handle, tooltip);

        _notificationService = NotificationFactory.Create(_trayManager, _iconId, appId);

        _trayManager.IconLeftClicked += OnIconLeftClicked;
        _trayManager.IconRightClicked += OnIconRightClicked;
        _trayManager.IconDoubleClicked += OnIconDoubleClicked;
        _trayManager.IconMouseMove += OnIconMouseMove;
    }

    /// <summary>
    /// Displays a notification. This will automatically use a modern Toast on supported
    /// OS versions or a legacy Balloon tip as a fallback.
    /// </summary>
    public void ShowNotification(string title, string message) => _notificationService.Notify(title, message);

    /// <summary>
    /// Explicitly displays a legacy-style Balloon notification.
    /// </summary>
    public void ShowBalloon(string title, string message) => _trayManager.ShowBalloon(_iconId, title, message);

    private void OnIconLeftClicked(uint id)
    {
        if (id != _iconId) return;
        LeftClick?.Invoke(this, EventArgs.Empty);
    }

    private void OnIconRightClicked(uint id)
    {
        if (id != _iconId) return;
        RightClick?.Invoke(this, EventArgs.Empty);
    }

    private void OnIconDoubleClicked(uint id)
    {
        if (id != _iconId) return;
        DoubleClick?.Invoke(this, EventArgs.Empty);
    }

    #region Hover State Machine Logic

    private void OnIconMouseMove(uint id)
    {
        if (id != _iconId || HoverContent is null) return;

        _hideTimer?.Stop();

        // DO NOT start the polling timer here. This was the source of the race condition.

        if (_hoverPopup is { IsVisible: true }) return;

        if (_hoverDelayTimer is null)
        {
            _hoverDelayTimer = new DispatcherTimer { Interval = HoverDelay };
            _hoverDelayTimer.Tick += (s, e) => ShowHoverPopup();
        }
        _hoverDelayTimer.Stop();
        _hoverDelayTimer.Start();
    }

    private void OnMouseLeavePollTimerTick(object? sender, EventArgs e)
    {
        var identifier = new NativeMethods.NOTIFYICONIDENTIFIER
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONIDENTIFIER>(),
            hWnd = _trayManager.MessageWindowHandle,
            uID = _iconId
        };

        if (NativeMethods.Shell_NotifyIconGetRect(ref identifier, out var iconRect) == 0)
        {
            NativeMethods.GetCursorPos(out var cursorPoint);
            var wpfRect = new Rect(iconRect.Left, iconRect.Top, iconRect.Right - iconRect.Left, iconRect.Bottom - iconRect.Top);

            // If the cursor is outside OUR icon's rectangle, we have a "leave" event.
            if (!wpfRect.Contains(new Point(cursorPoint.X, cursorPoint.Y)))
            {
                _mouseLeavePollTimer?.Stop(); // Stop polling to save resources.
                StartHideTimer();
            }
        }
        else
        {
            // If we can't get the rect for any reason, stop polling to be safe.
            _mouseLeavePollTimer?.Stop();
        }
    }

    private void ShowHoverPopup()
    {
        _hoverDelayTimer?.Stop();
        if (HoverContent is null) return;

        if (_hoverPopup is null)
        {
            _hoverPopup = new HoverPopupWindow();
            _hoverPopup.MouseLeave += (s, e) => StartHideTimer();
            _hoverPopup.MouseEnter += (s, e) => _hideTimer?.Stop();
        }

        _hoverPopup.Child = HoverContent;
        _hoverPopup.ShowAtCursor();

        // THE FIX: Start polling for "mouse leave" ONLY AFTER the popup is shown.
        if (_mouseLeavePollTimer is null)
        {
            _mouseLeavePollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _mouseLeavePollTimer.Tick += OnMouseLeavePollTimerTick;
        }
        _mouseLeavePollTimer.Start();
    }

    private void StartHideTimer()
    {
        _hoverDelayTimer?.Stop();
        // Also stop polling when we start the hide process.
        _mouseLeavePollTimer?.Stop();

        if (_hideTimer is null)
        {
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _hideTimer.Tick += (s, e) => HideHoverPopup();
        }
        _hideTimer.Start();
    }

    private void HideHoverPopup()
    {
        _hideTimer?.Stop();
        _mouseLeavePollTimer?.Stop(); // Ensure polling is stopped when hidden.
        _hoverPopup?.Hide();
    }

    #endregion

    /// <summary>
    /// Removes the icon from the notification area and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        // Unsubscribe from the shared manager's events.
        _trayManager.IconLeftClicked -= OnIconLeftClicked;
        _trayManager.IconRightClicked -= OnIconRightClicked;
        _trayManager.IconDoubleClicked -= OnIconDoubleClicked;
        _trayManager.IconMouseMove -= OnIconMouseMove;

        // Stop any timers associated with this instance.
        _hoverDelayTimer?.Stop();
        _hideTimer?.Stop();
        _mouseLeavePollTimer?.Stop();

        _hoverPopup?.Close();

        // CRITICAL FIX: Only remove this specific icon. Do NOT dispose the shared manager.
        _trayManager.RemoveIcon(_iconId);
        _icon.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
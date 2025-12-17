using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

using NotificationAreaKit.Wpf.Internal.Interop;

namespace NotificationAreaKit.Wpf.Internal;

/// <summary>
/// A singleton, process-wide manager for all tray icons. This class acts as the single
/// message pump for all Win32 tray events. It does not implement IDisposable to prevent
/// accidental disposal by a consumer; cleanup is managed via the static Shutdown() method.
/// </summary>
public sealed class TrayIconManager
{
    // 1. THREAD-SAFE SINGLETON IMPLEMENTATION
    // Lazy<T> guarantees the instance is created only once in a thread-safe manner.
    private static readonly Lazy<TrayIconManager> _lazyInstance = new(() => new TrayIconManager());

    /// <summary>
    /// Gets the single, shared instance of the TrayIconManager for the application.
    /// </summary>
    public static TrayIconManager Instance => _lazyInstance.Value;

    /// <summary>
    /// The handle to the hidden message window, required by consumers to get icon rectangles.
    /// </summary>
    public readonly IntPtr MessageWindowHandle;

    private readonly HwndSource _messageWindow;
    private readonly object _lock = new();
    private readonly Dictionary<uint, NativeMethods.NOTIFYICONDATA> _icons = new();
    private bool _disposed;
    private uint _nextId = 1;

    private readonly DispatcherTimer _singleClickTimer;
    private uint _pendingSingleClickId;
    private bool _handlingDoubleClick = false;

    // Events - Dispatched to UI thread automatically
    public event Action<uint>? IconLeftClicked;
    public event Action<uint>? IconRightClicked;
    public event Action<uint>? IconDoubleClicked;
    public event Action<uint>? IconMouseMove;

    // 2. PRIVATE CONSTRUCTOR
    // This is essential to enforce the singleton pattern, preventing external instantiation.
    private TrayIconManager()
    {
        // Create a message-only window
        // This MUST be done on the UI thread to ensure the HwndSource belongs to the correct Dispatcher.
        var parameters = new HwndSourceParameters("TrayIconMessageWindow")
        {
            WindowStyle = 0,
            Width = 0,
            Height = 0,
            ParentWindow = (IntPtr)(-3), // HWND_MESSAGE
        };
        _messageWindow = new HwndSource(parameters);
        MessageWindowHandle = _messageWindow.Handle;
        _messageWindow.AddHook(WndProc);

        // Initialize the timer
        _singleClickTimer = new DispatcherTimer { IsEnabled = false };
        _singleClickTimer.Interval = TimeSpan.FromMilliseconds(NativeMethods.GetDoubleClickTime());
        _singleClickTimer.Tick += OnSingleClickTimerTick;
    }

    // 3. PUBLIC STATIC SHUTDOWN METHOD
    // This is the sole, explicit entry point for application-level cleanup.
    // It's clear, safe, and cannot be called accidentally on an instance.
    public static void Shutdown()
    {
        // Only perform cleanup if the instance was actually created.
        if (_lazyInstance.IsValueCreated)
        {
            Instance.DisposeCore();
        }
    }

    // Timer tick handler
    private void OnSingleClickTimerTick(object? sender, EventArgs e)
    {
        _singleClickTimer.Stop();
        InvokeEvent(IconLeftClicked, _pendingSingleClickId);
    }

    /// <summary>
    /// Adds an icon to the system tray.
    /// </summary>
    /// <param name="iconHandle">Handle to the icon (HICON). Caller is responsible for keeping this valid if not owned.</param>
    /// <param name="tooltip">Tooltip text.</param>
    /// <returns>The unique ID of the icon.</returns>
    public uint AddIcon(IntPtr iconHandle, string tooltip)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TrayIconManager));

        uint id = _nextId++;
        var nid = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = MessageWindowHandle,
            uID = id,
            uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_SHOWTIP,
            uCallbackMessage = NativeMethods.WM_TRAY_CALLBACK,
            hIcon = iconHandle,
            szTip = tooltip,
            uTimeoutOrVersion = NativeMethods.NOTIFYICON_VERSION_4
        };

        lock (_lock)
        {
            if (!NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref nid))
            {
                throw new InvalidOperationException("Failed to add system tray icon.");
            }
            // Set version to 4 to receive modern callback behavior
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_SETVERSION, ref nid);
            _icons[id] = nid;
        }
        return id;
    }

    public void RemoveIcon(uint id)
    {
        lock (_lock)
        {
            if (_icons.TryGetValue(id, out var nid))
            {
                NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref nid);
                _icons.Remove(id);
            }
        }
    }

    /// <summary>
    /// Updates the balloon tip (Legacy/Win7 support).
    /// </summary>
    public void ShowBalloon(uint id, string title, string message, uint iconType = NativeMethods.NIIF_INFO)
    {
        lock (_lock)
        {
            if (!_icons.TryGetValue(id, out var nid)) return;
            nid.uFlags = NativeMethods.NIF_INFO;
            nid.szInfoTitle = title;
            nid.szInfo = message;
            nid.dwInfoFlags = iconType;
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref nid);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_TRAY_CALLBACK)
        {
            // CRITICAL FIX: Correct parsing for NOTIFYICON_VERSION_4
            // In Version 4:
            // lParam (Low Word)  = Mouse Message (e.g., WM_MOUSEMOVE)
            // lParam (High Word) = Icon ID
            // wParam             = Mouse X Coordinate (relative to screen)

            int lParam32 = lParam.ToInt32();
            uint id = (uint)((lParam32 >> 16) & 0xFFFF);
            int mouseMsg = lParam32 & 0xFFFF;

            switch (mouseMsg)
            {
                case NativeMethods.WM_LBUTTONUP:
                    // If we just handled a double-click, ignore this trailing mouse-up event.
                    if (_handlingDoubleClick)
                    {
                        _handlingDoubleClick = false;
                        break;
                    }

                    // Otherwise, start the timer to wait for a potential double-click.
                    _singleClickTimer.Stop();
                    _pendingSingleClickId = id;
                    _singleClickTimer.Start();
                    break;

                case NativeMethods.WM_LBUTTONDBLCLK:
                    // A double-click occurred. Stop the single-click timer,
                    // set the flag to ignore the next mouse-up, and fire the event.
                    _singleClickTimer.Stop();
                    _handlingDoubleClick = true;
                    InvokeEvent(IconDoubleClicked, id);
                    break;

                case NativeMethods.WM_RBUTTONUP:
                    InvokeEvent(IconRightClicked, id);
                    break;

                case NativeMethods.WM_MOUSEMOVE:
                    InvokeEvent(IconMouseMove, id);
                    break;
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void InvokeEvent(Action<uint>? evt, uint id)
    {
        if (evt == null) return;
        if (_messageWindow.Dispatcher.CheckAccess())
        {
            evt(id);
        }
        else
        {
            _messageWindow.Dispatcher.InvokeAsync(() => evt(id));
        }
    }

    // 4. PRIVATE CLEANUP METHOD
    // This contains the actual resource disposal logic. It is private to ensure
    // it can only be called through the controlled Shutdown() path.
    private void DisposeCore()
    {
        if (_disposed) return;
        _disposed = true;

        // Stop the timer on dispose
        _singleClickTimer.Stop();

        lock (_lock)
        {
            foreach (var nid in _icons.Values)
            {
                var copy = nid; // struct copy
                NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref copy);
            }
            _icons.Clear();
        }
        _messageWindow.RemoveHook(WndProc);
        _messageWindow.Dispose();
    }
}
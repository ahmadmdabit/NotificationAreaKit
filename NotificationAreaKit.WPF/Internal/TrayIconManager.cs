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
    private static readonly Lazy<TrayIconManager> lazyInstance = new(() => new TrayIconManager());

    /// <summary>
    /// Gets the single, shared instance of the TrayIconManager for the application.
    /// </summary>
    public static TrayIconManager Instance => lazyInstance.Value;

    /// <summary>
    /// The handle to the hidden message window, required by consumers to get icon rectangles.
    /// </summary>
    public readonly IntPtr MessageWindowHandle;

    private readonly HwndSource messageWindow;
    private readonly Lock locker = new();
    private readonly Dictionary<uint, SystemPrimitives.NotifyIconData> icons = [];
    private bool disposed;
    private uint nextId = 1;

    private readonly DispatcherTimer singleClickTimer;
    private uint pendingSingleClickId;
    private bool handlingDoubleClick = false;

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
        messageWindow = new HwndSource(parameters);
        MessageWindowHandle = messageWindow.Handle;
        messageWindow.AddHook(WndProc);

        // Initialize the timer
        singleClickTimer = new DispatcherTimer
        {
            IsEnabled = false,
            Interval = TimeSpan.FromMilliseconds(SystemPrimitives.GetDoubleClickTime())
        };
        singleClickTimer.Tick += OnSingleClickTimerTick;
    }

    // 3. PUBLIC STATIC SHUTDOWN METHOD
    // This is the sole, explicit entry point for application-level cleanup.
    // It's clear, safe, and cannot be called accidentally on an instance.
    public static void Shutdown()
    {
        // Only perform cleanup if the instance was actually created.
        if (lazyInstance.IsValueCreated)
        {
            Instance.DisposeCore();
        }
    }

    // Timer tick handler
    private void OnSingleClickTimerTick(object? sender, EventArgs e)
    {
        singleClickTimer.Stop();
        InvokeEvent(IconLeftClicked, pendingSingleClickId);
    }

    /// <summary>
    /// Adds an icon to the system tray.
    /// </summary>
    /// <param name="iconHandle">Handle to the icon (HICON). Caller is responsible for keeping this valid if not owned.</param>
    /// <param name="tooltip">Tooltip text.</param>
    /// <returns>The unique ID of the icon.</returns>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public uint AddIcon(IntPtr iconHandle, string tooltip)
    {
        if (disposed) throw new ObjectDisposedException(nameof(TrayIconManager));

        uint id = nextId++;
        var nid = new SystemPrimitives.NotifyIconData
        {
            cbSize = (uint)Marshal.SizeOf<SystemPrimitives.NotifyIconData>(),
            hWnd = MessageWindowHandle,
            uID = id,
            uFlags = SystemPrimitives.NifMessage | SystemPrimitives.NifIcon | SystemPrimitives.NifTip | SystemPrimitives.NifShowTip,
            uCallbackMessage = SystemPrimitives.WmTrayCallback,
            hIcon = iconHandle,
            uTimeoutOrVersion = SystemPrimitives.NotifyIconVersion4
        };

        // Use the helper method to copy the string into the fixed buffer.
        nid.SetTip(tooltip);

        lock (locker)
        {
            if (!SystemPrimitives.NotifyIcon(SystemPrimitives.NimAdd, ref nid))
            {
                throw new InvalidOperationException("Failed to add system tray icon.");
            }
            // Set version to 4 to receive modern callback behavior
            SystemPrimitives.NotifyIcon(SystemPrimitives.NimSetversion, ref nid);
            icons[id] = nid;
        }
        return id;
    }

    /// <summary>
    /// Updates the icon image for an existing tray icon.
    /// </summary>
    /// <param name="id">The icon ID.</param>
    /// <param name="iconHandle">The new HICON handle.</param>
    public void UpdateIcon(uint id, IntPtr iconHandle)
    {
        lock (locker)
        {
            if (icons.TryGetValue(id, out var nid))
            {
                nid.hIcon = iconHandle;
                nid.uFlags = SystemPrimitives.NifIcon;

                // Note: We do not update the version here, just the visual resource.
                if (!SystemPrimitives.NotifyIcon(SystemPrimitives.NimModify, ref nid))
                {
                    // In high-frequency scenarios, transient failures might occur if the OS is busy.
                    // We intentionally swallow this to prevent crashing the render loop.
                }

                // Update the state to ensure subsequent operations use the current handle
                icons[id] = nid;
            }
        }
    }

    public void RemoveIcon(uint id)
    {
        lock (locker)
        {
            if (icons.TryGetValue(id, out var nid))
            {
                SystemPrimitives.NotifyIcon(SystemPrimitives.NimDelete, ref nid);
                icons.Remove(id);
            }
        }
    }

    /// <summary>
    /// Updates the balloon tip (Legacy/Win7 support).
    /// </summary>
    public void ShowBalloon(uint id, string title, string message, uint iconType = SystemPrimitives.NiifInfo)
    {
        lock (locker)
        {
            if (!icons.TryGetValue(id, out var nid)) return;

            nid.uFlags = SystemPrimitives.NifInfo;
            nid.dwInfoFlags = iconType;

            // Use helper methods for fixed buffers
            nid.SetInfoTitle(title);
            nid.SetInfo(message);

            SystemPrimitives.NotifyIcon(SystemPrimitives.NimModify, ref nid);

            // Update local cache
            icons[id] = nid;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == SystemPrimitives.WmTrayCallback)
        {
            // Correct parsing for NOTIFYICON_VERSION_4
            // In Version 4:
            // lParam (Low Word)  = Mouse Message (e.g., WM_MOUSEMOVE)
            // lParam (High Word) = Icon ID
            // wParam             = Mouse X Coordinate (relative to screen)

            int lParam32 = lParam.ToInt32();
            uint id = (uint)((lParam32 >> 16) & 0xFFFF);
            int mouseMsg = lParam32 & 0xFFFF;

            switch (mouseMsg)
            {
                case SystemPrimitives.WmLButtonUp:
                    // If we just handled a double-click, ignore this trailing mouse-up event.
                    if (handlingDoubleClick)
                    {
                        handlingDoubleClick = false;
                        break;
                    }

                    // Otherwise, start the timer to wait for a potential double-click.
                    singleClickTimer.Stop();
                    pendingSingleClickId = id;
                    singleClickTimer.Start();
                    break;

                case SystemPrimitives.WmLButtonUpDblClk:
                    // A double-click occurred. Stop the single-click timer,
                    // set the flag to ignore the next mouse-up, and fire the event.
                    singleClickTimer.Stop();
                    handlingDoubleClick = true;
                    InvokeEvent(IconDoubleClicked, id);
                    break;

                case SystemPrimitives.WmRButtonUp:
                    InvokeEvent(IconRightClicked, id);
                    break;

                case SystemPrimitives.WmMouseMove:
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
        if (messageWindow.Dispatcher.CheckAccess())
        {
            evt(id);
        }
        else
        {
            messageWindow.Dispatcher.InvokeAsync(() => evt(id));
        }
    }

    // 4. PRIVATE CLEANUP METHOD
    // This contains the actual resource disposal logic. It is private to ensure
    // it can only be called through the controlled Shutdown() path.
    private void DisposeCore()
    {
        if (disposed) return;
        disposed = true;

        // Stop the timer on dispose
        singleClickTimer.Stop();

        lock (locker)
        {
            foreach (var nid in icons.Values)
            {
                var copy = nid; // struct copy
                SystemPrimitives.NotifyIcon(SystemPrimitives.NimDelete, ref copy);
            }
            icons.Clear();
        }
        messageWindow.RemoveHook(WndProc);
        messageWindow.Dispose();
    }
}
using System.Runtime.InteropServices;
using System.Windows;

using NotificationAreaKit.Wpf.Internal.Interop;

namespace NotificationAreaKit.Wpf.Internal;

/// <summary>
/// A special-purpose, borderless window designed to host hover content.
/// </summary>
public partial class HoverPopupWindow : Window
{
    public HoverPopupWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the UIElement to be displayed inside the popup window.
    /// </summary>
    public UIElement Child
    {
        get => ContentHost.Content as UIElement;
        set => ContentHost.Content = value;
    }

    /// <summary>
    /// Intelligently positions and shows the popup window using pure Win32 APIs,
    /// ensuring it is fully visible on the correct monitor and does not overlap the taskbar.
    /// </summary>
    public void ShowAtCursor()
    {
        // Render with zero opacity to measure size without flicker.
        this.Opacity = 0;
        this.Show();
        this.UpdateLayout();

        NativeMethods.GetCursorPos(out var cursorPoint);

        // 1. Get the handle to the monitor the cursor is on.
        IntPtr hMonitor = NativeMethods.MonitorFromPoint(cursorPoint, NativeMethods.MONITOR_DEFAULTTONEAREST);

        // 2. Get the monitor's information, including the crucial "working area".
        var monitorInfo = new NativeMethods.MONITORINFO();
        monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.MONITORINFO));
        NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo);
        var workingArea = monitorInfo.rcWork;

        double targetLeft;
        double targetTop;
        const int offset = 5; // A small buffer from the cursor

        // --- Vertical Positioning Logic ---
        // Is there enough space *below* the cursor in the working area?
        if (cursorPoint.Y + this.ActualHeight + offset < workingArea.Bottom)
        {
            targetTop = cursorPoint.Y + offset;
        }
        else
        {
            targetTop = cursorPoint.Y - this.ActualHeight - offset;
        }

        // --- Horizontal Positioning Logic ---
        // Is there enough space to the *right* of the cursor in the working area?
        if (cursorPoint.X + this.ActualWidth + offset < workingArea.Right)
        {
            targetLeft = cursorPoint.X + offset;
        }
        else
        {
            targetLeft = cursorPoint.X - this.ActualWidth - offset;
        }

        // Apply the calculated position.
        this.Left = targetLeft;
        this.Top = targetTop;

        // Fade the window in.
        this.Opacity = 1;
    }

    /// <summary>
    /// Hides the window when it loses focus.
    /// </summary>
    private void WindowDeactivated(object? sender, EventArgs e)
    {
        this.Hide();
    }
}
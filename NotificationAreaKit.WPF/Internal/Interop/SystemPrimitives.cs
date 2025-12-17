using System.Runtime.InteropServices;

namespace NotificationAreaKit.Wpf.Internal.Interop;

/// <summary>
/// Contains internal P/Invoke definitions for Win32 APIs required by the WPF layer.
/// Uses LibraryImport for optimized, compile-time marshalling where possible.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:Field names should not use Hungarian notation", Justification = "Win32 API conventions.")]
internal static partial class SystemPrimitives
{
    #region NotifyIcon Definitions

    // NotifyIcon Commands
    public const uint NimAdd = 0x00000000;

    public const uint NimModify = 0x00000001;
    public const uint NimDelete = 0x00000002;
    public const uint NimSetversion = 0x00000004;

    // NOTIFYICONDATA Version
    public const uint NotifyIconVersion4 = 4; // Use Windows Vista+ features

    // NOTIFYICONDATA Flags
    public const uint NifMessage = 0x00000001;

    public const uint NifIcon = 0x00000002;
    public const uint NifTip = 0x00000004;
    public const uint NifInfo = 0x00000010; // For Balloon tips
    public const uint NifShowTip = 0x00000080; // Use szTip as the standard tooltip

    // Balloon Icon Types (dwInfoFlags)
    public const uint NiifNone = 0x00000000;

    public const uint NiifInfo = 0x00000001;
    public const uint NiifWarning = 0x00000002;
    public const uint NiifError = 0x00000003;
    public const uint NiifUser = 0x00000004;
    public const uint NiifLargeIcon = 0x00000020;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NotifyIconData
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uTimeoutOrVersion; // Union for uTimeout and uVersion

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NotifyIconIdentifier
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public Guid guidItem;
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconGetRect", SetLastError = true)]
    public static extern int NotifyIconGetRect(ref NotifyIconIdentifier identifier, out Rect iconLocation);

    // DllImport is used here because LibraryImport source generator has issues with complex structs passed by reference.
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIcon", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

    #endregion NotifyIcon Definitions

    #region Window Message Constants

    public const int WmUser = 0x0400;
    public const int WmTrayCallback = WmUser + 1;

    // Mouse Messages
    public const int WmLButtonUp = 0x0202;

    public const int WmRButtonUp = 0x0205;
    public const int WmLButtonUpDblClk = 0x0203;
    public const int WmMouseMove = 0x0200;

    #endregion Window Message Constants

    #region User32 Definitions

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MonitorInfo
    {
        public uint cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out Point lpPoint);

    [LibraryImport("user32.dll")]
    public static partial uint GetDoubleClickTime();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyIcon(IntPtr hIcon);

    [LibraryImport("user32.dll")]
    public static partial IntPtr MonitorFromPoint(Point pt, uint dwFlags);

    public const uint MonitorDefaultToNearest = 0x00000002;

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    #endregion User32 Definitions
}
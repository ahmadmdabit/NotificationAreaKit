// NOTE:
// Do not use CodeMade on this file Ctrl+m, Ctrl+Space
// Beacause it removes the "fixed" from "public fixed char szInfoTitle[64];"

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

    /// <summary>
    /// Blittable version of NOTIFYICONDATA using fixed buffers.
    /// This eliminates marshalling overhead for high-frequency updates.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public unsafe struct NotifyIconData
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        // Fixed buffer for szTip (128 chars)
        public fixed char szTip[128];

        public uint dwState;
        public uint dwStateMask;

        // Fixed buffer for szInfo (256 chars)
        //public fixed char szInfo[256];
        public fixed char szInfo[256];

        public uint uTimeoutOrVersion;

        // Fixed buffer for szInfoTitle (64 chars)
        //public fixed char szInfoTitle[64];
        public fixed char szInfoTitle[64];

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;

        // Helper to set szTip safely
        public void SetTip(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                szTip[0] = '\0';
                return;
            }

            fixed (char* pDest = szTip)
            {
                // Truncate to 127 chars to ensure null terminator
                int length = Math.Min(text.Length, 127);
                text.AsSpan(0, length).CopyTo(new Span<char>(pDest, 128));
                pDest[length] = '\0';
            }
        }

        // Helper to set szInfo safely
        public void SetInfo(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                szInfo[0] = '\0';
                return;
            }

            fixed (char* pDest = szInfo)
            {
                int length = Math.Min(text.Length, 255);
                text.AsSpan(0, length).CopyTo(new Span<char>(pDest, 256));
                pDest[length] = '\0';
            }
        }

        // Helper to set szInfoTitle safely
        public void SetInfoTitle(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                szInfoTitle[0] = '\0';
                return;
            }

            fixed (char* pDest = szInfoTitle)
            {
                int length = Math.Min(text.Length, 63);
                text.AsSpan(0, length).CopyTo(new Span<char>(pDest, 64));
                pDest[length] = '\0';
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NotifyIconIdentifier
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public Guid guidItem;
    }

    // Converted to LibraryImport (Blittable)
    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconGetRect", SetLastError = true)]
    public static partial int NotifyIconGetRect(ref NotifyIconIdentifier identifier, out Rect iconLocation);

    // FIX: Explicitly target the Wide (Unicode) API.
    // LibraryImport does not automatically append 'W' like DllImport does.
    // Calling "Shell_NotifyIcon" binds to "Shell_NotifyIconA" (ANSI), causing:
    // 1. Tooltips truncated to 1 char (first byte of UTF-16 is char, second is null).
    // 2. Events fail because the ANSI API rejects the large Unicode struct size during NIM_SETVERSION.
    // Converted to LibraryImport (Blittable)
    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

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

    #region GDI / Icon Manipulation

    [StructLayout(LayoutKind.Sequential)]
    public struct IconInfo
    {
        // TRUE (1) indicates an Icon, FALSE (0) indicates a Cursor
        public int fIcon;
        public uint xHotspot;
        public uint yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapInfoHeader
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapInfo
    {
        public BitmapInfoHeader bmiHeader;
        public uint bmiColors;
    }

    public const uint DibRgbColors = 0;

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial IntPtr CreateDIBSection(IntPtr hdc, ref BitmapInfo pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr CreateIconIndirect(ref IconInfo piconinfo);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(IntPtr hObject);

    #endregion
}
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using NotificationAreaKit.Core.Interop;

namespace NotificationAreaKit.Core;

/// <summary>
/// Internal helper to create the required Start Menu shortcut for Toast Notifications.
/// This is mandatory for non-MSIX packaged desktop apps to show toasts.
/// </summary>
internal static class ShortcutHelper
{
    // A lock object to ensure thread-safety when checking for and creating the shortcut.
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures a shortcut for the current application exists in the Start Menu,
    /// containing the necessary Application User Model ID (AUMID).
    /// </summary>
    /// <param name="appId">The AUMID for the application.</param>
    /// <param name="appName">The desired name for the shortcut file.</param>
    public static void EnsureShortcutExists(string appId, string appName)
    {
        // This path is the standard location for per-user Start Menu items.
        string shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Start Menu\Programs",
            $"{appName}.lnk");

        // Thread-safe check and creation block.
        lock (_lock)
        {
            if (File.Exists(shortcutPath))
            {
                return;
            }

            var processPath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(processPath)) return;

            // Create the COM object for the shell link.
            NativeMethods.IShellLinkW link = (NativeMethods.IShellLinkW)new NativeMethods.ShellLink();
            link.SetPath(processPath);
            link.SetWorkingDirectory(Path.GetDirectoryName(processPath));

            // Query for the IPropertyStore interface to set the AUMID.
            NativeMethods.IPropertyStore store = (NativeMethods.IPropertyStore)link;
            var pkey = new NativeMethods.PropertyKey { fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid = 5 }; // System.AppUserModel.ID
            var propvar = new NativeMethods.PropVariant();

            try
            {
                propvar.vt = 31; // VT_LPWSTR (a Unicode string)
                propvar.pszVal = Marshal.StringToCoTaskMemUni(appId);

                store.SetValue(ref pkey, ref propvar);
                store.Commit();

                // Save the shortcut file to disk.
                ((IPersistFile)link).Save(shortcutPath, false);
            }
            finally
            {
                // CRITICAL: Clean up unmanaged resources to prevent memory leaks.
                NativeMethods.PropVariantClear(ref propvar);
                Marshal.ReleaseComObject(store);
                Marshal.ReleaseComObject(link);
            }
        }
    }
}
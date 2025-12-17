using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using NotificationAreaKit.Wpf.Internal.Interop;

namespace NotificationAreaKit.Wpf.UI;

/// <summary>
/// Provides WPF-specific UI helper extension methods.
/// </summary>
public static class WpfContextMenuExtensions
{
    /// <summary>
    /// Shows a WPF ContextMenu at the current mouse cursor position.
    /// </summary>
    /// <param name="menu">The context menu to show.</param>
    public static void ShowAtCursor(this ContextMenu menu)
    {
        NativeMethods.GetCursorPos(out var pt);

        menu.Placement = PlacementMode.AbsolutePoint;
        menu.HorizontalOffset = pt.X;
        menu.VerticalOffset = pt.Y;

        // This is required for the menu to open correctly.
        menu.IsOpen = true;
    }
}
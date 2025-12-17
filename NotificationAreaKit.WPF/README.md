# NotificationAreaKit.WPF

A robust, and production-ready .NET library for creating and managing Windows Notification Area (System Tray) icons in WPF applications.

[![.NET Version](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
- [Advanced Usage](#advanced-usage)
- [Architecture](#architecture)
- [Requirements](#requirements)
- [Troubleshooting](#troubleshooting)
- [License](#license)

## Overview

NotificationAreaKit.WPF provides a clean, object-oriented API for WPF applications needing tray icon functionality. It solves common challenges like notification failures, multi-icon management, and Win32 complexity.

## Features

- ✅ **Simple API**: Single `WpfTrayIcon` class for all operations
- ✅ **Bulletproof Notifications**: Automatic Toast/Balloon fallback
- ✅ **Hover Popups**: Custom WPF content on icon hover
- ✅ **Multi-Icon Support**: Manage multiple independent icons
- ✅ **Event-Driven**: Standard click events (Left, Right, Double)
- ✅ **WPF Integration**: Context menu helpers and UI extensions
- ✅ **Cross-Platform**: Windows 7+ compatibility
- ✅ **Production-Ready**: Handles edge cases and resource management

## Installation

### Requirements

- .NET 9.0+
- Windows 7, 10 version 19041+ or Windows 11
- WPF application

## Quick Start

### Basic Tray Icon

```csharp
using NotificationAreaKit.Wpf;

public partial class MainWindow : Window, IDisposable
{
    private WpfTrayIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();

        // Create tray icon
        _trayIcon = new WpfTrayIcon(
            "pack://application:,,,/icon.ico",
            "My App",
            "MyCompany.MyApp",
            "My Application"
        );

        // Handle events
        _trayIcon.LeftClick += OnLeftClick;
        _trayIcon.RightClick += OnRightClick;
    }

    private void OnLeftClick(object? sender, EventArgs e)
    {
        _trayIcon?.ShowNotification("Hello!", "Icon clicked!");
    }

    private void OnRightClick(object? sender, EventArgs e)
    {
        // Show context menu (see advanced usage)
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
    }
}
```

## API Reference

### WpfTrayIcon Class

The main class for managing tray icons.

#### Constructor

```csharp
public WpfTrayIcon(string iconPath, string tooltip, string appId, string appName)
```

- `iconPath`: Pack URI to .ico file (e.g., `"pack://application:,,,/Resources/icon.ico"`)
- `tooltip`: Hover text for the icon
- `appId`: Unique Application User Model ID
- `appName`: Display name for Start Menu shortcut

#### Events

```csharp
public event EventHandler? LeftClick
public event EventHandler? RightClick
public event EventHandler? DoubleClick
```

Standard mouse interaction events.

#### Properties

```csharp
public UIElement? HoverContent { get; set; }
public TimeSpan HoverDelay { get; set; } = TimeSpan.FromMilliseconds(200);
```

- `HoverContent`: WPF UIElement to show on hover
- `HoverDelay`: Delay before showing hover popup

#### Methods

```csharp
public void ShowNotification(string title, string message)
public void ShowBalloon(string title, string message)
public void Dispose()
```

- `ShowNotification()`: Smart notification (Toast on Win10+, Balloon fallback)
- `ShowBalloon()`: Force legacy balloon notification
- `Dispose()`: Remove icon and cleanup resources

### WpfContextMenuExtensions

Helper extensions for WPF UI components.

```csharp
public static void ShowAtCursor(this ContextMenu menu)
```

Shows a ContextMenu at the current cursor position.

## Advanced Usage

### Hover Popups

Display custom WPF content when hovering over the icon:

```csharp
var trayIcon = new WpfTrayIcon(iconPath, tooltip, appId, appName);

// Create custom hover content
var hoverPanel = new StackPanel();
hoverPanel.Children.Add(new TextBlock { Text = "Custom hover content!" });

trayIcon.HoverContent = hoverPanel;
trayIcon.HoverDelay = TimeSpan.FromMilliseconds(500);
```

### Context Menus

Show context menus on right-click:

```csharp
using NotificationAreaKit.Wpf.UI;

private void OnRightClick(object? sender, EventArgs e)
{
    var menu = (ContextMenu)FindResource("TrayContextMenu");
    menu.ShowAtCursor();
}
```

### Multiple Icons

Create and manage multiple tray icons:

```csharp
private List<WpfTrayIcon> _icons = new();

private void AddIcon()
{
    var icon = new WpfTrayIcon(iconPath, tooltip, appId, appName);
    icon.LeftClick += (s, e) => icon.ShowNotification("Icon", "Clicked!");
    _icons.Add(icon);
}

private void Cleanup()
{
    foreach (var icon in _icons)
        icon.Dispose();
    _icons.Clear();
}
```

### Minimize to Tray

Implement the common "minimize to tray" pattern:

```csharp
protected override void OnClosing(CancelEventArgs e)
{
    e.Cancel = true;
    Hide();
    _trayIcon?.ShowNotification("Minimized", "App is running in tray");
}
```

## Architecture

The library uses a **Facade pattern** with layered architecture:

- **Public API**: `WpfTrayIcon` provides simple interface
- **Internal Logic**: `TrayIconManager` singleton handles Win32 interop
- **Core Services**: `NotificationAreaKit.Core` manages notifications and interop
- **UI Extensions**: WPF-specific helpers for common tasks

Key design decisions:
- Singleton `TrayIconManager` for thread-safe multi-icon support
- Strategy pattern for notification services (Toast/Balloon)
- Proper resource disposal and cleanup
- Event-driven architecture for extensibility

## Requirements

- **Runtime**: .NET 9.0 or later
- **OS**: Windows 7, 10 (19041+) or Windows 11
- **Development**: Windows SDK 10.0.19041.0+
- **Dependencies**: System.Drawing.Common, Windows Runtime APIs

## Troubleshooting

### Notifications Not Showing

**First-run failures**: The library handles this automatically. Ensure proper AppID.

**Windows settings**: Check notification settings for your app.

### Icons Not Appearing

**Invalid icon path**: Must be valid Pack URI to .ico file.

**Missing resources**: Ensure icon is embedded or accessible.

### Build Issues

**SDK version**: Confirm .NET 9.0 SDK installed.

**Windows SDK**: Ensure 10.0.19041.0 is available.

**Platform target**: Must target Windows platform.

### Runtime Exceptions

**COM exceptions**: Usually handled, but check AppID uniqueness.

**Win32 errors**: Verify Windows version compatibility.

## License

MIT License - see [LICENSE.txt](../../LICENSE.txt) for details.

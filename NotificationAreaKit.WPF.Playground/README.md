# NotificationAreaKit.WPF.Playground

A comprehensive sample application demonstrating all features and best practices of the `NotificationAreaKit.WPF` library.

[![.NET Version](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

## Table of Contents

- [Overview](#overview)
- [Features Demonstrated](#features-demonstrated)
- [Getting Started](#getting-started)
- [Code Structure](#code-structure)
- [Learning Path](#learning-path)
- [Customization](#customization)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Related Documentation](#related-documentation)
- [License](#license)

## Overview

This playground serves as "living documentation" - a fully functional WPF application that showcases every capability of NotificationAreaKit.WPF. It's designed for developers to:

- **Learn by example**: See real code implementations
- **Test features**: Interact with all library features
- **Copy patterns**: Extract code snippets for your apps
- **Debug issues**: Observe library behavior in real-time

## Features Demonstrated

### Core Functionality

- ✅ **Dynamic Icon Management**: Add/remove multiple tray icons at runtime
- ✅ **Event Handling**: Left-click, right-click, and double-click responses
- ✅ **Smart Notifications**: Automatic Toast/Balloon selection
- ✅ **Legacy Balloons**: Explicit balloon notification triggering
- ✅ **Hover Popups**: Custom WPF content on icon hover

### UI Patterns

- ✅ **Context Menu Integration**: Right-click menu with ShowAtCursor()
- ✅ **Minimize to Tray**: Classic "hide instead of close" behavior
- ✅ **Real-time Logging**: Live event log showing all library actions
- ✅ **Icon Variety**: Multiple icon types and tooltips

### Best Practices

- ✅ **Resource Management**: Proper Dispose() patterns
- ✅ **Error Handling**: Graceful exception management
- ✅ **MVVM Support**: Data binding and commands
- ✅ **App Lifecycle**: Initialization and cleanup

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Windows 10/11 for full feature support

### Running the Playground

From the solution root:

```bash
# Restore dependencies
dotnet restore

# Run the playground
dotnet run --project NotificationAreaKit.WPF.Playground
```

Or from the playground directory:

```bash
cd NotificationAreaKit.WPF.Playground
dotnet run
```

### What You'll See

The application window provides:
- **Control Panel**: Buttons to add/remove icons and toggle features
- **Event Log**: Real-time display of all tray interactions
- **Feature Toggles**: Enable/disable hover popups
- **Status Indicators**: Icon count and active features

## Code Structure

### Key Files

| File | Purpose |
|---|---|
| `MainWindow.xaml` | UI layout and data bindings |
| `MainWindow.xaml.cs` | Core logic and event handlers |
| `App.xaml.cs` | Application initialization |
| `DelegateCommand.cs` | MVVM command implementation |

### Architecture Overview

```csharp
// 1. App Constants (required by library)
private const string AppID = "MyCompany.NotificationKit.Playground";
private const string AppName = "NotificationKit Playground";

// 2. State Management
private readonly List<WpfTrayIcon> _managedIcons = new();
private int _iconCounter = 0;

// 3. MVVM Setup
public ObservableCollection<string> LogMessages { get; } = new();
public ICommand ShowWindowCommand { get; }
public ICommand ExitCommand { get; }
```

### Icon Management Pattern

```csharp
private void AddIcon()
{
    _iconCounter++;
    var iconPath = GetNextIconPath();
    var tooltip = $"My Tray Icon #{_iconCounter}";

    try
    {
        var trayIcon = new WpfTrayIcon(iconPath, tooltip, AppID, AppName);

        // Configure hover content
        if (EnableHoverCheckBox.IsChecked == true)
        {
            trayIcon.HoverContent = (UIElement)FindResource("SampleHoverContent");
        }

        // Wire events
        trayIcon.LeftClick += OnIconLeftClick;
        trayIcon.RightClick += OnIconRightClick;
        trayIcon.DoubleClick += OnIconDoubleClick;

        _managedIcons.Add(trayIcon);
        Log($"SUCCESS: Added '{tooltip}' to notification area");
    }
    catch (Exception ex)
    {
        Log($"ERROR: Failed to create icon. {ex.Message}");
    }
}
```

### Event Handling

```csharp
private void OnIconLeftClick(object? sender, EventArgs e)
{
    if (sender is WpfTrayIcon icon)
    {
        Log($"LEFT CLICK: {icon}"); // icon.ToString() shows tooltip
        icon.ShowNotification("Left Click!", "You left-clicked the icon");
    }
}

private void OnIconRightClick(object? sender, EventArgs e)
{
    if (sender is WpfTrayIcon icon)
    {
        Log($"RIGHT CLICK: {icon}");
        ShowContextMenu();
    }
}
```

### Context Menu Integration

```csharp
private void ShowContextMenu()
{
    var menu = (ContextMenu)FindResource("TrayContextMenu");
    menu.ShowAtCursor();
}

// XAML Menu Definition
<!-- <ContextMenu x:Key="TrayContextMenu">
    <MenuItem Header="Show Window" Command="{Binding ShowWindowCommand}" />
    <MenuItem Header="Exit" Command="{Binding ExitCommand}" />
</ContextMenu> -->
```

### Cleanup Pattern

```csharp
private void RemoveIcon()
{
    if (_managedIcons.Count == 0)
    {
        Log("INFO: No icons to remove.");
        return;
    }

    var lastIcon = _managedIcons[^1];
    _managedIcons.Remove(lastIcon);
    lastIcon.Dispose();
    Log($"REMOVED: Icon disposed and removed from tray");
}

protected override void OnClosed(EventArgs e)
{
    // Clean up all icons
    foreach (var icon in _managedIcons)
        icon.Dispose();
    _managedIcons.Clear();

    base.OnClosed(e);
}
```

## Learning Path

### For Beginners

1. **Run the app** and click "Add Icon"
2. **Interact** with the tray icon (left/right/double click)
3. **Watch the log** to understand event flow
4. **Toggle hover** and see popup behavior
5. **Examine code** in MainWindow.xaml.cs

### For Experienced Developers

1. **Study patterns** for multi-icon management
2. **Review error handling** and logging
3. **Examine MVVM integration** with commands
4. **Check resource management** and disposal
5. **Adapt patterns** for your application

## Customization

### Adding New Icons

Place .ico files in `Resources/Images/Icons/` and update the icon selection logic.

### Modifying Hover Content

Edit the `SampleHoverContent` resource in MainWindow.xaml.

### Extending Events

Add custom event handlers for additional interactions.

## Troubleshooting

### Common Issues

**Icons not appearing**: Check icon paths are valid Pack URIs

**Notifications failing**: Ensure AppID is unique and app has shortcut

**Build errors**: Verify .NET 9.0 SDK and Windows targeting

**Events not firing**: Confirm event handlers are attached before icon creation

### Debug Tips

- **Use the log**: All library actions are logged in real-time
- **Check exceptions**: Errors are caught and displayed
- **Verify resources**: Ensure XAML resources are properly defined
- **Test isolation**: Try features one at a time

## Contributing

This playground is part of the NotificationAreaKit project. To contribute:

1. Test your changes here first
2. Ensure playground still works
3. Update examples if API changes
4. Add new demonstrations for new features

## Related Documentation

- [Main Project README](../../README.md)
- [WPF Library API](../NotificationAreaKit.WPF/README.md)
- [Core Library Internals](../NotificationAreaKit.Core/README.md)

## License

MIT License - see [LICENSE.txt](../../LICENSE.txt) for details.

# NotificationAreaKit

A robust, and production-ready .NET library for creating and managing Windows Notification Area (System Tray) icons in WPF applications.

[![.NET Version](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](#)

## Table of Contents

- [Overview](#overview)
- [The Problem Solved](#the-problem-solved)
- [Project Structure](#project-structure)
- [Requirements](#requirements)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Testing](#testing)
- [Contributing](#contributing)
- [Roadmap](#roadmap)
- [Troubleshooting](#troubleshooting)
- [License](#license)

## Overview

NotificationAreaKit is a comprehensive solution for WPF developers needing reliable tray icon functionality. It provides a clean, object-oriented API that handles the complexities of Windows interop, notification systems, and cross-version compatibility.

Key features include:
- **Simple API**: Single `WpfTrayIcon` class for all tray operations
- **Bulletproof Notifications**: Automatic Toast/Balloon fallback
- **Hover Popups**: Custom WPF content on hover
- **Multi-Icon Support**: Manage multiple independent icons
- **Event-Driven**: Standard click and double-click events
- **Production-Ready**: Handles edge cases like first-run failures

## The Problem Solved

Creating production-quality tray applications is notoriously complex. This library addresses critical challenges:

- **First-Run Notification Failures**: Eliminates `COMException` on initial toast notifications
- **Multi-Icon Management**: Clean lifecycle management for multiple icons
- **Win32 Complexity**: Abstracts P/Invoke and message handling
- **OS Compatibility**: Seamless support from Windows 7 to 11
- **Resource Management**: Proper disposal and singleton patterns

## Project Structure

The solution uses a layered architecture for maintainability:

| Project | Description |
|---|---|
| `NotificationAreaKit.Core` | An internal, platform-agnostic library containing low-level WinRT/Win32 interop for notifications and shortcut management. **Not for direct consumption.** |
| `NotificationAreaKit.WPF` | The public-facing library for WPF developers. It provides the main `WpfTrayIcon` class and is intended for distribution as a NuGet package. |
| `NotificationAreaKit.WPF.Playground` | A sample application that demonstrates all features of the library and serves as a best-practice guide for consumers. |

## Requirements

- **.NET SDK**: 9.0 or later
- **Platform**: Windows 7 or newer
- **Development**: Windows 7/10/11 for full feature testing

## Getting Started

### Quick Start

1. **Clone and build**:
   ```bash
   git clone https://github.com/ahmadmdabit/NotificationAreaKit.git
   cd NotificationAreaKit
   dotnet restore && dotnet build
   ```

2. **Run the playground**:
   ```bash
   dotnet run --project NotificationAreaKit.WPF.Playground
   ```

3. **Basic usage**:
```csharp
using NotificationAreaKit.Wpf;

var trayIcon = new WpfTrayIcon(
    "pack://application:,,,/icon.ico",
    "My App",
    "MyCompany.MyApp",
    "My Application"
);

trayIcon.LeftClick += (s, e) => trayIcon.ShowNotification("Hello!", "Clicked!");
```

## Usage

See the [WPF Library README](NotificationAreaKit.WPF/README.md) for detailed API documentation and examples.

## Testing

Run tests with:
```bash
dotnet test
```

The solution includes unit tests for core logic and integration tests for UI components.

## Contributing

We welcome contributions! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for new functionality
4. Ensure all tests pass (`dotnet test`)
5. Follow existing code style and patterns
6. Submit a pull request

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## Roadmap

- [ ] Advanced notification templates
- [ ] Localization support
- [ ] Theming integration

## Troubleshooting

### Common Issues

**Notifications not showing on first run**:
- Ensure proper AppID and shortcut setup (handled automatically by the library)

**Icons not appearing**:
- Verify icon path is a valid Pack URI
- Check Windows notification settings

**Build failures**:
- Confirm .NET 9.0 SDK is installed
- Ensure Windows SDK 10.0.19041.0 is available

### Getting Help

- Check existing [GitHub Issues](https://github.com/ahmadmdabit/NotificationAreaKit/issues)
- Review the [Playground App](NotificationAreaKit.WPF.Playground/) for examples
- Open a new issue for bugs or feature requests

## License

MIT License - see [LICENSE.txt](LICENSE.txt) for details.

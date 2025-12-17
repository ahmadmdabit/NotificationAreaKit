# NotificationAreaKit.Core

Internal, platform-agnostic core library for the NotificationAreaKit solution.

> **⚠️ Warning**
> This is an internal support library and is **not intended for direct consumption**. Its API is not guaranteed to be stable and may change without notice. Please use the `NotificationAreaKit.WPF` package instead.

## Table of Contents

- [Overview](#overview)
  - [Design Principles](#design-principles)
  - [Key Components](#key-components)
  - [Visibility and Dependencies](#visibility-dand-dependencies)
- [Implementation Details](#implementation-details)
  - [Notification Services](#notification-services)
  - [Win32 Interop](#win32-interop)
  - [Error Handling](#error-handling)
- [Development Notes](#development-notes)
  - [Building](#building)
  - [Testing](#testing)
- [License](#license)
## Overview

This project provides the foundational, UI-framework-agnostic logic for Windows Notification Area integration. It encapsulates low-level interactions with Windows APIs, ensuring reliability and cross-version compatibility.

## Architecture

### Design Principles

- **Separation of Concerns**: Strictly limited to Windows API interactions
- **Platform Agnostic**: No UI framework dependencies
- **Internal Visibility**: All public types are `internal` with controlled exposure

### Key Components

| Component | Purpose |
|---|---|
| **ToastNotificationService** | Modern WinRT toast notifications for Windows 10+ |
| **ShortcutHelper** | COM interop for Start Menu shortcut management |
| **Interop Namespace** | P/Invoke definitions and native method signatures |
| **INotificationService** | Abstraction for notification strategies |

### Visibility and Dependencies

- All types are `internal` by default
- Exposed to `NotificationAreaKit.WPF` via `[InternalsVisibleTo]`
- Dependencies: .NET 9.0 SDK, Windows SDK 10.0.19041.0

## Implementation Details

### Notification Services

The library implements a strategy pattern for notifications:

- **ToastNotificationService**: Uses Windows.UI.Notifications for modern toasts
- **ResilientNotificationService**: Wraps toast service with fallback logic
- **Balloon Notifications**: Legacy support via Shell_NotifyIcon

### Win32 Interop

Handles complex Win32 APIs:
- Icon management with Shell_NotifyIcon
- Message loop integration
- Cursor position tracking
- Rectangle calculations for hover detection

### Error Handling

Robust error handling for:
- COM exceptions on first-run notifications
- Invalid icon resources
- Missing Windows APIs on older versions

## Development Notes

This library is not meant for direct use. For WPF applications, use `NotificationAreaKit.WPF` instead.

### Building

```bash
dotnet build NotificationAreaKit.Core.csproj
```

### Testing

Core logic is tested via the WPF project's test suite, as the core is internal-only.

## License

MIT License - see [LICENSE.txt](../../LICENSE.txt) for details.

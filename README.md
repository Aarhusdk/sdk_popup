# sdk_popup

Toast notification COM control for Clarion using DevExpress AlertControl.

## Overview

This is a ClarionCOM control that provides toast/popup notifications for Clarion for Windows applications. It uses the DevExpress `AlertControl` component to display professional-looking notification popups with configurable title, message, duration, position, and type.

**ProgID:** `sdk_popup.sdk_popupControl`

## Requirements

- .NET Framework 4.7.2 or later
- Clarion 11.0 or later
- DevExpress WinForms (v24+)

## Features

- Show toast notifications with title and message
- Configurable display duration (default 3000ms)
- Multiple screen positions (TopLeft, TopRight, BottomLeft, BottomRight, etc.)
- Notification types: Info, Success, Warning, Error
- Custom background and text colors (hex format)
- Programmatic dismiss
- Events for notification shown, dismissed, and clicked

## Installation

Copy the contents of the `Clarion/accessory/` folder to your Clarion accessory folder:
- `bin/sdk_popup.dll` - The compiled control
- `bin/DevExpress.*.dll` - DevExpress dependency DLLs
- `resources/sdk_popup.manifest` - Registration-free COM manifest
- `resources/sdk_popup.sdk_popupControl.*` - Metadata files

## API Reference

### Methods

| Method | Parameters | Description |
|--------|-----------|-------------|
| `ShowNotification` | message (STRING) | Shows a toast notification with the specified message |
| `ShowNotificationWithTitle` | title (STRING), message (STRING) | Shows a toast notification with title and message |
| `SetTitle` | title (STRING) | Sets the notification title text |
| `GetTitle` | - | Gets the current notification title text |
| `SetMessage` | message (STRING) | Sets the notification message text |
| `GetMessage` | - | Gets the current notification message text |
| `SetDuration` | milliseconds (LONG) | Sets the display duration in milliseconds (default: 3000) |
| `GetDuration` | - | Gets the current display duration in milliseconds |
| `SetPosition` | position (STRING) | Sets position: TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight |
| `GetPosition` | - | Gets the current notification position |
| `SetNotificationType` | notificationType (STRING) | Sets type: Info, Success, Warning, Error |
| `GetNotificationType` | - | Gets the current notification type |
| `SetBackgroundColor` | hexColor (STRING) | Sets the background color (hex format, e.g. #FF6600) |
| `GetBackgroundColor` | - | Gets the current background color |
| `SetTextColor` | hexColor (STRING) | Sets the text color (hex format) |
| `GetTextColor` | - | Gets the current text color |
| `DismissNotification` | - | Dismisses any currently visible notification |
| `GetIsVisible` | - | Returns whether a notification is currently visible |
| `About` | - | Displays control name and version information |

### Events

| Event | Parameters | Description |
|-------|-----------|-------------|
| `NotificationShown` | title (STRING), message (STRING) | Fired when a notification is shown |
| `NotificationDismissed` | reason (STRING) | Fired when a notification is dismissed (Timeout, UserDismissed, Programmatic) |
| `NotificationClicked` | title (STRING) | Fired when the user clicks on the notification |

## Building from Source

1. Clone this repository
2. Open in Visual Studio or use Claude Code
3. Build in Release mode

Or use Claude Code:
```
/ClarionCOM
```
Then select "Build existing project".

## License

MIT License - see [LICENSE](LICENSE) for details.

## Links

- [COM for Clarion Documentation](https://clarionlive.com/com_for_clarion)
- [COM Marketplace](https://clarionlive.com/com_for_clarion/marketplace)

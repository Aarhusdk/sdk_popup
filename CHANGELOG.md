# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [1.6.0] - 2026-10-13

### Removed
- **DevExpress dependency.** The `Default` style no longer uses DevExpress `AlertControl`; it is now drawn by the control's own `StyledNotificationForm` (Light look). The `DevExpress.Win` package reference is gone, so no DevExpress DLLs or license are needed. The COM interface (methods, DispIds, events) is unchanged.

### Changed
- HTML text (`ShowNotificationHtml*`, `SetHtmlEnabled`) is converted to plain text for every style: `<br>` becomes a line break, other tags are removed, entities (`&amp;` etc.) are decoded. Bold/colour markup is no longer rendered.
- `SetMaxVisibleNotifications`, positions (incl. Center/TopCenter/BottomCenter), hover-pause, progress and in-place updates now use the same stacking logic for all styles.
- `ShowNotification` / `ShowNotificationWithTitle` (the non-Ex methods) now go through the same stack as the Ex methods, so they no longer overlap other notifications.

### Fixed
- `DismissNotification`, `DismissAllNotifications` and disposal did not close styled notification windows (only image-only ones).

## [1.5.0] - 2026-04-16

### Added — 7 new features (completes top-10 roadmap)

- **Action buttons + ActionButtonClicked event**: Add clickable buttons to notifications via `AddActionButton(id, caption, isPrimary)`. Up to 3 recommended. Fires `ActionButtonClicked(notificationId, buttonId)` when clicked. Works in both DevExpress AlertControl (via `Buttons` collection) and styled forms (GDI+ rendered with hover state). `ClearActionButtons()` resets. `GetActionButtonCount()` returns current count.
- **HTML/rich text**: `ShowNotificationHtml(message)`, `ShowNotificationWithTitleHtml(title, message)`, `ShowNotificationHtmlEx(title, message)` return notification ID. `SetHtmlEnabled(bool)` toggles HTML rendering globally. Default style renders HTML natively via DevExpress `AllowHtmlText`; styled forms strip HTML tags and render plain text.
- **Multi-monitor**: `SetDisplayMonitor(index)` (-1 = auto-follow owner window, 0+ = specific monitor). `GetMonitorCount()` returns total monitors. All position calculations updated (`PositionImageOnlyForm`, `ReflowImageForms`, `RepositionAlertForms`).
- **ShowPending / Resolve / Reject pattern**: `ShowPending(title, message)` creates pinned Info notification with 0% progress and returns ID. `ResolveNotification(id, successMessage)` swaps to Success + 100% progress + auto-dismisses after 3 sec. `RejectNotification(id, errorMessage)` swaps to Error, stays pinned. Perfect for long-running Clarion operations (SQL, PDF, email).
- **Do Not Disturb mode**: `SetDoNotDisturb(bool)` manual toggle. `SetDoNotDisturbMinutes(n)` timer-based auto-expire. `SetDoNotDisturbSchedule(startHHMM, endHHMM)` supports overnight windows (e.g. "22:00"-"07:00"). `GetDoNotDisturbReason()` returns `"manual"` / `"timer"` / `"scheduled"` / `""`. Suppressed notifications are logged to history if enabled.
- **Notification history**: `SetHistoryEnabled(bool)`, `SetHistoryMaxSize(n)` (default 50), `GetHistoryCount()`, `GetHistoryItemJson(index)` (0 = newest; returns `{"timestamp","title","message","type","reason"}` JSON), `ClearHistory()`. Captures dismissed notifications + DND-suppressed ones.
- **JSON presets**: `SavePreset(name)` persists current settings (style/type/position/duration/opacity/pinned/colors/sound/hover) to `%APPDATA%\sdk_popup\presets.json`. `LoadPreset(name)`, `DeletePreset(name)`, `ListPresets()`. `ShowFromJson(jsonConfig)` shows a notification directly from a JSON config string — perfect for template-driven notifications from Clarion.

### New events
- `ActionButtonClicked(notificationId, buttonId)` — fires when user clicks an action button

### New helper classes (internal)
- `NotificationHistory` — fixed-size ring buffer with JSON export
- `PresetManager` — named-preset persistence with minimal JSON parser
- `DoNotDisturbManager` — manual/timer/scheduled DND state machine
- `ActionButton` — button configuration data
- `MiniJson` — ~120-line JSON parser (avoids `System.Web.Extensions` dependency)

### Changed
- Manifest version bumped 1.4.0.0 → 1.5.0.0
- `RepositionAlertForms` now handles all 7 positions (not just center) — needed for multi-monitor support with any position.
- DevExpress AlertControl.AllowHtmlText set to True by default (non-HTML text still displays correctly as plain).

### Compatibility
All additions are opt-in. Existing v1.4 code works unchanged.

## [1.4.0] - 2026-04-16

### Added
- **Hover-pause**: the auto-dismiss timer now pauses while the mouse is over a notification and resumes when it leaves. Enabled by default. New API: `SetHoverPause(bool)` / `GetHoverPause()`.
  - Works for all three form types: DevExpress AlertForm (via reflection on internal timer — silently no-ops if the timer field cannot be located on the current DevExpress version), `StyledNotificationForm` (Dark/Light/Rounded/Minimal), and image-only forms.
- **Sound presets**: new `SetSoundPreset(name)` / `GetSoundPreset()` methods with built-in presets: `Success` (Asterisk), `Error` (Hand), `Warning` (Exclamation), `Chime` (Windows chimes.wav), `Ding` (Windows ding.wav), `Silent`. Preset takes precedence over `SetSoundPath` when both are set. No WAV files or embedded resources required — uses `SystemSounds` and `%WINDIR%\Media\*.wav`.
- **Deduplication**: new `SetDeduplication(bool)` / `GetDeduplication()`. When enabled, calling a tracked (`*Ex`) show method with a title+message that matches a currently visible notification updates the existing one in-place (via `UpdateNotification`) and returns its ID instead of creating a duplicate. Only applies to tracked methods with text content (not image-only). Opt-in, default off.
- **Rate limiting**: new `SetRateLimit(maxPerSecond)` / `GetRateLimit()`. Limits how many notifications can be shown per second. Excess calls within a 1-second window are dropped (logged via `Trace.WriteLine`). Applies to all show paths (legacy and tracked). 0 = unlimited (default).

### Changed
- Manifest version bumped 1.3.0.0 → 1.4.0.0.

### Compatibility
- All additions are opt-in or default to preserving v1.3 behavior, except hover-pause which defaults to ON. This is an intentional UX improvement — call `SetHoverPause(false)` to restore v1.3 behavior if needed.

## [1.3.0] - 2026-02-14

### Added
- **Style/Theme System**: SetStyle/GetStyle/GetAvailableStyles for switching notification appearance
- **5 Built-in Styles**:
  - `Default` - Original DevExpress AlertControl (unchanged, backwards compatible)
  - `Dark` - Dark background, white text, colored accent bar, rounded corners
  - `Light` - White background, dark text, colored accent bar, subtle border, rounded corners
  - `Rounded` - Gradient background in type color, white text, large rounded corners
  - `Minimal` - Compact, no icon, colored bottom border, no close button
- Type-based accent colors: Info (blue), Success (green), Warning (orange), Error (red)
- Custom GDI+ rendered notification forms with fade-in/fade-out animation
- Drop shadow support on styled forms
- Full stacking, in-place update, and progress bar support for all styles
- SetBackgroundColor/SetTextColor overrides work with all styles

## [1.1.0] - 2026-02-09

### Added
- Image size control: SetImageSize/GetImageWidth/GetImageHeight (custom display dimensions)
- Image-only notifications: ShowImageNotification() displays floating image without title, message, or chrome
- Transparent borderless form for image-only mode (no background artifacts)

### Fixed
- Center positioning reliability improved (disabled animation for center positions)
- ShowImageNotification now shows only the image with full transparency (no magenta/color bleeding)

## [1.0.5] - 2026-02-09

### Fixed
- Center positioning now works correctly (TopCenter, BottomCenter, Center) - uses deferred BeginInvoke to override DevExpress internal positioning

### Added
- Sound support: SetSoundEnabled/GetSoundEnabled, SetSoundPath/GetSoundPath (system sound or custom WAV)
- Opacity control: SetOpacity/GetOpacity (10-100% transparency)
- Pinned notifications: SetPinned/GetPinned (sticky mode - stays until manually dismissed)
- Custom image support: SetImagePath/GetImagePath/ClearImage
- Center positioning support (Center, TopCenter, BottomCenter)

## [1.0.0] - 2026-02-09

- Initial release
- Toast notification display with configurable title, message, duration, and position
- DevExpress AlertControl-based notifications
- Support for Info, Success, Warning, Error notification types
- Configurable background and text colors
- Events: NotificationShown, NotificationDismissed, NotificationClicked

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

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

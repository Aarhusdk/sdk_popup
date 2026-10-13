using System;
using System.Runtime.InteropServices;

namespace sdk_popup
{
    /// <summary>
    /// Main COM interface for the sdk_popup toast notification control.
    /// Defines all methods that Clarion can call via getter/setter pattern.
    /// </summary>
    [ComVisible(true)]
    [Guid("0030A89E-B1EF-45C7-95D4-2BAA408C2FB0")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface Isdk_popup
    {
        /// <summary>
        /// Shows a toast notification with the specified message
        /// </summary>
        [DispId(1)]
        void ShowNotification(string message);

        /// <summary>
        /// Shows a toast notification with title and message
        /// </summary>
        [DispId(2)]
        void ShowNotificationWithTitle(string title, string message);

        /// <summary>
        /// Sets the notification title text
        /// </summary>
        [DispId(3)]
        void SetTitle(string title);

        /// <summary>
        /// Gets the current notification title text
        /// </summary>
        [DispId(4)]
        string GetTitle();

        /// <summary>
        /// Sets the notification message text
        /// </summary>
        [DispId(5)]
        void SetMessage(string message);

        /// <summary>
        /// Gets the current notification message text
        /// </summary>
        [DispId(6)]
        string GetMessage();

        /// <summary>
        /// Sets the display duration in milliseconds (default: 3000)
        /// </summary>
        [DispId(7)]
        void SetDuration(int milliseconds);

        /// <summary>
        /// Gets the current display duration in milliseconds
        /// </summary>
        [DispId(8)]
        int GetDuration();

        /// <summary>
        /// Sets the notification position: TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight, Center
        /// </summary>
        [DispId(9)]
        void SetPosition(string position);

        /// <summary>
        /// Gets the current notification position
        /// </summary>
        [DispId(10)]
        string GetPosition();

        /// <summary>
        /// Sets the notification type: Info, Success, Warning, Error
        /// </summary>
        [DispId(11)]
        void SetNotificationType(string notificationType);

        /// <summary>
        /// Gets the current notification type
        /// </summary>
        [DispId(12)]
        string GetNotificationType();

        /// <summary>
        /// Sets the background color for the notification (hex format, e.g. #FF6600)
        /// </summary>
        [DispId(13)]
        void SetBackgroundColor(string hexColor);

        /// <summary>
        /// Gets the current background color
        /// </summary>
        [DispId(14)]
        string GetBackgroundColor();

        /// <summary>
        /// Sets the text color for the notification (hex format)
        /// </summary>
        [DispId(15)]
        void SetTextColor(string hexColor);

        /// <summary>
        /// Gets the current text color
        /// </summary>
        [DispId(16)]
        string GetTextColor();

        /// <summary>
        /// Sets the image from a file path (PNG, JPG, BMP, ICO)
        /// </summary>
        [DispId(20)]
        void SetImagePath(string filePath);

        /// <summary>
        /// Gets the current image file path
        /// </summary>
        [DispId(21)]
        string GetImagePath();

        /// <summary>
        /// Clears the custom image, reverting to the notification type icon
        /// </summary>
        [DispId(22)]
        void ClearImage();

        /// <summary>
        /// Dismisses any currently visible notification
        /// </summary>
        [DispId(17)]
        void DismissNotification();

        /// <summary>
        /// Returns whether a notification is currently visible
        /// </summary>
        [DispId(18)]
        bool GetIsVisible();

        /// <summary>
        /// Displays control name and version information in a MessageBox
        /// </summary>
        [DispId(19)]
        void About();

        // --- Sound ---

        /// <summary>
        /// Enables or disables playing a sound when a notification is shown
        /// </summary>
        [DispId(23)]
        void SetSoundEnabled(bool enabled);

        /// <summary>
        /// Gets whether sound is enabled
        /// </summary>
        [DispId(24)]
        bool GetSoundEnabled();

        /// <summary>
        /// Sets a custom WAV file path for the notification sound. Empty string uses the default system sound.
        /// </summary>
        [DispId(25)]
        void SetSoundPath(string wavFilePath);

        /// <summary>
        /// Gets the current custom WAV file path
        /// </summary>
        [DispId(26)]
        string GetSoundPath();

        // --- Opacity ---

        /// <summary>
        /// Sets the notification opacity (0-100, where 100 is fully opaque). Default: 100
        /// </summary>
        [DispId(27)]
        void SetOpacity(int percent);

        /// <summary>
        /// Gets the current notification opacity (0-100)
        /// </summary>
        [DispId(28)]
        int GetOpacity();

        // --- Sticky/Pinned ---

        /// <summary>
        /// Sets whether the notification stays visible until manually dismissed (true = sticky)
        /// </summary>
        [DispId(29)]
        void SetPinned(bool pinned);

        /// <summary>
        /// Gets whether notifications are pinned (sticky)
        /// </summary>
        [DispId(30)]
        bool GetPinned();

        // --- Image Size ---

        /// <summary>
        /// Sets the display size for the notification image in pixels. 0,0 = original size.
        /// </summary>
        [DispId(31)]
        void SetImageSize(int width, int height);

        /// <summary>
        /// Gets the configured image display width (0 = original)
        /// </summary>
        [DispId(32)]
        int GetImageWidth();

        /// <summary>
        /// Gets the configured image display height (0 = original)
        /// </summary>
        [DispId(33)]
        int GetImageHeight();

        /// <summary>
        /// Shows a notification with only an image (no title or message text).
        /// Call SetImagePath first to set the image.
        /// </summary>
        [DispId(34)]
        void ShowImageNotification();

        // --- Notification Stacking (v1.2.0) ---

        /// <summary>
        /// Shows a notification using current settings and returns a notification ID
        /// </summary>
        [DispId(40)]
        string ShowNotificationEx();

        /// <summary>
        /// Shows a notification with title and message, returns a notification ID
        /// </summary>
        [DispId(41)]
        string ShowNotificationWithTitleEx(string title, string message);

        /// <summary>
        /// Dismisses a specific notification by its ID
        /// </summary>
        [DispId(42)]
        void DismissNotificationById(string notificationId);

        /// <summary>
        /// Sets the maximum number of simultaneously visible notifications (0 = unlimited)
        /// </summary>
        [DispId(43)]
        void SetMaxVisibleNotifications(int maxCount);

        /// <summary>
        /// Gets the maximum number of simultaneously visible notifications
        /// </summary>
        [DispId(44)]
        int GetMaxVisibleNotifications();

        /// <summary>
        /// Gets the number of currently active (visible) notifications
        /// </summary>
        [DispId(45)]
        int GetActiveNotificationCount();

        /// <summary>
        /// Gets the number of queued notifications waiting to be shown
        /// </summary>
        [DispId(46)]
        int GetQueuedNotificationCount();

        /// <summary>
        /// Dismisses all active and queued notifications
        /// </summary>
        [DispId(47)]
        void DismissAllNotifications();

        /// <summary>
        /// Shows an image-only notification with stacking support, returns a notification ID
        /// </summary>
        [DispId(48)]
        string ShowImageNotificationEx();

        // --- Auto-Update (v1.2.0) ---

        /// <summary>
        /// Updates the title and message of an existing notification in-place.
        /// Returns true if the notification was found and updated.
        /// </summary>
        [DispId(49)]
        bool UpdateNotification(string notificationId, string title, string message);

        /// <summary>
        /// Updates the progress percentage of an existing notification (0-100).
        /// Returns true if the notification was found and updated.
        /// </summary>
        [DispId(50)]
        bool UpdateProgress(string notificationId, int percent);

        /// <summary>
        /// Updates the title, message, and progress of an existing notification in one call.
        /// Returns true if the notification was found and updated.
        /// </summary>
        [DispId(51)]
        bool UpdateNotificationFull(string notificationId, string title, string message, int percent);

        // --- Style/Theme (v1.3.0) ---

        /// <summary>
        /// Sets the notification style: Default, Dark, Light, Rounded, Minimal.
        /// All styles are drawn by the control's own notification form (no third-party UI library).
        /// </summary>
        [DispId(52)]
        void SetStyle(string styleName);

        /// <summary>
        /// Gets the current notification style name
        /// </summary>
        [DispId(53)]
        string GetStyle();

        /// <summary>
        /// Returns a comma-separated list of available style names
        /// </summary>
        [DispId(54)]
        string GetAvailableStyles();

        // --- Sound Preset (v1.4.0) ---

        /// <summary>
        /// Sets a built-in sound preset: Success, Error, Warning, Chime, Ding, Silent.
        /// Takes precedence over SetSoundPath when both are set.
        /// </summary>
        [DispId(55)]
        void SetSoundPreset(string preset);

        /// <summary>
        /// Gets the current sound preset name (empty string if none)
        /// </summary>
        [DispId(56)]
        string GetSoundPreset();

        // --- Deduplication & Rate Limiting (v1.4.0) ---

        /// <summary>
        /// Enables deduplication: showing a notification with the same title+message
        /// as a currently visible one will update the existing notification in-place
        /// instead of creating a new one. Only applies to tracked (*Ex) methods.
        /// </summary>
        [DispId(57)]
        void SetDeduplication(bool enabled);

        /// <summary>
        /// Gets whether deduplication is enabled
        /// </summary>
        [DispId(58)]
        bool GetDeduplication();

        /// <summary>
        /// Sets the maximum number of notifications allowed per second.
        /// Excess notifications within the same 1-second window are dropped.
        /// 0 = no limit (default).
        /// </summary>
        [DispId(59)]
        void SetRateLimit(int maxPerSecond);

        /// <summary>
        /// Gets the current rate limit (0 = unlimited)
        /// </summary>
        [DispId(60)]
        int GetRateLimit();

        // --- Hover Pause (v1.4.0) ---

        /// <summary>
        /// Enables pausing the auto-dismiss timer while the mouse is over the notification.
        /// Timer resumes when the mouse leaves. Default: enabled.
        /// </summary>
        [DispId(61)]
        void SetHoverPause(bool enabled);

        /// <summary>
        /// Gets whether hover-pause is enabled
        /// </summary>
        [DispId(62)]
        bool GetHoverPause();

        // --- HTML Rich Text (v1.5.0) ---

        /// <summary>
        /// Shows a notification with HTML-formatted message text.
        /// HTML is converted to plain text: br-tags become line breaks, other tags are removed,
        /// entities are decoded.
        /// </summary>
        [DispId(63)]
        void ShowNotificationHtml(string htmlMessage);

        /// <summary>
        /// Shows a notification with HTML title and message
        /// </summary>
        [DispId(64)]
        void ShowNotificationWithTitleHtml(string title, string htmlMessage);

        /// <summary>
        /// Shows a notification with HTML content and returns a notification ID
        /// </summary>
        [DispId(65)]
        string ShowNotificationHtmlEx(string title, string htmlMessage);

        /// <summary>
        /// Enables or disables HTML rendering for all subsequent notifications
        /// </summary>
        [DispId(66)]
        void SetHtmlEnabled(bool enabled);

        /// <summary>
        /// Gets whether HTML rendering is enabled
        /// </summary>
        [DispId(67)]
        bool GetHtmlEnabled();

        // --- Multi-Monitor (v1.5.0) ---

        /// <summary>
        /// Sets the display monitor: -1 = auto (follow owner window), 0+ = specific monitor index
        /// </summary>
        [DispId(68)]
        void SetDisplayMonitor(int monitorIndex);

        /// <summary>
        /// Gets the configured display monitor index
        /// </summary>
        [DispId(69)]
        int GetDisplayMonitor();

        /// <summary>
        /// Gets the number of monitors available on the system
        /// </summary>
        [DispId(70)]
        int GetMonitorCount();

        // --- ShowPending / Resolve / Reject (v1.5.0) ---

        /// <summary>
        /// Shows a pinned "in progress" notification with initial 0% progress.
        /// Use for long-running operations. Returns a notification ID for Resolve/Reject.
        /// </summary>
        [DispId(71)]
        string ShowPending(string title, string message);

        /// <summary>
        /// Resolves a pending notification with success state.
        /// Changes type to Success, sets progress 100%, unpins, auto-dismisses.
        /// </summary>
        [DispId(72)]
        bool ResolveNotification(string notificationId, string successMessage);

        /// <summary>
        /// Rejects a pending notification with error state.
        /// Changes type to Error. Stays pinned so user must acknowledge.
        /// </summary>
        [DispId(73)]
        bool RejectNotification(string notificationId, string errorMessage);

        // --- Do Not Disturb (v1.5.0) ---

        /// <summary>
        /// Enables or disables Do Not Disturb mode.
        /// When DND is active, all notifications are suppressed and logged to history (if enabled).
        /// </summary>
        [DispId(74)]
        void SetDoNotDisturb(bool enabled);

        /// <summary>
        /// Gets whether DND is currently active (checks manual, timer, and schedule)
        /// </summary>
        [DispId(75)]
        bool GetDoNotDisturb();

        /// <summary>
        /// Enables DND for the specified number of minutes, then auto-disables.
        /// 0 = disable timer.
        /// </summary>
        [DispId(76)]
        void SetDoNotDisturbMinutes(int minutes);

        /// <summary>
        /// Sets a daily DND schedule (e.g. "22:00" to "07:00" for night-time quiet).
        /// Times are in 24h HH:MM format. Pass empty strings to clear.
        /// </summary>
        [DispId(77)]
        void SetDoNotDisturbSchedule(string startHHMM, string endHHMM);

        /// <summary>
        /// Clears the DND schedule (equivalent to SetDoNotDisturbSchedule("",""))
        /// </summary>
        [DispId(78)]
        void ClearDoNotDisturbSchedule();

        /// <summary>
        /// Returns why DND is active: "manual", "timer", "scheduled", or "" (not active)
        /// </summary>
        [DispId(79)]
        string GetDoNotDisturbReason();

        // --- Notification History (v1.5.0) ---

        /// <summary>
        /// Enables or disables notification history logging (default: false)
        /// </summary>
        [DispId(80)]
        void SetHistoryEnabled(bool enabled);

        /// <summary>
        /// Gets whether history logging is enabled
        /// </summary>
        [DispId(81)]
        bool GetHistoryEnabled();

        /// <summary>
        /// Sets maximum number of history entries to retain (default: 50)
        /// </summary>
        [DispId(82)]
        void SetHistoryMaxSize(int maxSize);

        /// <summary>
        /// Gets the number of notifications currently in history
        /// </summary>
        [DispId(83)]
        int GetHistoryCount();

        /// <summary>
        /// Returns a JSON-formatted history entry. Index 0 = newest, higher = older.
        /// Returns empty string if index is out of range.
        /// Fields: {"timestamp":"...","title":"...","message":"...","type":"...","reason":"..."}
        /// </summary>
        [DispId(84)]
        string GetHistoryItemJson(int index);

        /// <summary>
        /// Clears all history entries
        /// </summary>
        [DispId(85)]
        void ClearHistory();

        // --- JSON Presets (v1.5.0) ---

        /// <summary>
        /// Saves current notification settings (style, type, position, duration, etc.) as a named preset.
        /// Presets persist to %APPDATA%\sdk_popup\presets.json
        /// </summary>
        [DispId(86)]
        void SavePreset(string name);

        /// <summary>
        /// Loads a preset by name, applying all its settings.
        /// Returns true if the preset was found.
        /// </summary>
        [DispId(87)]
        bool LoadPreset(string name);

        /// <summary>
        /// Deletes a preset by name. Returns true if found and deleted.
        /// </summary>
        [DispId(88)]
        bool DeletePreset(string name);

        /// <summary>
        /// Returns a comma-separated list of saved preset names
        /// </summary>
        [DispId(89)]
        string ListPresets();

        /// <summary>
        /// Shows a notification from a JSON configuration string.
        /// JSON fields: title, message, style, type, position, duration, opacity, pinned.
        /// Returns notification ID.
        /// </summary>
        [DispId(90)]
        string ShowFromJson(string jsonConfig);

        // --- Action Buttons (v1.5.0) ---

        /// <summary>
        /// Adds a clickable action button to upcoming notifications.
        /// Buttons persist until ClearActionButtons is called.
        /// Max 3 buttons recommended for readability.
        /// </summary>
        [DispId(91)]
        void AddActionButton(string buttonId, string caption, bool isPrimary);

        /// <summary>
        /// Removes all action buttons
        /// </summary>
        [DispId(92)]
        void ClearActionButtons();

        /// <summary>
        /// Gets the current number of configured action buttons
        /// </summary>
        [DispId(93)]
        int GetActionButtonCount();
    }
}

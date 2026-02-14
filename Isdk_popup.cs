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
        /// Default uses DevExpress AlertControl. Other styles use custom modern forms.
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
    }
}

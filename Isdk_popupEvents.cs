using System;
using System.Runtime.InteropServices;

namespace sdk_popup
{
    /// <summary>
    /// COM event interface for the sdk_popup toast notification control.
    /// Defines all events that can be fired to Clarion.
    /// </summary>
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    [Guid("380BC3DF-AA92-49F2-AAB5-8DAEDF53F7F9")]
    public interface Isdk_popupEvents
    {
        /// <summary>
        /// Fired when a notification is shown
        /// </summary>
        /// <param name="title">The notification title</param>
        /// <param name="message">The notification message</param>
        [DispId(1)]
        void NotificationShown(string title, string message);

        /// <summary>
        /// Fired when a notification is dismissed (auto-timeout or manual)
        /// </summary>
        /// <param name="reason">Dismiss reason: Timeout, UserDismissed, Programmatic</param>
        [DispId(2)]
        void NotificationDismissed(string reason);

        /// <summary>
        /// Fired when the user clicks on the notification
        /// </summary>
        /// <param name="title">The notification title that was clicked</param>
        [DispId(3)]
        void NotificationClicked(string title);

        // --- Stack Events (v1.2.0) ---

        /// <summary>
        /// Fired when a stacked notification is shown, includes notification ID
        /// </summary>
        [DispId(10)]
        void StackNotificationShown(string notificationId, string title, string message);

        /// <summary>
        /// Fired when a stacked notification is dismissed, includes notification ID
        /// </summary>
        [DispId(11)]
        void StackNotificationDismissed(string notificationId, string reason);

        /// <summary>
        /// Fired when a stacked notification is clicked, includes notification ID
        /// </summary>
        [DispId(12)]
        void StackNotificationClicked(string notificationId, string title);

        // --- Auto-Update Events (v1.2.0) ---

        /// <summary>
        /// Fired when a notification's title or message is updated in-place
        /// </summary>
        [DispId(13)]
        void NotificationUpdated(string notificationId, string title, string message);

        /// <summary>
        /// Fired when a notification's progress percentage changes
        /// </summary>
        [DispId(14)]
        void ProgressChanged(string notificationId, int percent);
    }
}

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
    }
}

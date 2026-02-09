using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DevExpress.XtraBars.Alerter;

namespace sdk_popup
{
    /// <summary>
    /// Toast notification COM control for Clarion using DevExpress AlertControl.
    /// Displays popup/toast notifications with configurable message, title, duration, and position.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("543EDB99-F490-435D-9A2A-A9230A335041")]
    [ComSourceInterfaces(typeof(Isdk_popupEvents))]
    [ProgId("sdk_popup.sdk_popupControl")]
    public partial class sdk_popupControl : UserControl, Isdk_popup
    {
        #region Fields

        private AlertControl _alertControl;
        private string _title = "Notification";
        private string _message = "";
        private int _duration = 3000;
        private string _position = "TopRight";
        private string _notificationType = "Info";
        private string _backgroundColor = "";
        private string _textColor = "";
        private bool _isVisible;

        #endregion

        #region COM Event Delegates

        /// <summary>
        /// Delegate for NotificationShown event
        /// </summary>
        public delegate void NotificationShownDelegate(string title, string message);

        /// <summary>
        /// Delegate for NotificationDismissed event
        /// </summary>
        public delegate void NotificationDismissedDelegate(string reason);

        /// <summary>
        /// Delegate for NotificationClicked event
        /// </summary>
        public delegate void NotificationClickedDelegate(string title);

        #endregion

        #region COM Events

        /// <summary>
        /// Fired when a notification is shown
        /// </summary>
        public event NotificationShownDelegate NotificationShown;

        /// <summary>
        /// Fired when a notification is dismissed
        /// </summary>
        public event NotificationDismissedDelegate NotificationDismissed;

        /// <summary>
        /// Fired when the user clicks on the notification
        /// </summary>
        public event NotificationClickedDelegate NotificationClicked;

        #endregion

        #region Constructor

        /// <summary>
        /// Parameterless constructor required for COM.
        /// DO NOT create child controls here - use OnHandleCreated.
        /// </summary>
        public sdk_popupControl()
        {
            Size = new Size(1, 1);
            DoubleBuffered = true;
        }

        /// <summary>
        /// Create the AlertControl after the window handle exists.
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (!DesignMode)
            {
                InitializeAlertControl();
            }
        }

        /// <summary>
        /// Initialize the DevExpress AlertControl component.
        /// </summary>
        private void InitializeAlertControl()
        {
            if (_alertControl != null) return;

            _alertControl = new AlertControl();
            _alertControl.AutoFormDelay = _duration;
            _alertControl.FormShowingEffect = AlertFormShowingEffect.FadeIn;
            _alertControl.ShowPinButton = false;
            _alertControl.ShowCloseButton = true;
            _alertControl.ShowToolTips = false;

            _alertControl.AlertClick += AlertControl_AlertClick;
            _alertControl.FormClosing += AlertControl_FormClosing;

            ApplyPosition();
        }

        #endregion

        #region Isdk_popup Methods

        /// <summary>
        /// Shows a toast notification with the specified message
        /// </summary>
        [ComVisible(true)]
        [Description("Shows a toast notification with the specified message")]
        public void ShowNotification(string message)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(ShowNotification), message);
                    return;
                }

                _message = message ?? "";
                ShowAlert();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowNotification error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a toast notification with title and message
        /// </summary>
        [ComVisible(true)]
        [Description("Shows a toast notification with title and message")]
        public void ShowNotificationWithTitle(string title, string message)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string, string>(ShowNotificationWithTitle), title, message);
                    return;
                }

                _title = title ?? "Notification";
                _message = message ?? "";
                ShowAlert();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowNotificationWithTitle error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the notification title text
        /// </summary>
        [ComVisible(true)]
        [Description("Sets the notification title text")]
        public void SetTitle(string title)
        {
            _title = title ?? "Notification";
        }

        /// <summary>
        /// Gets the current notification title text
        /// </summary>
        [ComVisible(true)]
        [Description("Gets the current notification title text")]
        public string GetTitle()
        {
            return _title ?? "";
        }

        /// <summary>
        /// Sets the notification message text
        /// </summary>
        [ComVisible(true)]
        [Description("Sets the notification message text")]
        public void SetMessage(string message)
        {
            _message = message ?? "";
        }

        /// <summary>
        /// Gets the current notification message text
        /// </summary>
        [ComVisible(true)]
        [Description("Gets the current notification message text")]
        public string GetMessage()
        {
            return _message ?? "";
        }

        /// <summary>
        /// Sets the display duration in milliseconds (default: 3000)
        /// </summary>
        [ComVisible(true)]
        [Description("Sets the display duration in milliseconds")]
        public void SetDuration(int milliseconds)
        {
            _duration = Math.Max(500, milliseconds);
            if (_alertControl != null)
            {
                _alertControl.AutoFormDelay = _duration;
            }
        }

        /// <summary>
        /// Gets the current display duration in milliseconds
        /// </summary>
        [ComVisible(true)]
        [Description("Gets the current display duration in milliseconds")]
        public int GetDuration()
        {
            return _duration;
        }

        /// <summary>
        /// Sets the notification position: TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight
        /// </summary>
        [ComVisible(true)]
        [Description("Sets the notification position")]
        public void SetPosition(string position)
        {
            _position = position ?? "TopRight";
            ApplyPosition();
        }

        /// <summary>
        /// Gets the current notification position
        /// </summary>
        [ComVisible(true)]
        [Description("Gets the current notification position")]
        public string GetPosition()
        {
            return _position ?? "TopRight";
        }

        /// <summary>
        /// Sets the notification type: Info, Success, Warning, Error
        /// </summary>
        [ComVisible(true)]
        [Description("Sets the notification type")]
        public void SetNotificationType(string notificationType)
        {
            _notificationType = notificationType ?? "Info";
        }

        /// <summary>
        /// Gets the current notification type
        /// </summary>
        [ComVisible(true)]
        [Description("Gets the current notification type")]
        public string GetNotificationType()
        {
            return _notificationType ?? "Info";
        }

        /// <summary>
        /// Sets the background color for the notification (hex format, e.g. #FF6600)
        /// </summary>
        [ComVisible(true)]
        [Description("Sets the background color for the notification")]
        public void SetBackgroundColor(string hexColor)
        {
            _backgroundColor = hexColor ?? "";
        }

        /// <summary>
        /// Gets the current background color
        /// </summary>
        [ComVisible(true)]
        [Description("Gets the current background color")]
        public string GetBackgroundColor()
        {
            return _backgroundColor ?? "";
        }

        /// <summary>
        /// Sets the text color for the notification (hex format)
        /// </summary>
        [ComVisible(true)]
        [Description("Sets the text color for the notification")]
        public void SetTextColor(string hexColor)
        {
            _textColor = hexColor ?? "";
        }

        /// <summary>
        /// Gets the current text color
        /// </summary>
        [ComVisible(true)]
        [Description("Gets the current text color")]
        public string GetTextColor()
        {
            return _textColor ?? "";
        }

        /// <summary>
        /// Dismisses any currently visible notification
        /// </summary>
        [ComVisible(true)]
        [Description("Dismisses any currently visible notification")]
        public void DismissNotification()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(DismissNotification));
                    return;
                }

                if (_alertControl != null)
                {
                    _alertControl.AlertFormList.ForEach(f => f.Close());
                    _isVisible = false;
                    RaiseNotificationDismissed("Programmatic");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup DismissNotification error: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns whether a notification is currently visible
        /// </summary>
        [ComVisible(true)]
        [Description("Returns whether a notification is currently visible")]
        public bool GetIsVisible()
        {
            return _isVisible;
        }

        /// <summary>
        /// Displays control name and version information
        /// </summary>
        [ComVisible(true)]
        [Description("Shows control name and version information")]
        public void About()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var name = assembly.GetName().Name;
                var version = assembly.GetName().Version;
                var versionStr = $"{version.Major}.{version.Minor}.{version.Build}";

                MessageBox.Show(
                    $"{name}\nVersion: {versionStr}\n\nDevExpress toast notification control for Clarion",
                    "About",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch { }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Shows the DevExpress alert notification
        /// </summary>
        private void ShowAlert()
        {
            if (_alertControl == null) return;

            try
            {
                var info = new AlertInfo(_title, _message);

                // Apply notification type icon
                info.Image = GetNotificationIcon();

                // Apply custom appearance
                _alertControl.AutoFormDelay = _duration;
                ApplyPosition();

                // Show alert relative to the owning form
                Form ownerForm = FindForm();
                if (ownerForm != null)
                {
                    _alertControl.Show(ownerForm, info);
                }
                else
                {
                    _alertControl.Show(null, info);
                }

                _isVisible = true;
                RaiseNotificationShown(_title, _message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowAlert error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets an icon bitmap based on the notification type
        /// </summary>
        private Image GetNotificationIcon()
        {
            try
            {
                switch ((_notificationType ?? "Info").ToLower())
                {
                    case "success":
                        return SystemIcons.Information.ToBitmap();
                    case "warning":
                        return SystemIcons.Warning.ToBitmap();
                    case "error":
                        return SystemIcons.Error.ToBitmap();
                    case "info":
                    default:
                        return SystemIcons.Information.ToBitmap();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Applies the current position setting to the AlertControl
        /// </summary>
        private void ApplyPosition()
        {
            if (_alertControl == null) return;

            switch ((_position ?? "TopRight").ToLower())
            {
                case "topleft":
                    _alertControl.FormLocation = AlertFormLocation.TopLeft;
                    break;
                case "topcenter":
                    _alertControl.FormLocation = AlertFormLocation.TopLeft;
                    break;
                case "topright":
                    _alertControl.FormLocation = AlertFormLocation.TopRight;
                    break;
                case "bottomleft":
                    _alertControl.FormLocation = AlertFormLocation.BottomLeft;
                    break;
                case "bottomcenter":
                    _alertControl.FormLocation = AlertFormLocation.BottomLeft;
                    break;
                case "bottomright":
                    _alertControl.FormLocation = AlertFormLocation.BottomRight;
                    break;
                default:
                    _alertControl.FormLocation = AlertFormLocation.TopRight;
                    break;
            }
        }

        #endregion

        #region Event Handlers

        private void AlertControl_AlertClick(object sender, AlertClickEventArgs e)
        {
            try
            {
                RaiseNotificationClicked(_title);
            }
            catch { }
        }

        private void AlertControl_FormClosing(object sender, AlertFormClosingEventArgs e)
        {
            try
            {
                _isVisible = false;
                RaiseNotificationDismissed("Timeout");
            }
            catch { }
        }

        #endregion

        #region Event Raising Methods

        private void RaiseNotificationShown(string title, string message)
        {
            if (NotificationShown != null)
            {
                try
                {
                    NotificationShown(title, message);
                }
                catch { }
            }
        }

        private void RaiseNotificationDismissed(string reason)
        {
            if (NotificationDismissed != null)
            {
                try
                {
                    NotificationDismissed(reason);
                }
                catch { }
            }
        }

        private void RaiseNotificationClicked(string title)
        {
            if (NotificationClicked != null)
            {
                try
                {
                    NotificationClicked(title);
                }
                catch { }
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Dispose pattern for proper cleanup
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_alertControl != null)
                {
                    _alertControl.AlertClick -= AlertControl_AlertClick;
                    _alertControl.FormClosing -= AlertControl_FormClosing;
                    _alertControl.Dispose();
                    _alertControl = null;
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}

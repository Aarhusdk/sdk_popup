using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Media;
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
        private string _imagePath = "";
        private Image _customImage;
        private bool _isVisible;
        private bool _soundEnabled;
        private string _soundPath = "";
        private int _opacity = 100;
        private bool _pinned;
        private int _imageWidth;
        private int _imageHeight;
        private Form _imageOnlyForm;
        private Timer _imageOnlyTimer;

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
            _alertControl.BeforeFormShow += AlertControl_BeforeFormShow;

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
        /// Sets the image from a file path (PNG, JPG, BMP, ICO)
        /// </summary>
        [ComVisible(true)]
        [Description("Sets the image from a file path")]
        public void SetImagePath(string filePath)
        {
            try
            {
                _imagePath = filePath ?? "";
                if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
                {
                    // Load image from file without locking it
                    using (var stream = new FileStream(_imagePath, FileMode.Open, FileAccess.Read))
                    {
                        _customImage?.Dispose();
                        _customImage = Image.FromStream(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup SetImagePath error: {ex.Message}");
                _customImage = null;
            }
        }

        /// <summary>
        /// Gets the current image file path
        /// </summary>
        [ComVisible(true)]
        [Description("Gets the current image file path")]
        public string GetImagePath()
        {
            return _imagePath ?? "";
        }

        /// <summary>
        /// Clears the custom image, reverting to the notification type icon
        /// </summary>
        [ComVisible(true)]
        [Description("Clears the custom image")]
        public void ClearImage()
        {
            _imagePath = "";
            _customImage?.Dispose();
            _customImage = null;
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

                // Close image-only form
                CloseImageOnlyForm();

                // Close alert forms
                if (_alertControl != null)
                {
                    _alertControl.AlertFormList.ForEach(f => f.Close());
                }

                _isVisible = false;
                RaiseNotificationDismissed("Programmatic");
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

        // --- Sound ---

        [ComVisible(true)]
        [Description("Enables or disables playing a sound when a notification is shown")]
        public void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
        }

        [ComVisible(true)]
        [Description("Gets whether sound is enabled")]
        public bool GetSoundEnabled()
        {
            return _soundEnabled;
        }

        [ComVisible(true)]
        [Description("Sets a custom WAV file path for the notification sound")]
        public void SetSoundPath(string wavFilePath)
        {
            _soundPath = wavFilePath ?? "";
        }

        [ComVisible(true)]
        [Description("Gets the current custom WAV file path")]
        public string GetSoundPath()
        {
            return _soundPath ?? "";
        }

        // --- Opacity ---

        [ComVisible(true)]
        [Description("Sets the notification opacity (0-100, where 100 is fully opaque)")]
        public void SetOpacity(int percent)
        {
            _opacity = Math.Max(10, Math.Min(100, percent));
        }

        [ComVisible(true)]
        [Description("Gets the current notification opacity (0-100)")]
        public int GetOpacity()
        {
            return _opacity;
        }

        // --- Sticky/Pinned ---

        [ComVisible(true)]
        [Description("Sets whether the notification stays visible until manually dismissed")]
        public void SetPinned(bool pinned)
        {
            _pinned = pinned;
        }

        [ComVisible(true)]
        [Description("Gets whether notifications are pinned (sticky)")]
        public bool GetPinned()
        {
            return _pinned;
        }

        // --- Image Size ---

        [ComVisible(true)]
        [Description("Sets the display size for the notification image in pixels. 0,0 = original size.")]
        public void SetImageSize(int width, int height)
        {
            _imageWidth = Math.Max(0, width);
            _imageHeight = Math.Max(0, height);
        }

        [ComVisible(true)]
        [Description("Gets the configured image display width")]
        public int GetImageWidth()
        {
            return _imageWidth;
        }

        [ComVisible(true)]
        [Description("Gets the configured image display height")]
        public int GetImageHeight()
        {
            return _imageHeight;
        }

        [ComVisible(true)]
        [Description("Shows a notification with only an image - no background, title, or message")]
        public void ShowImageNotification()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(ShowImageNotification));
                    return;
                }

                // Close any previous image-only form
                CloseImageOnlyForm();

                Image img = GetDisplayImage();
                if (img == null) return;

                // Create borderless transparent form
                _imageOnlyForm = new Form();
                _imageOnlyForm.FormBorderStyle = FormBorderStyle.None;
                _imageOnlyForm.ShowInTaskbar = false;
                _imageOnlyForm.TopMost = true;
                _imageOnlyForm.StartPosition = FormStartPosition.Manual;
                _imageOnlyForm.BackColor = Color.Magenta;
                _imageOnlyForm.TransparencyKey = Color.Magenta;
                _imageOnlyForm.Size = new Size(img.Width, img.Height);

                if (_opacity < 100)
                {
                    _imageOnlyForm.Opacity = _opacity / 100.0;
                }

                // Add image
                var pictureBox = new PictureBox();
                pictureBox.Image = img;
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox.Size = new Size(img.Width, img.Height);
                pictureBox.Location = new Point(0, 0);
                pictureBox.BackColor = Color.Transparent;
                pictureBox.Cursor = Cursors.Hand;
                _imageOnlyForm.Controls.Add(pictureBox);

                // Position the form
                PositionImageOnlyForm(_imageOnlyForm);

                // Click to dismiss
                pictureBox.Click += (s, e) =>
                {
                    CloseImageOnlyForm();
                    RaiseNotificationClicked("");
                };

                // Auto-close timer (unless pinned)
                if (!_pinned)
                {
                    _imageOnlyTimer = new Timer();
                    _imageOnlyTimer.Interval = _duration;
                    _imageOnlyTimer.Tick += (s, e) =>
                    {
                        CloseImageOnlyForm();
                        RaiseNotificationDismissed("Timeout");
                    };
                    _imageOnlyTimer.Start();
                }

                // Play sound
                if (_soundEnabled)
                {
                    PlayNotificationSound();
                }

                _imageOnlyForm.Show();
                _isVisible = true;
                RaiseNotificationShown("", "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowImageNotification error: {ex.Message}");
            }
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

                // Apply custom image or notification type icon, with optional resize
                info.Image = GetDisplayImage();

                // Apply custom appearance
                _alertControl.AutoFormDelay = _pinned ? int.MaxValue : _duration;
                _alertControl.ShowPinButton = _pinned;
                ApplyPosition();

                // Determine if center repositioning is needed
                string pos = (_position ?? "").ToLower();
                bool needsReposition = (pos == "center" || pos == "topcenter" || pos == "bottomcenter");

                // Disable animation for center positions - animation overrides our position
                _alertControl.FormShowingEffect = needsReposition
                    ? AlertFormShowingEffect.None
                    : AlertFormShowingEffect.FadeIn;

                // Play sound
                if (_soundEnabled)
                {
                    PlayNotificationSound();
                }

                // Show alert
                Form ownerForm = FindForm();
                _alertControl.Show(ownerForm, info);

                _isVisible = true;
                RaiseNotificationShown(_title, _message);

                // Reposition AFTER Show() for center positions
                if (needsReposition)
                {
                    var capturedPos = pos;
                    BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            RepositionAlertForms(capturedPos);
                        }
                        catch { }
                    }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowAlert error: {ex.Message}");
            }
        }

        /// <summary>
        /// Positions the image-only form according to the current position setting
        /// </summary>
        private void PositionImageOnlyForm(Form form)
        {
            Screen screen = Screen.PrimaryScreen;
            Rectangle workArea = screen.WorkingArea;
            int x, y;

            switch ((_position ?? "TopRight").ToLower())
            {
                case "topleft":
                    x = workArea.Left + 10;
                    y = workArea.Top + 10;
                    break;
                case "topcenter":
                    x = workArea.Left + (workArea.Width - form.Width) / 2;
                    y = workArea.Top + 10;
                    break;
                case "topright":
                    x = workArea.Right - form.Width - 10;
                    y = workArea.Top + 10;
                    break;
                case "bottomleft":
                    x = workArea.Left + 10;
                    y = workArea.Bottom - form.Height - 10;
                    break;
                case "bottomcenter":
                    x = workArea.Left + (workArea.Width - form.Width) / 2;
                    y = workArea.Bottom - form.Height - 10;
                    break;
                case "bottomright":
                    x = workArea.Right - form.Width - 10;
                    y = workArea.Bottom - form.Height - 10;
                    break;
                case "center":
                    x = workArea.Left + (workArea.Width - form.Width) / 2;
                    y = workArea.Top + (workArea.Height - form.Height) / 2;
                    break;
                default:
                    x = workArea.Right - form.Width - 10;
                    y = workArea.Top + 10;
                    break;
            }

            form.Location = new Point(x, y);
        }

        /// <summary>
        /// Closes and disposes the image-only form
        /// </summary>
        private void CloseImageOnlyForm()
        {
            if (_imageOnlyTimer != null)
            {
                _imageOnlyTimer.Stop();
                _imageOnlyTimer.Dispose();
                _imageOnlyTimer = null;
            }

            if (_imageOnlyForm != null && !_imageOnlyForm.IsDisposed)
            {
                _imageOnlyForm.Close();
                _imageOnlyForm.Dispose();
                _imageOnlyForm = null;
                _isVisible = false;
            }
        }

        /// <summary>
        /// Repositions all active alert forms to center positions.
        /// Called via BeginInvoke after Show() so DevExpress has finished its positioning.
        /// </summary>
        private void RepositionAlertForms(string pos)
        {
            if (_alertControl == null) return;

            var forms = _alertControl.AlertFormList;
            if (forms == null || forms.Count == 0) return;

            Screen screen = Screen.PrimaryScreen;
            Rectangle workArea = screen.WorkingArea;

            for (int i = 0; i < forms.Count; i++)
            {
                var alertForm = forms[i];
                int x = workArea.Left + (workArea.Width - alertForm.Width) / 2;
                int y;

                if (pos == "center")
                {
                    y = workArea.Top + (workArea.Height - alertForm.Height) / 2;
                }
                else if (pos == "topcenter")
                {
                    y = workArea.Top + 10;
                }
                else // bottomcenter
                {
                    y = workArea.Bottom - alertForm.Height - 10;
                }

                alertForm.Location = new Point(x, y);
            }
        }

        /// <summary>
        /// Gets the image to display, resized if SetImageSize was called
        /// </summary>
        private Image GetDisplayImage()
        {
            Image source = _customImage ?? GetNotificationIcon();
            if (source == null) return null;

            // Resize if custom dimensions are set
            if (_imageWidth > 0 && _imageHeight > 0)
            {
                try
                {
                    var resized = new Bitmap(_imageWidth, _imageHeight);
                    using (var g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(source, 0, 0, _imageWidth, _imageHeight);
                    }
                    return resized;
                }
                catch
                {
                    return source;
                }
            }

            return source;
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
        /// Plays the notification sound (custom WAV or system default)
        /// </summary>
        private void PlayNotificationSound()
        {
            try
            {
                if (!string.IsNullOrEmpty(_soundPath) && File.Exists(_soundPath))
                {
                    using (var player = new SoundPlayer(_soundPath))
                    {
                        player.Play();
                    }
                }
                else
                {
                    SystemSounds.Asterisk.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup PlayNotificationSound error: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the current position setting to the AlertControl.
        /// For "Center" position, we use BeforeFormShow to reposition the alert form.
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
                    _alertControl.FormLocation = AlertFormLocation.TopRight;
                    break;
                case "topright":
                    _alertControl.FormLocation = AlertFormLocation.TopRight;
                    break;
                case "bottomleft":
                    _alertControl.FormLocation = AlertFormLocation.BottomLeft;
                    break;
                case "bottomcenter":
                    _alertControl.FormLocation = AlertFormLocation.BottomRight;
                    break;
                case "bottomright":
                    _alertControl.FormLocation = AlertFormLocation.BottomRight;
                    break;
                case "center":
                    // Use TopRight as base; BeforeFormShow will reposition to center
                    _alertControl.FormLocation = AlertFormLocation.TopRight;
                    break;
                default:
                    _alertControl.FormLocation = AlertFormLocation.TopRight;
                    break;
            }
        }

        #endregion

        #region Event Handlers

        private void AlertControl_BeforeFormShow(object sender, AlertFormEventArgs e)
        {
            try
            {
                // Apply opacity
                if (_opacity < 100)
                {
                    e.AlertForm.Opacity = _opacity / 100.0;
                }
            }
            catch { }
        }

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
                CloseImageOnlyForm();

                _customImage?.Dispose();
                _customImage = null;

                if (_alertControl != null)
                {
                    _alertControl.AlertClick -= AlertControl_AlertClick;
                    _alertControl.FormClosing -= AlertControl_FormClosing;
                    _alertControl.BeforeFormShow -= AlertControl_BeforeFormShow;
                    _alertControl.Dispose();
                    _alertControl = null;
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}

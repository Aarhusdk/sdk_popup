using System;
using System.Collections.Generic;
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
    /// v1.2.0: Multi-notification stacking, queuing, and per-notification ID tracking.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("543EDB99-F490-435D-9A2A-A9230A335041")]
    [ComSourceInterfaces(typeof(Isdk_popupEvents))]
    [ProgId("sdk_popup.sdk_popupControl")]
    public partial class sdk_popupControl : UserControl, Isdk_popup
    {
        #region NotificationRecord

        private class NotificationRecord
        {
            public string Id;
            public string Title;
            public string Message;
            public AlertInfo AlertInfo;     // null for image-only
            public object AlertFormRef;     // AlertForm reference for in-place updates (set in BeforeFormShow)
            public Form ImageForm;          // null for standard alerts
            public Timer ImageTimer;        // null for standard alerts
            public bool IsImageOnly;
            public bool IsStyledForm;       // true for custom styled notifications
        }

        #endregion

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
        private string _styleName = "Default";

        // --- Stacking (v1.2.0) ---
        private Dictionary<string, NotificationRecord> _notifications = new Dictionary<string, NotificationRecord>();
        private Dictionary<object, string> _alertFormToIdMap = new Dictionary<object, string>();
        private Dictionary<Form, string> _imageFormToIdMap = new Dictionary<Form, string>();
        private List<string> _visibleImageFormOrder = new List<string>();
        private Queue<NotificationRecord> _imageOnlyQueue = new Queue<NotificationRecord>();
        private int _nextNotificationId = 1;
        private int _maxStackSize;  // 0 = unlimited

        #endregion

        #region COM Event Delegates

        public delegate void NotificationShownDelegate(string title, string message);
        public delegate void NotificationDismissedDelegate(string reason);
        public delegate void NotificationClickedDelegate(string title);

        // Stack event delegates (v1.2.0)
        public delegate void StackNotificationShownDelegate(string notificationId, string title, string message);
        public delegate void StackNotificationDismissedDelegate(string notificationId, string reason);
        public delegate void StackNotificationClickedDelegate(string notificationId, string title);

        // Auto-update event delegates (v1.2.0)
        public delegate void NotificationUpdatedDelegate(string notificationId, string title, string message);
        public delegate void ProgressChangedDelegate(string notificationId, int percent);

        #endregion

        #region COM Events

        public event NotificationShownDelegate NotificationShown;
        public event NotificationDismissedDelegate NotificationDismissed;
        public event NotificationClickedDelegate NotificationClicked;

        // Stack events (v1.2.0)
        public event StackNotificationShownDelegate StackNotificationShown;
        public event StackNotificationDismissedDelegate StackNotificationDismissed;
        public event StackNotificationClickedDelegate StackNotificationClicked;

        // Auto-update events (v1.2.0)
        public event NotificationUpdatedDelegate NotificationUpdated;
        public event ProgressChangedDelegate ProgressChanged;

        #endregion

        #region Constructor

        public sdk_popupControl()
        {
            Size = new Size(1, 1);
            DoubleBuffered = true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (!DesignMode)
            {
                InitializeAlertControl();
            }
        }

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

        #region Isdk_popup Methods (existing)

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

        [ComVisible(true)]
        [Description("Sets the notification title text")]
        public void SetTitle(string title)
        {
            _title = (title ?? "Notification").Trim().Trim('"');
        }

        [ComVisible(true)]
        [Description("Gets the current notification title text")]
        public string GetTitle()
        {
            return _title ?? "";
        }

        [ComVisible(true)]
        [Description("Sets the notification message text")]
        public void SetMessage(string message)
        {
            _message = (message ?? "").Trim().Trim('"');
        }

        [ComVisible(true)]
        [Description("Gets the current notification message text")]
        public string GetMessage()
        {
            return _message ?? "";
        }

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

        [ComVisible(true)]
        [Description("Gets the current display duration in milliseconds")]
        public int GetDuration()
        {
            return _duration;
        }

        [ComVisible(true)]
        [Description("Sets the notification position")]
        public void SetPosition(string position)
        {
            _position = (position ?? "TopRight").Trim().Trim('"');
            ApplyPosition();
        }

        [ComVisible(true)]
        [Description("Gets the current notification position")]
        public string GetPosition()
        {
            return _position ?? "TopRight";
        }

        [ComVisible(true)]
        [Description("Sets the notification type")]
        public void SetNotificationType(string notificationType)
        {
            _notificationType = (notificationType ?? "Info").Trim().Trim('"');
        }

        [ComVisible(true)]
        [Description("Gets the current notification type")]
        public string GetNotificationType()
        {
            return _notificationType ?? "Info";
        }

        [ComVisible(true)]
        [Description("Sets the background color for the notification")]
        public void SetBackgroundColor(string hexColor)
        {
            _backgroundColor = (hexColor ?? "").Trim().Trim('"');
        }

        [ComVisible(true)]
        [Description("Gets the current background color")]
        public string GetBackgroundColor()
        {
            return _backgroundColor ?? "";
        }

        [ComVisible(true)]
        [Description("Sets the text color for the notification")]
        public void SetTextColor(string hexColor)
        {
            _textColor = (hexColor ?? "").Trim().Trim('"');
        }

        [ComVisible(true)]
        [Description("Gets the current text color")]
        public string GetTextColor()
        {
            return _textColor ?? "";
        }

        [ComVisible(true)]
        [Description("Sets the image from a file path")]
        public void SetImagePath(string filePath)
        {
            try
            {
                _imagePath = (filePath ?? "").Trim().Trim('"');
                if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
                {
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

        [ComVisible(true)]
        [Description("Gets the current image file path")]
        public string GetImagePath()
        {
            return _imagePath ?? "";
        }

        [ComVisible(true)]
        [Description("Clears the custom image")]
        public void ClearImage()
        {
            _imagePath = "";
            _customImage?.Dispose();
            _customImage = null;
        }

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

                // Close legacy image-only form
                CloseImageOnlyForm();

                // Close all stacked image forms
                CloseAllImageForms();

                // Clear image queue
                _imageOnlyQueue.Clear();

                // Close alert forms
                if (_alertControl != null)
                {
                    _alertControl.AlertFormList.ForEach(f => f.Close());
                }

                // Clear all tracking
                _notifications.Clear();
                _alertFormToIdMap.Clear();
                _imageFormToIdMap.Clear();
                _visibleImageFormOrder.Clear();

                _isVisible = false;
                RaiseNotificationDismissed("Programmatic");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup DismissNotification error: {ex.Message}");
            }
        }

        [ComVisible(true)]
        [Description("Returns whether a notification is currently visible")]
        public bool GetIsVisible()
        {
            return _isVisible || _notifications.Count > 0;
        }

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
            _soundPath = (wavFilePath ?? "").Trim().Trim('"');
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

        // --- Style/Theme ---

        [ComVisible(true)]
        [Description("Sets the notification style: Default, Dark, Light, Rounded, Minimal")]
        public void SetStyle(string styleName)
        {
            string s = (styleName ?? "Default").Trim().Trim('"');
            var style = NotificationStyleRegistry.GetStyle(s);
            _styleName = (style != null || s.Equals("Default", StringComparison.OrdinalIgnoreCase)) ? s : "Default";
        }

        [ComVisible(true)]
        [Description("Gets the current notification style name")]
        public string GetStyle()
        {
            return _styleName ?? "Default";
        }

        [ComVisible(true)]
        [Description("Returns a comma-separated list of available style names")]
        public string GetAvailableStyles()
        {
            return "Default,Dark,Light,Rounded,Minimal";
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

                // Close any previous legacy image-only form
                CloseImageOnlyForm();

                Image img = GetDisplayImage();
                if (img == null) return;

                var transKey = Color.FromArgb(1, 0, 1);

                _imageOnlyForm = new Form();
                _imageOnlyForm.FormBorderStyle = FormBorderStyle.None;
                _imageOnlyForm.ShowInTaskbar = false;
                _imageOnlyForm.TopMost = true;
                _imageOnlyForm.StartPosition = FormStartPosition.Manual;
                _imageOnlyForm.AllowTransparency = true;
                _imageOnlyForm.BackColor = transKey;
                _imageOnlyForm.TransparencyKey = transKey;

                if (_opacity < 100)
                {
                    _imageOnlyForm.Opacity = _opacity / 100.0;
                }

                var pb = new PictureBox();
                pb.Image = img;
                pb.BackColor = transKey;
                pb.Dock = DockStyle.Fill;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Cursor = Cursors.Hand;
                _imageOnlyForm.Controls.Add(pb);

                _imageOnlyForm.ClientSize = new Size(img.Width, img.Height);

                PositionImageOnlyForm(_imageOnlyForm);

                pb.Click += (s, e) =>
                {
                    CloseImageOnlyForm();
                    RaiseNotificationClicked("");
                };

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

        #region Isdk_popup Methods (v1.2.0 Stacking)

        [ComVisible(true)]
        [Description("Shows a notification using current settings and returns a notification ID")]
        public string ShowNotificationEx()
        {
            try
            {
                if (InvokeRequired)
                {
                    return (string)Invoke(new Func<string>(ShowNotificationEx));
                }

                return ShowAlertTracked(_title, _message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowNotificationEx error: {ex.Message}");
                return "";
            }
        }

        [ComVisible(true)]
        [Description("Shows a notification with title and message, returns a notification ID")]
        public string ShowNotificationWithTitleEx(string title, string message)
        {
            try
            {
                if (InvokeRequired)
                {
                    return (string)Invoke(new Func<string, string, string>(ShowNotificationWithTitleEx), title, message);
                }

                string t = title ?? "Notification";
                string m = message ?? "";
                _title = t;
                _message = m;
                return ShowAlertTracked(t, m);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowNotificationWithTitleEx error: {ex.Message}");
                return "";
            }
        }

        [ComVisible(true)]
        [Description("Dismisses a specific notification by its ID")]
        public void DismissNotificationById(string notificationId)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(DismissNotificationById), notificationId);
                    return;
                }

                if (string.IsNullOrEmpty(notificationId)) return;
                if (!_notifications.TryGetValue(notificationId, out var record)) return;

                if (record.IsImageOnly || record.IsStyledForm)
                {
                    DismissImageFormRecord(record, "Programmatic");
                }
                else
                {
                    DismissAlertRecord(record, "Programmatic");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup DismissNotificationById error: {ex.Message}");
            }
        }

        [ComVisible(true)]
        [Description("Sets the maximum number of simultaneously visible notifications (0 = unlimited)")]
        public void SetMaxVisibleNotifications(int maxCount)
        {
            _maxStackSize = Math.Max(0, maxCount);

            if (_alertControl != null)
            {
                _alertControl.FormMaxCount = _maxStackSize > 0 ? _maxStackSize : 0;
            }
        }

        [ComVisible(true)]
        [Description("Gets the maximum number of simultaneously visible notifications")]
        public int GetMaxVisibleNotifications()
        {
            return _maxStackSize;
        }

        [ComVisible(true)]
        [Description("Gets the number of currently active (visible) notifications")]
        public int GetActiveNotificationCount()
        {
            int count = 0;

            if (_alertControl != null)
            {
                count += _alertControl.AlertFormList.Count;
            }

            count += _visibleImageFormOrder.Count;

            return count;
        }

        [ComVisible(true)]
        [Description("Gets the number of queued notifications waiting to be shown")]
        public int GetQueuedNotificationCount()
        {
            return _imageOnlyQueue.Count;
        }

        [ComVisible(true)]
        [Description("Dismisses all active and queued notifications")]
        public void DismissAllNotifications()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(DismissAllNotifications));
                    return;
                }

                // Close all stacked image forms (with timers)
                CloseAllImageForms();

                // Clear image queue
                _imageOnlyQueue.Clear();

                // Close all alert forms
                if (_alertControl != null)
                {
                    _alertControl.AlertFormList.ForEach(f => f.Close());
                }

                // Fire dismissed events for all tracked notifications
                foreach (var kvp in new Dictionary<string, NotificationRecord>(_notifications))
                {
                    RaiseStackNotificationDismissed(kvp.Key, "Programmatic");
                }

                // Clear all tracking
                _notifications.Clear();
                _alertFormToIdMap.Clear();
                _imageFormToIdMap.Clear();
                _visibleImageFormOrder.Clear();

                RaiseNotificationDismissed("Programmatic");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup DismissAllNotifications error: {ex.Message}");
            }
        }

        [ComVisible(true)]
        [Description("Shows an image-only notification with stacking support, returns a notification ID")]
        public string ShowImageNotificationEx()
        {
            try
            {
                if (InvokeRequired)
                {
                    return (string)Invoke(new Func<string>(ShowImageNotificationEx));
                }

                return ShowImageTracked();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowImageNotificationEx error: {ex.Message}");
                return "";
            }
        }

        #endregion

        #region Isdk_popup Methods (v1.2.0 Auto-Update)

        [ComVisible(true)]
        [Description("Updates the title and message of an existing notification in-place")]
        public bool UpdateNotification(string notificationId, string title, string message)
        {
            try
            {
                if (InvokeRequired)
                {
                    return (bool)Invoke(new Func<string, string, string, bool>(UpdateNotification), notificationId, title, message);
                }

                notificationId = (notificationId ?? "").Trim();
                if (string.IsNullOrEmpty(notificationId)) return false;
                if (!_notifications.TryGetValue(notificationId, out var record)) return false;

                string newTitle = (title ?? "").Trim();
                string newMessage = (message ?? "").Trim();
                record.Title = newTitle;
                record.Message = newMessage;

                if (record.IsImageOnly)
                {
                    // Image-only notifications don't have text to update
                    return false;
                }

                // Styled form: update via StyledNotificationForm
                if (record.IsStyledForm && record.ImageForm is StyledNotificationForm styledForm)
                {
                    styledForm.UpdateText(newTitle, newMessage);
                    RaiseNotificationUpdated(notificationId, newTitle, newMessage);
                    return true;
                }

                // Update the AlertForm controls in-place
                if (record.AlertFormRef != null)
                {
                    UpdateAlertFormText(record.AlertFormRef, newTitle, newMessage);
                }

                RaiseNotificationUpdated(notificationId, newTitle, newMessage);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup UpdateNotification error: {ex.Message}");
                return false;
            }
        }

        [ComVisible(true)]
        [Description("Updates the progress percentage of an existing notification (0-100)")]
        public bool UpdateProgress(string notificationId, int percent)
        {
            try
            {
                if (InvokeRequired)
                {
                    return (bool)Invoke(new Func<string, int, bool>(UpdateProgress), notificationId, percent);
                }

                notificationId = (notificationId ?? "").Trim();
                if (string.IsNullOrEmpty(notificationId)) return false;
                if (!_notifications.TryGetValue(notificationId, out var record)) return false;

                int clampedPercent = Math.Max(0, Math.Min(100, percent));

                if (record.IsImageOnly)
                {
                    return false;
                }

                // Styled form: update via StyledNotificationForm
                if (record.IsStyledForm && record.ImageForm is StyledNotificationForm styledProgressForm)
                {
                    styledProgressForm.UpdateProgress(clampedPercent);
                    RaiseProgressChanged(notificationId, clampedPercent);
                    return true;
                }

                // Find or create progress bar on the AlertForm
                if (record.AlertFormRef != null)
                {
                    UpdateAlertFormProgress(record.AlertFormRef, clampedPercent);
                }

                RaiseProgressChanged(notificationId, clampedPercent);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup UpdateProgress error: {ex.Message}");
                return false;
            }
        }

        [ComVisible(true)]
        [Description("Updates the title, message, and progress of an existing notification in one call")]
        public bool UpdateNotificationFull(string notificationId, string title, string message, int percent)
        {
            try
            {
                if (InvokeRequired)
                {
                    return (bool)Invoke(new Func<string, string, string, int, bool>(UpdateNotificationFull), notificationId, title, message, percent);
                }

                notificationId = (notificationId ?? "").Trim();
                if (string.IsNullOrEmpty(notificationId)) return false;
                if (!_notifications.TryGetValue(notificationId, out var record)) return false;

                if (record.IsImageOnly)
                {
                    return false;
                }

                string newTitle = (title ?? "").Trim();
                string newMessage = (message ?? "").Trim();
                int clampedPercent = Math.Max(0, Math.Min(100, percent));
                record.Title = newTitle;
                record.Message = newMessage;

                // Styled form: update via StyledNotificationForm
                if (record.IsStyledForm && record.ImageForm is StyledNotificationForm styledFullForm)
                {
                    styledFullForm.UpdateText(newTitle, newMessage);
                    styledFullForm.UpdateProgress(clampedPercent);
                    RaiseNotificationUpdated(notificationId, newTitle, newMessage);
                    RaiseProgressChanged(notificationId, clampedPercent);
                    return true;
                }

                if (record.AlertFormRef != null)
                {
                    UpdateAlertFormText(record.AlertFormRef, newTitle, newMessage);
                    UpdateAlertFormProgress(record.AlertFormRef, clampedPercent);
                }

                RaiseNotificationUpdated(notificationId, newTitle, newMessage);
                RaiseProgressChanged(notificationId, clampedPercent);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup UpdateNotificationFull error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Private Methods

        private string GenerateNotificationId()
        {
            return (_nextNotificationId++).ToString();
        }

        /// <summary>
        /// Shows a tracked standard alert notification and returns its ID.
        /// </summary>
        private string ShowAlertTracked(string title, string message)
        {
            // Route to styled form if non-default style
            var style = NotificationStyleRegistry.GetStyle(_styleName);
            if (style != null)
            {
                return ShowStyledAlertTracked(title, message);
            }

            if (_alertControl == null) return "";

            try
            {
                string id = GenerateNotificationId();

                var info = new AlertInfo(title, message);
                info.Image = GetDisplayImage();
                info.Tag = id;

                _alertControl.AutoFormDelay = _pinned ? int.MaxValue : _duration;
                _alertControl.ShowPinButton = _pinned;
                ApplyPosition();

                string pos = (_position ?? "").ToLower();
                bool needsReposition = (pos == "center" || pos == "topcenter" || pos == "bottomcenter");

                _alertControl.FormShowingEffect = needsReposition
                    ? AlertFormShowingEffect.None
                    : AlertFormShowingEffect.FadeIn;

                var record = new NotificationRecord
                {
                    Id = id,
                    Title = title,
                    Message = message,
                    AlertInfo = info,
                    IsImageOnly = false
                };
                _notifications[id] = record;

                if (_soundEnabled)
                {
                    PlayNotificationSound();
                }

                Form ownerForm = FindForm();
                _alertControl.Show(ownerForm, info);

                _isVisible = true;
                RaiseNotificationShown(title, message);
                RaiseStackNotificationShown(id, title, message);

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

                return id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowAlertTracked error: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Shows the DevExpress alert notification (legacy, non-tracked)
        /// </summary>
        private void ShowAlert()
        {
            // Route to styled form if non-default style
            var style = NotificationStyleRegistry.GetStyle(_styleName);
            if (style != null)
            {
                ShowStyledAlert(_title, _message);
                return;
            }

            if (_alertControl == null) return;

            try
            {
                var info = new AlertInfo(_title, _message);
                info.Image = GetDisplayImage();

                _alertControl.AutoFormDelay = _pinned ? int.MaxValue : _duration;
                _alertControl.ShowPinButton = _pinned;
                ApplyPosition();

                string pos = (_position ?? "").ToLower();
                bool needsReposition = (pos == "center" || pos == "topcenter" || pos == "bottomcenter");

                _alertControl.FormShowingEffect = needsReposition
                    ? AlertFormShowingEffect.None
                    : AlertFormShowingEffect.FadeIn;

                if (_soundEnabled)
                {
                    PlayNotificationSound();
                }

                Form ownerForm = FindForm();
                _alertControl.Show(ownerForm, info);

                _isVisible = true;
                RaiseNotificationShown(_title, _message);

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
        /// Shows a tracked image-only notification with stacking support.
        /// </summary>
        private string ShowImageTracked()
        {
            Image img = GetDisplayImage();
            if (img == null) return "";

            string id = GenerateNotificationId();

            var record = new NotificationRecord
            {
                Id = id,
                Title = "",
                Message = "",
                IsImageOnly = true
            };

            // Check if we need to queue
            if (_maxStackSize > 0 && _visibleImageFormOrder.Count >= _maxStackSize)
            {
                _notifications[id] = record;
                _imageOnlyQueue.Enqueue(record);
                return id;
            }

            _notifications[id] = record;
            ShowImageFormFromRecord(record, img);
            return id;
        }

        /// <summary>
        /// Creates and shows the actual image form for a tracked record.
        /// </summary>
        private void ShowImageFormFromRecord(NotificationRecord record, Image img = null)
        {
            if (img == null)
            {
                img = GetDisplayImage();
                if (img == null) return;
            }

            var transKey = Color.FromArgb(1, 0, 1);

            var form = new Form();
            form.FormBorderStyle = FormBorderStyle.None;
            form.ShowInTaskbar = false;
            form.TopMost = true;
            form.StartPosition = FormStartPosition.Manual;
            form.AllowTransparency = true;
            form.BackColor = transKey;
            form.TransparencyKey = transKey;

            if (_opacity < 100)
            {
                form.Opacity = _opacity / 100.0;
            }

            var pb = new PictureBox();
            pb.Image = img;
            pb.BackColor = transKey;
            pb.Dock = DockStyle.Fill;
            pb.SizeMode = PictureBoxSizeMode.StretchImage;
            pb.Cursor = Cursors.Hand;
            form.Controls.Add(pb);

            form.ClientSize = new Size(img.Width, img.Height);

            record.ImageForm = form;
            _imageFormToIdMap[form] = record.Id;
            _visibleImageFormOrder.Add(record.Id);

            // Position with stacking offset
            ReflowImageForms();

            // Click to dismiss
            var capturedId = record.Id;
            pb.Click += (s, e) =>
            {
                DismissImageFormById(capturedId, "UserDismissed");
                RaiseNotificationClicked("");
                RaiseStackNotificationClicked(capturedId, "");
            };

            // Auto-close timer (unless pinned)
            if (!_pinned)
            {
                var timer = new Timer();
                timer.Interval = _duration;
                record.ImageTimer = timer;
                var timerId = record.Id;
                timer.Tick += (s, e) =>
                {
                    DismissImageFormById(timerId, "Timeout");
                };
                timer.Start();
            }

            if (_soundEnabled)
            {
                PlayNotificationSound();
            }

            form.Show();
            _isVisible = true;
            RaiseNotificationShown("", "");
            RaiseStackNotificationShown(record.Id, "", "");
        }

        /// <summary>
        /// Dismisses a tracked image form by notification ID.
        /// </summary>
        private void DismissImageFormById(string id, string reason)
        {
            if (!_notifications.TryGetValue(id, out var record)) return;
            DismissImageFormRecord(record, reason);
        }

        /// <summary>
        /// Dismisses a tracked image form from its record.
        /// </summary>
        private void DismissImageFormRecord(NotificationRecord record, string reason)
        {
            // Stop and dispose timer
            if (record.ImageTimer != null)
            {
                record.ImageTimer.Stop();
                record.ImageTimer.Dispose();
                record.ImageTimer = null;
            }

            // Close and dispose form
            if (record.ImageForm != null && !record.ImageForm.IsDisposed)
            {
                _imageFormToIdMap.Remove(record.ImageForm);
                record.ImageForm.Close();
                record.ImageForm.Dispose();
                record.ImageForm = null;
            }

            // Remove from tracking
            _visibleImageFormOrder.Remove(record.Id);
            _notifications.Remove(record.Id);

            // Fire events
            RaiseNotificationDismissed(reason);
            RaiseStackNotificationDismissed(record.Id, reason);

            // Reflow remaining
            ReflowImageForms();

            // Dequeue next if available
            DequeueNextImageForm();

            // Update visibility
            if (_notifications.Count == 0 && !_isVisible)
            {
                _isVisible = false;
            }
        }

        /// <summary>
        /// Dismisses a tracked standard alert from its record.
        /// </summary>
        private void DismissAlertRecord(NotificationRecord record, string reason)
        {
            // Find and close the alert form
            if (_alertControl != null && record.AlertInfo != null)
            {
                // Find the alert form by matching tag in our map
                object formToClose = null;
                foreach (var kvp in _alertFormToIdMap)
                {
                    if (kvp.Value == record.Id)
                    {
                        formToClose = kvp.Key;
                        break;
                    }
                }

                if (formToClose != null)
                {
                    _alertFormToIdMap.Remove(formToClose);
                    try
                    {
                        // AlertForm inherits from Form, try to close it
                        var closeMethod = formToClose.GetType().GetMethod("Close");
                        closeMethod?.Invoke(formToClose, null);
                    }
                    catch { }
                }
            }

            _notifications.Remove(record.Id);

            RaiseNotificationDismissed(reason);
            RaiseStackNotificationDismissed(record.Id, reason);
        }

        /// <summary>
        /// Dequeues the next image-only notification if there's room.
        /// </summary>
        private void DequeueNextImageForm()
        {
            if (_imageOnlyQueue.Count == 0) return;
            if (_maxStackSize > 0 && _visibleImageFormOrder.Count >= _maxStackSize) return;

            var record = _imageOnlyQueue.Dequeue();
            ShowImageFormFromRecord(record);
        }

        /// <summary>
        /// Closes all tracked image forms (but not the legacy _imageOnlyForm).
        /// </summary>
        private void CloseAllImageForms()
        {
            // Copy IDs to avoid modification during iteration
            var ids = new List<string>(_visibleImageFormOrder);
            foreach (var id in ids)
            {
                if (_notifications.TryGetValue(id, out var record) && record.IsImageOnly)
                {
                    if (record.ImageTimer != null)
                    {
                        record.ImageTimer.Stop();
                        record.ImageTimer.Dispose();
                        record.ImageTimer = null;
                    }

                    if (record.ImageForm != null && !record.ImageForm.IsDisposed)
                    {
                        _imageFormToIdMap.Remove(record.ImageForm);
                        record.ImageForm.Close();
                        record.ImageForm.Dispose();
                        record.ImageForm = null;
                    }
                }
            }

            _visibleImageFormOrder.Clear();
        }

        /// <summary>
        /// Reflows (repositions) all visible stacked image forms.
        /// </summary>
        private void ReflowImageForms()
        {
            if (_visibleImageFormOrder.Count == 0) return;

            Screen screen = Screen.PrimaryScreen;
            Rectangle workArea = screen.WorkingArea;
            string pos = (_position ?? "TopRight").ToLower();
            int gap = 10;
            bool stackDown = !pos.StartsWith("bottom");
            int cumulativeHeight = 0;

            foreach (var id in _visibleImageFormOrder)
            {
                if (!_notifications.TryGetValue(id, out var record)) continue;
                var form = record.ImageForm;
                if (form == null || form.IsDisposed) continue;

                int x, y;

                switch (pos)
                {
                    case "topleft":
                    case "bottomleft":
                        x = workArea.Left + 10;
                        break;
                    case "topcenter":
                    case "bottomcenter":
                    case "center":
                        x = workArea.Left + (workArea.Width - form.Width) / 2;
                        break;
                    default: // topright, bottomright
                        x = workArea.Right - form.Width - 10;
                        break;
                }

                if (stackDown)
                {
                    int baseY = pos == "center"
                        ? workArea.Top + (workArea.Height - form.Height) / 2
                        : workArea.Top + 10;
                    y = baseY + cumulativeHeight;
                }
                else
                {
                    int baseY = workArea.Bottom - form.Height - 10;
                    y = baseY - cumulativeHeight;
                }

                form.Location = new Point(x, y);
                cumulativeHeight += form.Height + gap;
            }
        }

        /// <summary>
        /// Shows a styled notification (non-tracked, legacy path).
        /// </summary>
        private void ShowStyledAlert(string title, string message)
        {
            try
            {
                var style = NotificationStyleRegistry.GetStyle(_styleName);
                if (style == null) return;

                Image icon = style.ShowIcon ? GetDisplayImage() : null;
                Color? bgOverride = ParseHexColor(_backgroundColor);
                Color? textOverride = ParseHexColor(_textColor);

                var form = new StyledNotificationForm(
                    style, title, message, _notificationType,
                    icon, _opacity, _pinned, _duration,
                    bgOverride, textOverride);

                form.Dismissed += (reason) =>
                {
                    _isVisible = false;
                    RaiseNotificationDismissed(reason);
                };

                form.Clicked += () =>
                {
                    RaiseNotificationClicked(title);
                };

                PositionImageOnlyForm(form);

                if (_soundEnabled)
                {
                    PlayNotificationSound();
                }

                form.Show();
                _isVisible = true;
                RaiseNotificationShown(title, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowStyledAlert error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a tracked styled notification with stacking support. Returns notification ID.
        /// </summary>
        private string ShowStyledAlertTracked(string title, string message)
        {
            try
            {
                var style = NotificationStyleRegistry.GetStyle(_styleName);
                if (style == null) return "";

                string id = GenerateNotificationId();
                Image icon = style.ShowIcon ? GetDisplayImage() : null;
                Color? bgOverride = ParseHexColor(_backgroundColor);
                Color? textOverride = ParseHexColor(_textColor);

                var form = new StyledNotificationForm(
                    style, title, message, _notificationType,
                    icon, _opacity, _pinned, _duration,
                    bgOverride, textOverride);

                var record = new NotificationRecord
                {
                    Id = id,
                    Title = title,
                    Message = message,
                    ImageForm = form,
                    IsImageOnly = false,
                    IsStyledForm = true
                };
                _notifications[id] = record;
                _imageFormToIdMap[form] = id;
                _visibleImageFormOrder.Add(id);

                var capturedId = id;
                form.Dismissed += (reason) =>
                {
                    DismissImageFormById(capturedId, reason);
                };

                form.Clicked += () =>
                {
                    RaiseNotificationClicked(title);
                    RaiseStackNotificationClicked(capturedId, title);
                };

                // Reflow positions (reuses existing stacking logic)
                ReflowImageForms();

                if (_soundEnabled)
                {
                    PlayNotificationSound();
                }

                form.Show();
                _isVisible = true;
                RaiseNotificationShown(title, message);
                RaiseStackNotificationShown(id, title, message);

                return id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowStyledAlertTracked error: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Parses a hex color string (#RRGGBB) to a Color. Returns null if invalid/empty.
        /// </summary>
        private Color? ParseHexColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor)) return null;
            try
            {
                return ColorTranslator.FromHtml(hexColor);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Positions the legacy image-only form
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
        /// Closes and disposes the legacy image-only form
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
        /// Updates text labels on a DevExpress AlertForm by traversing child controls.
        /// AlertForm contains a panel with LabelControl children for caption and text.
        /// </summary>
        private void UpdateAlertFormText(object alertFormRef, string title, string message)
        {
            try
            {
                var form = alertFormRef as Form;
                if (form == null || form.IsDisposed) return;

                // DevExpress AlertForm uses nested controls. Walk all controls to find text elements.
                // The AlertForm typically has: a caption label (bold) and a text label.
                bool foundCaption = false;
                foreach (Control ctrl in form.Controls)
                {
                    UpdateControlTextRecursive(ctrl, title, message, ref foundCaption);
                }

                form.Invalidate(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup UpdateAlertFormText error: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively searches for DevExpress LabelControl or Label controls to update text.
        /// The first text control found is treated as the caption/title, the second as the message.
        /// </summary>
        private void UpdateControlTextRecursive(Control control, string title, string message, ref bool foundCaption)
        {
            // Check if this is a text-displaying control (LabelControl or Label)
            string typeName = control.GetType().Name;
            if (typeName == "LabelControl" || typeName == "Label")
            {
                if (!foundCaption)
                {
                    control.Text = title;
                    foundCaption = true;
                }
                else
                {
                    control.Text = message;
                    return; // Found both, done
                }
            }

            // Recurse into child controls
            foreach (Control child in control.Controls)
            {
                UpdateControlTextRecursive(child, title, message, ref foundCaption);
            }
        }

        /// <summary>
        /// Finds or creates a ProgressBar on the AlertForm and sets its value.
        /// </summary>
        private void UpdateAlertFormProgress(object alertFormRef, int percent)
        {
            try
            {
                var form = alertFormRef as Form;
                if (form == null || form.IsDisposed) return;

                // Find existing ProgressBar
                ProgressBar progressBar = FindControlRecursive<ProgressBar>(form);

                if (progressBar == null)
                {
                    // Create a progress bar at the bottom of the alert form
                    progressBar = new ProgressBar();
                    progressBar.Minimum = 0;
                    progressBar.Maximum = 100;
                    progressBar.Height = 8;
                    progressBar.Dock = DockStyle.Bottom;
                    progressBar.Style = ProgressBarStyle.Continuous;
                    form.Controls.Add(progressBar);
                    progressBar.BringToFront();
                }

                progressBar.Value = percent;
                progressBar.Visible = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup UpdateAlertFormProgress error: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds the first control of type T in the form's control hierarchy.
        /// </summary>
        private T FindControlRecursive<T>(Control parent) where T : Control
        {
            foreach (Control child in parent.Controls)
            {
                if (child is T match) return match;

                T found = FindControlRecursive<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        private Image GetDisplayImage()
        {
            Image source = _customImage ?? GetNotificationIcon();
            if (source == null) return null;

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

        private Image GetNotificationIcon()
        {
            try
            {
                return NotificationIconRenderer.CreateIcon(_notificationType);
            }
            catch
            {
                return null;
            }
        }

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

                // Map AlertForm -> notification ID via AlertInfo.Tag
                // and store AlertForm reference on the record for in-place updates
                try
                {
                    var alertInfo = e.AlertForm.AlertInfo;
                    if (alertInfo?.Tag is string id && !string.IsNullOrEmpty(id))
                    {
                        _alertFormToIdMap[e.AlertForm] = id;

                        if (_notifications.TryGetValue(id, out var record))
                        {
                            record.AlertFormRef = e.AlertForm;
                        }
                    }
                }
                catch { }
            }
            catch { }
        }

        private void AlertControl_AlertClick(object sender, AlertClickEventArgs e)
        {
            try
            {
                // Try to get notification ID from AlertInfo.Tag
                string id = null;
                try { id = e.Info?.Tag as string; }
                catch { }

                string title = _title;
                if (!string.IsNullOrEmpty(id) && _notifications.TryGetValue(id, out var record))
                {
                    title = record.Title;
                }

                RaiseNotificationClicked(title);

                if (!string.IsNullOrEmpty(id))
                {
                    RaiseStackNotificationClicked(id, title);
                }
            }
            catch { }
        }

        private void AlertControl_FormClosing(object sender, AlertFormClosingEventArgs e)
        {
            try
            {
                // Try to find the notification ID for this closing form
                string id = null;

                // Try via AlertForm property on the event args
                try
                {
                    var alertFormProp = e.GetType().GetProperty("AlertForm");
                    if (alertFormProp != null)
                    {
                        var alertForm = alertFormProp.GetValue(e);
                        if (alertForm != null && _alertFormToIdMap.TryGetValue(alertForm, out var mappedId))
                        {
                            id = mappedId;
                            _alertFormToIdMap.Remove(alertForm);
                        }
                    }
                }
                catch { }

                // If we found a tracked notification, clean it up
                if (!string.IsNullOrEmpty(id))
                {
                    _notifications.Remove(id);
                    RaiseStackNotificationDismissed(id, "Timeout");
                }

                // Legacy behavior
                if (_notifications.Count == 0)
                {
                    _isVisible = false;
                }
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
                try { NotificationShown(title, message); }
                catch { }
            }
        }

        private void RaiseNotificationDismissed(string reason)
        {
            if (NotificationDismissed != null)
            {
                try { NotificationDismissed(reason); }
                catch { }
            }
        }

        private void RaiseNotificationClicked(string title)
        {
            if (NotificationClicked != null)
            {
                try { NotificationClicked(title); }
                catch { }
            }
        }

        private void RaiseStackNotificationShown(string notificationId, string title, string message)
        {
            if (StackNotificationShown != null)
            {
                try { StackNotificationShown(notificationId, title, message); }
                catch { }
            }
        }

        private void RaiseStackNotificationDismissed(string notificationId, string reason)
        {
            if (StackNotificationDismissed != null)
            {
                try { StackNotificationDismissed(notificationId, reason); }
                catch { }
            }
        }

        private void RaiseStackNotificationClicked(string notificationId, string title)
        {
            if (StackNotificationClicked != null)
            {
                try { StackNotificationClicked(notificationId, title); }
                catch { }
            }
        }

        private void RaiseNotificationUpdated(string notificationId, string title, string message)
        {
            if (NotificationUpdated != null)
            {
                try { NotificationUpdated(notificationId, title, message); }
                catch { }
            }
        }

        private void RaiseProgressChanged(string notificationId, int percent)
        {
            if (ProgressChanged != null)
            {
                try { ProgressChanged(notificationId, percent); }
                catch { }
            }
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Close legacy image form
                CloseImageOnlyForm();

                // Close all tracked image forms
                CloseAllImageForms();
                _imageOnlyQueue.Clear();
                _notifications.Clear();
                _alertFormToIdMap.Clear();
                _imageFormToIdMap.Clear();
                _visibleImageFormOrder.Clear();

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

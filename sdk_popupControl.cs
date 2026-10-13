using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace sdk_popup
{
    /// <summary>
    /// Toast notification COM control for Clarion (own WinForms notification forms, no third-party UI library).
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
            public Form ImageForm;          // null for standard alerts
            public Timer ImageTimer;        // null for standard alerts
            public bool IsImageOnly;
            public bool IsStyledForm;       // true for custom styled notifications
        }

        #endregion

        #region Fields

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
        private Dictionary<Form, string> _imageFormToIdMap = new Dictionary<Form, string>();
        private List<string> _visibleImageFormOrder = new List<string>();
        private Queue<NotificationRecord> _imageOnlyQueue = new Queue<NotificationRecord>();
        private int _nextNotificationId = 1;
        private int _maxStackSize;  // 0 = unlimited

        // --- v1.4.0 ---
        private string _soundPreset = "";
        private bool _deduplication;
        private int _rateLimit;  // 0 = no limit
        private List<DateTime> _recentToasts = new List<DateTime>();
        private bool _hoverPause = true;

        // --- v1.5.0 ---
        private bool _htmlEnabled;
        private int _monitorIndex = -1;  // -1 = auto (owner window)
        private readonly DoNotDisturbManager _dnd = new DoNotDisturbManager();
        private readonly NotificationHistory _history = new NotificationHistory();
        private bool _historyEnabled;
        private readonly PresetManager _presets = new PresetManager();
        private readonly List<ActionButton> _actionButtons = new List<ActionButton>();

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

        // Action button event delegate (v1.5.0)
        public delegate void ActionButtonClickedDelegate(string notificationId, string buttonId);

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

        // Action button events (v1.5.0)
        public event ActionButtonClickedDelegate ActionButtonClicked;

        #endregion

        #region Constructor

        public sdk_popupControl()
        {
            Size = new Size(1, 1);
            DoubleBuffered = true;
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


                // Clear all tracking
                _notifications.Clear();
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
                    $"{name}\nVersion: {versionStr}\n\nToast notification control for Clarion",
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

        // --- Sound Preset (v1.4.0) ---

        [ComVisible(true)]
        [Description("Sets a built-in sound preset: Success, Error, Warning, Chime, Ding, Silent")]
        public void SetSoundPreset(string preset)
        {
            _soundPreset = (preset ?? "").Trim().Trim('"');
        }

        [ComVisible(true)]
        [Description("Gets the current sound preset name")]
        public string GetSoundPreset()
        {
            return _soundPreset ?? "";
        }

        // --- Deduplication & Rate Limiting (v1.4.0) ---

        [ComVisible(true)]
        [Description("Enables deduplication: duplicate title+message updates existing notification in-place")]
        public void SetDeduplication(bool enabled)
        {
            _deduplication = enabled;
        }

        [ComVisible(true)]
        [Description("Gets whether deduplication is enabled")]
        public bool GetDeduplication()
        {
            return _deduplication;
        }

        [ComVisible(true)]
        [Description("Sets the maximum number of notifications per second (0 = unlimited)")]
        public void SetRateLimit(int maxPerSecond)
        {
            _rateLimit = Math.Max(0, maxPerSecond);
        }

        [ComVisible(true)]
        [Description("Gets the current rate limit")]
        public int GetRateLimit()
        {
            return _rateLimit;
        }

        // --- Hover Pause (v1.4.0) ---

        [ComVisible(true)]
        [Description("Enables pausing the auto-dismiss timer while mouse is over the notification")]
        public void SetHoverPause(bool enabled)
        {
            _hoverPause = enabled;
        }

        [ComVisible(true)]
        [Description("Gets whether hover-pause is enabled")]
        public bool GetHoverPause()
        {
            return _hoverPause;
        }

        #endregion

        #region Isdk_popup Methods (v1.5.0 HTML)

        [ComVisible(true)]
        [Description("Shows a notification with HTML-formatted message")]
        public void ShowNotificationHtml(string htmlMessage)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(ShowNotificationHtml), htmlMessage);
                    return;
                }
                bool prev = _htmlEnabled;
                _htmlEnabled = true;
                _message = htmlMessage ?? "";
                ShowAlert();
                _htmlEnabled = prev;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowNotificationHtml error: {ex.Message}");
            }
        }

        [ComVisible(true)]
        [Description("Shows a notification with HTML title and message")]
        public void ShowNotificationWithTitleHtml(string title, string htmlMessage)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string, string>(ShowNotificationWithTitleHtml), title, htmlMessage);
                    return;
                }
                bool prev = _htmlEnabled;
                _htmlEnabled = true;
                _title = title ?? "Notification";
                _message = htmlMessage ?? "";
                ShowAlert();
                _htmlEnabled = prev;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowNotificationWithTitleHtml error: {ex.Message}");
            }
        }

        [ComVisible(true)]
        [Description("Shows a notification with HTML content, returns notification ID")]
        public string ShowNotificationHtmlEx(string title, string htmlMessage)
        {
            try
            {
                if (InvokeRequired)
                {
                    return (string)Invoke(new Func<string, string, string>(ShowNotificationHtmlEx), title, htmlMessage);
                }
                bool prev = _htmlEnabled;
                _htmlEnabled = true;
                string t = title ?? "Notification";
                string m = htmlMessage ?? "";
                _title = t;
                _message = m;
                string id = ShowAlertTracked(t, m);
                _htmlEnabled = prev;
                return id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowNotificationHtmlEx error: {ex.Message}");
                return "";
            }
        }

        [ComVisible(true)]
        [Description("Enables HTML input for text (tags are stripped, <br> becomes a line break, entities are decoded)")]
        public void SetHtmlEnabled(bool enabled)
        {
            _htmlEnabled = enabled;
        }

        [ComVisible(true)]
        [Description("Gets whether HTML rendering is enabled")]
        public bool GetHtmlEnabled()
        {
            return _htmlEnabled;
        }

        #endregion

        #region Isdk_popup Methods (v1.5.0 Multi-Monitor)

        [ComVisible(true)]
        [Description("Sets the display monitor: -1 = auto, 0+ = specific monitor index")]
        public void SetDisplayMonitor(int monitorIndex)
        {
            _monitorIndex = monitorIndex;
        }

        [ComVisible(true)]
        [Description("Gets the configured display monitor index")]
        public int GetDisplayMonitor()
        {
            return _monitorIndex;
        }

        [ComVisible(true)]
        [Description("Gets the number of monitors on the system")]
        public int GetMonitorCount()
        {
            try { return Screen.AllScreens.Length; }
            catch { return 1; }
        }

        #endregion

        #region Isdk_popup Methods (v1.5.0 ShowPending / Resolve / Reject)

        [ComVisible(true)]
        [Description("Shows a pinned notification with progress bar - call ResolveNotification or RejectNotification when done")]
        public string ShowPending(string title, string message)
        {
            try
            {
                if (InvokeRequired)
                {
                    return (string)Invoke(new Func<string, string, string>(ShowPending), title, message);
                }

                _title = title ?? "";
                _message = message ?? "";
                _notificationType = "Info";
                bool prevPinned = _pinned;
                _pinned = true;

                string id = ShowAlertTracked(_title, _message);

                _pinned = prevPinned;

                if (!string.IsNullOrEmpty(id))
                {
                    // Inject a 0% progress bar so users see "work in progress"
                    UpdateProgress(id, 0);
                }

                return id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowPending error: {ex.Message}");
                return "";
            }
        }

        [ComVisible(true)]
        [Description("Resolves a pending notification: turns it into a Success notification and auto-dismisses")]
        public bool ResolveNotification(string notificationId, string successMessage)
        {
            try
            {
                if (InvokeRequired)
                {
                    return (bool)Invoke(new Func<string, string, bool>(ResolveNotification), notificationId, successMessage);
                }

                if (string.IsNullOrEmpty(notificationId)) return false;
                if (!_notifications.TryGetValue(notificationId, out var record)) return false;

                string newMessage = successMessage ?? "Done";
                _notificationType = "Success";

                // Update text + progress to 100%
                UpdateNotificationFull(notificationId, record.Title, newMessage, 100);

                // Schedule dismissal after a short display time (3 seconds)
                var closeTimer = new Timer { Interval = 3000 };
                closeTimer.Tick += (s, e) =>
                {
                    closeTimer.Stop();
                    closeTimer.Dispose();
                    try { DismissNotificationById(notificationId); } catch { }
                };
                closeTimer.Start();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ResolveNotification error: {ex.Message}");
                return false;
            }
        }

        [ComVisible(true)]
        [Description("Rejects a pending notification: turns it into an Error notification that stays pinned until dismissed")]
        public bool RejectNotification(string notificationId, string errorMessage)
        {
            try
            {
                if (InvokeRequired)
                {
                    return (bool)Invoke(new Func<string, string, bool>(RejectNotification), notificationId, errorMessage);
                }

                if (string.IsNullOrEmpty(notificationId)) return false;
                if (!_notifications.TryGetValue(notificationId, out var record)) return false;

                string newMessage = errorMessage ?? "Failed";
                _notificationType = "Error";

                UpdateNotification(notificationId, record.Title, newMessage);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup RejectNotification error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Isdk_popup Methods (v1.5.0 Do Not Disturb)

        [ComVisible(true)]
        [Description("Enables or disables Do Not Disturb mode")]
        public void SetDoNotDisturb(bool enabled)
        {
            _dnd.Manual = enabled;
        }

        [ComVisible(true)]
        [Description("Returns whether DND is currently active (manual, timer, or scheduled)")]
        public bool GetDoNotDisturb()
        {
            return _dnd.IsActive();
        }

        [ComVisible(true)]
        [Description("Enables DND for the given number of minutes, then auto-disables")]
        public void SetDoNotDisturbMinutes(int minutes)
        {
            _dnd.SetTimer(minutes);
        }

        [ComVisible(true)]
        [Description("Sets a daily DND schedule. Times in HH:MM format. Supports overnight windows.")]
        public void SetDoNotDisturbSchedule(string startHHMM, string endHHMM)
        {
            _dnd.SetSchedule(startHHMM, endHHMM);
        }

        [ComVisible(true)]
        [Description("Clears the DND schedule")]
        public void ClearDoNotDisturbSchedule()
        {
            _dnd.ClearSchedule();
        }

        [ComVisible(true)]
        [Description("Returns DND reason: manual, timer, scheduled, or empty if inactive")]
        public string GetDoNotDisturbReason()
        {
            return _dnd.GetReason();
        }

        #endregion

        #region Isdk_popup Methods (v1.5.0 Notification History)

        [ComVisible(true)]
        [Description("Enables or disables notification history logging")]
        public void SetHistoryEnabled(bool enabled)
        {
            _historyEnabled = enabled;
        }

        [ComVisible(true)]
        [Description("Gets whether history logging is enabled")]
        public bool GetHistoryEnabled()
        {
            return _historyEnabled;
        }

        [ComVisible(true)]
        [Description("Sets the maximum number of history entries retained (default 50)")]
        public void SetHistoryMaxSize(int maxSize)
        {
            _history.MaxSize = maxSize;
        }

        [ComVisible(true)]
        [Description("Gets the current number of entries in history")]
        public int GetHistoryCount()
        {
            return _history.Count;
        }

        [ComVisible(true)]
        [Description("Returns a JSON entry by index (0 = newest). Empty string if out of range.")]
        public string GetHistoryItemJson(int index)
        {
            return _history.GetJson(index);
        }

        [ComVisible(true)]
        [Description("Clears the history")]
        public void ClearHistory()
        {
            _history.Clear();
        }

        #endregion

        #region Isdk_popup Methods (v1.5.0 JSON Presets)

        [ComVisible(true)]
        [Description("Saves current settings as a named preset")]
        public void SavePreset(string name)
        {
            _presets.Save(name, CaptureCurrentPreset());
        }

        [ComVisible(true)]
        [Description("Loads a preset by name. Returns true if found.")]
        public bool LoadPreset(string name)
        {
            var p = _presets.Get(name);
            if (p == null) return false;
            ApplyPreset(p);
            return true;
        }

        [ComVisible(true)]
        [Description("Deletes a preset by name. Returns true if found and deleted.")]
        public bool DeletePreset(string name)
        {
            return _presets.Delete(name);
        }

        [ComVisible(true)]
        [Description("Returns comma-separated list of saved preset names")]
        public string ListPresets()
        {
            return _presets.ListNames();
        }

        [ComVisible(true)]
        [Description("Shows a notification from a JSON config string")]
        public string ShowFromJson(string jsonConfig)
        {
            try
            {
                if (InvokeRequired)
                {
                    return (string)Invoke(new Func<string, string>(ShowFromJson), jsonConfig);
                }

                if (string.IsNullOrEmpty(jsonConfig)) return "";

                var parser = new MiniJson(jsonConfig);
                var d = parser.ParseValue() as System.Collections.Generic.Dictionary<string, object>;
                if (d == null) return "";

                string title = PresetManager.GetString(d, "title", "Notification");
                string message = PresetManager.GetString(d, "message", "");

                // Apply optional settings if present
                ApplyIfPresent(d);

                return ShowAlertTracked(title, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup ShowFromJson error: {ex.Message}");
                return "";
            }
        }

        #endregion

        #region Isdk_popup Methods (v1.5.0 Action Buttons)

        [ComVisible(true)]
        [Description("Adds an action button to upcoming notifications")]
        public void AddActionButton(string buttonId, string caption, bool isPrimary)
        {
            if (string.IsNullOrEmpty(buttonId) || string.IsNullOrEmpty(caption)) return;
            _actionButtons.Add(new ActionButton
            {
                Id = buttonId.Trim(),
                Caption = caption.Trim(),
                IsPrimary = isPrimary
            });
        }

        [ComVisible(true)]
        [Description("Removes all configured action buttons")]
        public void ClearActionButtons()
        {
            _actionButtons.Clear();
        }

        [ComVisible(true)]
        [Description("Returns the current number of configured action buttons")]
        public int GetActionButtonCount()
        {
            return _actionButtons.Count;
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

                if (ShouldThrottle())
                {
                    System.Diagnostics.Trace.WriteLine("sdk_popup: rate limit exceeded, dropped image notification");
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

                    // Hover-pause
                    if (_hoverPause)
                    {
                        var capturedForm = _imageOnlyForm;
                        EventHandler onEnter = (s, _) => { try { _imageOnlyTimer?.Stop(); } catch { } };
                        EventHandler onLeave = (s, _) =>
                        {
                            try
                            {
                                if (capturedForm != null && !capturedForm.IsDisposed &&
                                    !capturedForm.ClientRectangle.Contains(capturedForm.PointToClient(Cursor.Position)))
                                {
                                    _imageOnlyTimer?.Start();
                                }
                            }
                            catch { }
                        };
                        _imageOnlyForm.MouseEnter += onEnter;
                        _imageOnlyForm.MouseLeave += onLeave;
                        pb.MouseEnter += onEnter;
                        pb.MouseLeave += onLeave;
                    }
                }

                if (_soundEnabled)
                {
                    PlayNotificationSound();
                }

                _imageOnlyForm.Show();
                _isVisible = true;
                RecordRecentToast();
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

                DismissImageFormRecord(record, "Programmatic");
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
            int count = _visibleImageFormOrder.Count;

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


                // Fire dismissed events for all tracked notifications
                foreach (var kvp in new Dictionary<string, NotificationRecord>(_notifications))
                {
                    RaiseStackNotificationDismissed(kvp.Key, "Programmatic");
                }

                // Clear all tracking
                _notifications.Clear();
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

                return false;
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

                return false;
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

                return false;
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
        /// Returns true if a new notification would exceed the rate limit. Trims the buffer as a side effect.
        /// </summary>
        private bool ShouldThrottle()
        {
            TrimRecentToasts();
            if (_rateLimit <= 0) return false;

            DateTime now = DateTime.UtcNow;
            int recent = 0;
            for (int i = 0; i < _recentToasts.Count; i++)
            {
                if ((now - _recentToasts[i]).TotalMilliseconds < 1000)
                    recent++;
            }
            return recent >= _rateLimit;
        }

        /// <summary>
        /// If deduplication is on and a tracked notification with matching title+message is visible,
        /// update it in-place and return its ID. Otherwise returns null.
        /// </summary>
        private string TryDedupUpdate(string title, string message)
        {
            if (!_deduplication) return null;

            string t = title ?? "";
            string m = message ?? "";

            foreach (var kvp in _notifications)
            {
                var r = kvp.Value;
                if (r == null || r.IsImageOnly) continue;
                if ((r.Title ?? "") == t && (r.Message ?? "") == m)
                {
                    UpdateNotification(r.Id, t, m);
                    return r.Id;
                }
            }
            return null;
        }

        private void RecordRecentToast()
        {
            _recentToasts.Add(DateTime.UtcNow);
            TrimRecentToasts();
        }

        private void TrimRecentToasts()
        {
            DateTime cutoff = DateTime.UtcNow.AddSeconds(-2);
            while (_recentToasts.Count > 0 && _recentToasts[0] < cutoff)
            {
                _recentToasts.RemoveAt(0);
            }
        }

        /// <summary>
        /// v1.5: Returns the target Screen based on the monitor index setting.
        /// -1 or invalid index = follow owner window or fall back to primary.
        /// </summary>
        private Screen GetTargetScreen()
        {
            try
            {
                if (_monitorIndex < 0)
                {
                    // Auto: follow owner form if available
                    Form owner = FindForm();
                    if (owner != null && !owner.IsDisposed)
                    {
                        return Screen.FromControl(owner);
                    }
                    return Screen.PrimaryScreen;
                }

                var all = Screen.AllScreens;
                if (_monitorIndex >= 0 && _monitorIndex < all.Length)
                {
                    return all[_monitorIndex];
                }
            }
            catch { }
            return Screen.PrimaryScreen;
        }

        /// <summary>
        /// v1.5: Captures current settings as a Preset object.
        /// </summary>
        private PresetManager.Preset CaptureCurrentPreset()
        {
            return new PresetManager.Preset
            {
                Style        = _styleName,
                Type         = _notificationType,
                Position     = _position,
                Duration     = _duration,
                Opacity      = _opacity,
                Pinned       = _pinned,
                BgColor      = _backgroundColor,
                TextColor    = _textColor,
                SoundPreset  = _soundPreset,
                SoundEnabled = _soundEnabled,
                HoverPause   = _hoverPause
            };
        }

        /// <summary>
        /// v1.5: Applies a preset's settings to the current state.
        /// </summary>
        private void ApplyPreset(PresetManager.Preset p)
        {
            if (p == null) return;
            _styleName         = p.Style ?? "Default";
            _notificationType  = p.Type ?? "Info";
            _position          = p.Position ?? "TopRight";
            _duration          = Math.Max(500, p.Duration);
            _opacity           = Math.Max(10, Math.Min(100, p.Opacity));
            _pinned            = p.Pinned;
            _backgroundColor   = p.BgColor ?? "";
            _textColor         = p.TextColor ?? "";
            _soundPreset       = p.SoundPreset ?? "";
            _soundEnabled      = p.SoundEnabled;
            _hoverPause        = p.HoverPause;
        }

        /// <summary>
        /// v1.5: Applies optional settings from a parsed JSON dictionary (for ShowFromJson).
        /// </summary>
        private void ApplyIfPresent(System.Collections.Generic.Dictionary<string, object> d)
        {
            if (d == null) return;

            string style     = PresetManager.GetString(d, "style", null);
            string type      = PresetManager.GetString(d, "type", null);
            string position  = PresetManager.GetString(d, "position", null);

            if (style != null) SetStyle(style);
            if (type != null) _notificationType = type;
            if (position != null) _position = position;

            if (d.ContainsKey("duration")) SetDuration(PresetManager.GetInt(d, "duration", _duration));
            if (d.ContainsKey("opacity"))  SetOpacity(PresetManager.GetInt(d, "opacity", _opacity));
            if (d.ContainsKey("pinned"))   _pinned = PresetManager.GetBool(d, "pinned", _pinned);

            string bg = PresetManager.GetString(d, "bgColor", null);
            if (bg != null) _backgroundColor = bg;
            string tx = PresetManager.GetString(d, "textColor", null);
            if (tx != null) _textColor = tx;
        }

        /// <summary>
        /// v1.5: Logs a dismissed notification to history if enabled.
        /// </summary>
        private void LogToHistory(string title, string message, string type, string reason)
        {
            if (_historyEnabled)
            {
                _history.Add(title, message, type, reason);
            }
        }

        /// <summary>
        /// v1.5: Converts HTML text to plain text for the notification forms (which draw plain text):
        /// br-tags become line breaks, other tags are removed, entities (&amp;amp; etc.) are decoded.
        /// </summary>
        private static string StripHtml(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            s = System.Text.RegularExpressions.Regex.Replace(s, @"<br\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            s = System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "");
            return System.Net.WebUtility.HtmlDecode(s);
        }

        /// <summary>
        /// Shows a tracked standard alert notification and returns its ID.
        /// </summary>
        private string ShowAlertTracked(string title, string message)
        {
            // DND: suppress and log to history if enabled
            if (_dnd.IsActive())
            {
                LogToHistory(title, message, _notificationType, "Suppressed (DND: " + _dnd.GetReason() + ")");
                System.Diagnostics.Trace.WriteLine("sdk_popup: suppressed by DND (" + _dnd.GetReason() + ")");
                return "";
            }

            // Dedup: reuse existing notification if same title+message is visible
            string dedupId = TryDedupUpdate(title, message);
            if (dedupId != null) return dedupId;

            // Rate limit: drop if over threshold
            if (ShouldThrottle())
            {
                System.Diagnostics.Trace.WriteLine("sdk_popup: rate limit exceeded, dropped notification");
                return "";
            }

            return ShowStyledAlertTracked(title, message);
        }

        /// <summary>
        /// Shows a notification (legacy, non-tracked)
        /// </summary>
        private void ShowAlert()
        {
            // DND: suppress (and log) if enabled
            if (_dnd.IsActive())
            {
                LogToHistory(_title, _message, _notificationType, "Suppressed (DND: " + _dnd.GetReason() + ")");
                System.Diagnostics.Trace.WriteLine("sdk_popup: suppressed by DND (" + _dnd.GetReason() + ")");
                return;
            }

            // Rate limit: drop if over threshold
            if (ShouldThrottle())
            {
                System.Diagnostics.Trace.WriteLine("sdk_popup: rate limit exceeded, dropped notification");
                return;
            }

            ShowStyledAlertTracked(_title, _message);   // tracked, so it stacks with the other notifications
        }

        /// <summary>
        /// Shows a tracked image-only notification with stacking support.
        /// </summary>
        private string ShowImageTracked()
        {
            // Rate limit (image-only notifications don't support dedup since no text)
            if (ShouldThrottle())
            {
                System.Diagnostics.Trace.WriteLine("sdk_popup: rate limit exceeded, dropped image notification");
                return "";
            }

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

                // Hover-pause: pause timer while mouse is over the form
                if (_hoverPause)
                {
                    var capturedTimer = timer;
                    var capturedForm = form;
                    EventHandler onEnter = (s, _) => { try { capturedTimer.Stop(); } catch { } };
                    EventHandler onLeave = (s, _) =>
                    {
                        try
                        {
                            if (!capturedForm.IsDisposed &&
                                !capturedForm.ClientRectangle.Contains(capturedForm.PointToClient(Cursor.Position)))
                            {
                                capturedTimer.Start();
                            }
                        }
                        catch { }
                    };
                    form.MouseEnter += onEnter;
                    form.MouseLeave += onLeave;
                    pb.MouseEnter += onEnter;
                    pb.MouseLeave += onLeave;
                }
            }

            if (_soundEnabled)
            {
                PlayNotificationSound();
            }

            form.Show();
            _isVisible = true;
            RecordRecentToast();
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

            // v1.5: history log
            LogToHistory(record.Title, record.Message, _notificationType, reason);

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
                if (_notifications.TryGetValue(id, out var record) && (record.IsImageOnly || record.IsStyledForm))
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

            Screen screen = GetTargetScreen();
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
                    style, StripHtml(title), StripHtml(message), _notificationType,
                    icon, _opacity, _pinned, _duration,
                    bgOverride, textOverride, _hoverPause,
                    _actionButtons.Count > 0 ? new List<ActionButton>(_actionButtons) : null);

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

                form.ActionClicked += (buttonId) =>
                {
                    RaiseActionButtonClicked(capturedId, buttonId);
                };

                // Reflow positions (reuses existing stacking logic)
                ReflowImageForms();

                if (_soundEnabled)
                {
                    PlayNotificationSound();
                }

                form.Show();
                _isVisible = true;
                RecordRecentToast();
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
            Screen screen = GetTargetScreen();
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
                // Preset takes precedence over custom path
                if (!string.IsNullOrEmpty(_soundPreset))
                {
                    PlayPresetSound(_soundPreset);
                    return;
                }

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

        private void PlayPresetSound(string preset)
        {
            switch ((preset ?? "").Trim().ToLower())
            {
                case "success":
                    SystemSounds.Asterisk.Play();
                    break;
                case "error":
                    SystemSounds.Hand.Play();
                    break;
                case "warning":
                    SystemSounds.Exclamation.Play();
                    break;
                case "chime":
                    PlayMediaFile("chimes.wav", SystemSounds.Asterisk);
                    break;
                case "ding":
                    PlayMediaFile("ding.wav", SystemSounds.Beep);
                    break;
                case "silent":
                    break;
                default:
                    SystemSounds.Asterisk.Play();
                    break;
            }
        }

        private void PlayMediaFile(string fileName, SystemSound fallback)
        {
            try
            {
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string path = Path.Combine(winDir, "Media", fileName);
                if (File.Exists(path))
                {
                    using (var player = new SoundPlayer(path))
                    {
                        player.Play();
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"sdk_popup PlayMediaFile error: {ex.Message}");
            }
            try { fallback.Play(); } catch { }
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

        private void RaiseActionButtonClicked(string notificationId, string buttonId)
        {
            if (ActionButtonClicked != null)
            {
                try { ActionButtonClicked(notificationId, buttonId); }
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
                _imageFormToIdMap.Clear();
                _visibleImageFormOrder.Clear();

                _customImage?.Dispose();
                _customImage = null;
            }

            base.Dispose(disposing);
        }

        #endregion
    }

}

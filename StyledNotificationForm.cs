using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace sdk_popup
{
    /// <summary>
    /// Custom borderless notification form with GDI+ rendering.
    /// Supports rounded corners, accent bars, fade-in animation, close button,
    /// progress bar, and in-place text/progress updates.
    /// </summary>
    internal class StyledNotificationForm : Form
    {
        private readonly NotificationStyle _style;
        private string _title;
        private string _message;
        private readonly Color _accentColor;
        private readonly Image _icon;
        private readonly double _targetOpacity;
        private readonly bool _pinned;
        private readonly int _duration;
        private int _progressPercent = -1; // -1 = no progress bar

        private Timer _fadeInTimer;
        private Timer _autoCloseTimer;
        private double _currentOpacity;
        private bool _closeButtonHovered;
        private Rectangle _closeButtonRect;
        private Color _transparencyKey = Color.Empty;
        private bool _disposed;

        // Color overrides from SetBackgroundColor/SetTextColor
        private readonly Color? _bgOverride;
        private readonly Color? _textOverride;

        public event Action<string> Dismissed;
        public event Action Clicked;

        public StyledNotificationForm(
            NotificationStyle style,
            string title,
            string message,
            string notificationType,
            Image icon,
            int opacity,
            bool pinned,
            int duration,
            Color? bgOverride = null,
            Color? textOverride = null)
        {
            _style = style;
            _title = title ?? "";
            _message = message ?? "";
            _accentColor = NotificationStyleRegistry.GetAccentColor(notificationType);
            _icon = icon;
            _targetOpacity = Math.Max(0.1, Math.Min(1.0, opacity / 100.0));
            _pinned = pinned;
            _duration = Math.Max(500, duration);
            _bgOverride = bgOverride;
            _textOverride = textOverride;

            InitializeForm();
        }

        private void InitializeForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;

            // Calculate form height based on text content
            int formHeight = CalculateFormHeight();
            ClientSize = new Size(_style.FormWidth, formHeight);

            // Smooth rounded corners via TransparencyKey (avoids jagged Region edges)
            if (_style.CornerRadius > 0)
            {
                _transparencyKey = Color.FromArgb(1, 0, 1);
                AllowTransparency = true;
                BackColor = _transparencyKey;
                TransparencyKey = _transparencyKey;
            }

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // WS_EX_TOOLWINDOW: hide from taskbar and alt-tab
                cp.ExStyle |= 0x80;
                // CS_DROPSHADOW: native OS drop shadow
                // Guard: _style may be null if CreateParams is accessed during base constructor
                // CS_DROPSHADOW only works with rectangular forms (no TransparencyKey)
                if (_style != null && _style.UseDropShadow && _style.CornerRadius == 0)
                {
                    cp.ClassStyle |= 0x00020000;
                }
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Fade-in animation
            Opacity = 0;
            _currentOpacity = 0;
            StartFadeIn();

            if (!_pinned)
            {
                _autoCloseTimer = new Timer();
                _autoCloseTimer.Interval = _duration;
                _autoCloseTimer.Tick += (s, args) =>
                {
                    _autoCloseTimer.Stop();
                    DismissWithFade("Timeout");
                };
                _autoCloseTimer.Start();
            }
        }

        #region Public Update Methods

        public void UpdateText(string title, string message)
        {
            _title = title ?? "";
            _message = message ?? "";

            // Recalculate height
            int newHeight = CalculateFormHeight();
            if (ClientSize.Height != newHeight)
            {
                ClientSize = new Size(_style.FormWidth, newHeight);
            }

            Invalidate();
        }

        public void UpdateProgress(int percent)
        {
            _progressPercent = Math.Max(0, Math.Min(100, percent));
            Invalidate();
        }

        #endregion

        #region Painting

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var bounds = ClientRectangle;

            // Clear with TransparencyKey for smooth rounded corners
            if (_transparencyKey != Color.Empty)
            {
                g.Clear(_transparencyKey);
            }

            Color bgColor = _bgOverride ?? GetEffectiveBackgroundColor();

            // 1. Background
            if (_style.UseGradient && !_bgOverride.HasValue)
            {
                Color gradStart = Color.FromArgb(220, _accentColor.R, _accentColor.G, _accentColor.B);
                Color gradEnd = Color.FromArgb(180, _accentColor.R, _accentColor.G, _accentColor.B);
                using (var brush = new LinearGradientBrush(bounds, gradStart, gradEnd, 135f))
                {
                    FillRoundedRect(g, brush, bounds, _style.CornerRadius);
                }
            }
            else
            {
                using (var brush = new SolidBrush(bgColor))
                {
                    FillRoundedRect(g, brush, bounds, _style.CornerRadius);
                }
            }

            // 2. Border
            if (_style.BorderWidth > 0)
            {
                using (var pen = new Pen(_style.BorderColor, _style.BorderWidth))
                {
                    DrawRoundedRect(g, pen, bounds, _style.CornerRadius);
                }
            }

            // 3. Accent bar (left side)
            if (_style.ShowAccentBar && _style.AccentBarWidth > 0)
            {
                var barRect = new Rectangle(0, 0, _style.AccentBarWidth, bounds.Height);
                using (var brush = new SolidBrush(_accentColor))
                {
                    if (_style.CornerRadius > 0)
                    {
                        // Clip to left rounded edge
                        var clipPath = CreateRoundedRectPath(bounds, _style.CornerRadius);
                        var oldClip = g.Clip;
                        g.SetClip(clipPath, CombineMode.Intersect);
                        g.FillRectangle(brush, barRect);
                        g.Clip = oldClip;
                        clipPath.Dispose();
                    }
                    else
                    {
                        g.FillRectangle(brush, barRect);
                    }
                }
            }

            // 4. Bottom border (Minimal style)
            if (_style.ShowBottomBorder && _style.BottomBorderHeight > 0)
            {
                var borderRect = new Rectangle(0, bounds.Height - _style.BottomBorderHeight, bounds.Width, _style.BottomBorderHeight);
                using (var brush = new SolidBrush(_accentColor))
                {
                    g.FillRectangle(brush, borderRect);
                }
            }

            // Calculate content area
            int contentLeft = _style.PaddingLeft;
            if (_style.ShowAccentBar) contentLeft = Math.Max(contentLeft, _style.AccentBarWidth + 8);

            int iconSize = 0;
            int iconLeft = contentLeft;
            int iconTop = _style.PaddingTop;

            // 5. Icon
            if (_style.ShowIcon && _icon != null)
            {
                iconSize = 32;
                var iconRect = new Rectangle(iconLeft, iconTop, iconSize, iconSize);
                g.DrawImage(_icon, iconRect);
                contentLeft = iconLeft + iconSize + 8;
            }

            // 6. Close button
            if (_style.ShowCloseButton)
            {
                int btnSize = 16;
                int btnX = bounds.Width - _style.PaddingRight - btnSize;
                int btnY = _style.PaddingTop;
                _closeButtonRect = new Rectangle(btnX, btnY, btnSize, btnSize);

                Color btnColor = _closeButtonHovered ? _style.CloseButtonHoverColor : _style.CloseButtonColor;
                using (var pen = new Pen(btnColor, 1.5f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    int m = 3; // margin inside button rect
                    g.DrawLine(pen, btnX + m, btnY + m, btnX + btnSize - m, btnY + btnSize - m);
                    g.DrawLine(pen, btnX + btnSize - m, btnY + m, btnX + m, btnY + btnSize - m);
                }
            }

            // 7. Text area
            int textLeft = contentLeft;
            int textRight = _style.ShowCloseButton ? _closeButtonRect.Left - 8 : bounds.Width - _style.PaddingRight;
            int textWidth = textRight - textLeft;
            int textTop = _style.PaddingTop;

            Color titleColor = _textOverride ?? _style.TitleColor;
            Color messageColor = _textOverride ?? _style.MessageColor;

            // Title
            if (!string.IsNullOrEmpty(_title))
            {
                using (var font = new Font(_style.FontFamily, _style.TitleFontSize, _style.TitleFontStyle, GraphicsUnit.Point))
                using (var brush = new SolidBrush(titleColor))
                {
                    var titleRect = new RectangleF(textLeft, textTop, textWidth, 0);
                    var sf = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoClip };
                    var titleSize = g.MeasureString(_title, font, textWidth, sf);
                    titleRect.Height = titleSize.Height;
                    g.DrawString(_title, font, brush, titleRect, sf);
                    textTop += (int)Math.Ceiling(titleSize.Height) + 2;
                }
            }

            // Message
            if (!string.IsNullOrEmpty(_message))
            {
                using (var font = new Font(_style.FontFamily, _style.MessageFontSize, _style.MessageFontStyle, GraphicsUnit.Point))
                using (var brush = new SolidBrush(messageColor))
                {
                    var msgRect = new RectangleF(textLeft, textTop, textWidth, 0);
                    var sf = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoClip };
                    var msgSize = g.MeasureString(_message, font, textWidth, sf);
                    msgRect.Height = msgSize.Height;
                    g.DrawString(_message, font, brush, msgRect, sf);
                }
            }

            // 8. Progress bar
            if (_progressPercent >= 0)
            {
                int barHeight = _style.ProgressBarHeight;
                int barY = bounds.Height - barHeight - (_style.ShowBottomBorder ? _style.BottomBorderHeight : 0);
                var trackRect = new Rectangle(0, barY, bounds.Width, barHeight);
                var fillWidth = (int)(bounds.Width * (_progressPercent / 100.0));
                var fillRect = new Rectangle(0, barY, fillWidth, barHeight);

                Color trackColor = _style.ProgressBarBackColor;
                Color fillColor = _style.ProgressBarFillColor == Color.Empty ? _accentColor : _style.ProgressBarFillColor;

                // Clip to rounded rect to prevent bleeding at bottom corners
                GraphicsPath progressClip = null;
                Region oldProgressClip = null;
                if (_style.CornerRadius > 0)
                {
                    progressClip = CreateRoundedRectPath(bounds, _style.CornerRadius);
                    oldProgressClip = g.Clip;
                    g.SetClip(progressClip, CombineMode.Intersect);
                }

                using (var trackBrush = new SolidBrush(trackColor))
                {
                    g.FillRectangle(trackBrush, trackRect);
                }
                if (fillWidth > 0)
                {
                    using (var fillBrush = new SolidBrush(fillColor))
                    {
                        g.FillRectangle(fillBrush, fillRect);
                    }
                }

                if (progressClip != null)
                {
                    g.Clip = oldProgressClip;
                    progressClip.Dispose();
                }
            }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("sdk_popup OnPaint error: " + ex.Message);
            }
        }

        private Color GetEffectiveBackgroundColor()
        {
            if (_style.BackgroundColor != Color.Empty)
                return _style.BackgroundColor;
            // Rounded style: derive from accent color
            return Color.FromArgb(220, _accentColor.R, _accentColor.G, _accentColor.B);
        }

        #endregion

        #region Rounded Rectangle Helpers

        private GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;

            if (d <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void FillRoundedRect(Graphics g, Brush brush, Rectangle rect, int radius)
        {
            if (radius <= 0)
            {
                g.FillRectangle(brush, rect);
                return;
            }
            using (var path = CreateRoundedRectPath(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        private void DrawRoundedRect(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            if (radius <= 0)
            {
                g.DrawRectangle(pen, rect);
                return;
            }
            // Inset by pen width to keep border inside bounds
            var inset = new Rectangle(
                rect.X + (int)pen.Width / 2,
                rect.Y + (int)pen.Width / 2,
                rect.Width - (int)pen.Width,
                rect.Height - (int)pen.Width);
            using (var path = CreateRoundedRectPath(inset, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        #endregion

        #region Height Calculation

        private int CalculateFormHeight()
        {
            int height = _style.PaddingTop + _style.PaddingBottom;
            int textWidth = _style.FormWidth - _style.PaddingRight;

            int contentLeft = _style.PaddingLeft;
            if (_style.ShowAccentBar) contentLeft = Math.Max(contentLeft, _style.AccentBarWidth + 8);
            if (_style.ShowIcon && _icon != null) contentLeft += 32 + 8;
            if (_style.ShowCloseButton) textWidth -= 24;
            textWidth -= contentLeft;

            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                if (!string.IsNullOrEmpty(_title))
                {
                    using (var font = new Font(_style.FontFamily, _style.TitleFontSize, _style.TitleFontStyle, GraphicsUnit.Point))
                    {
                        var size = g.MeasureString(_title, font, Math.Max(1, textWidth));
                        height += (int)Math.Ceiling(size.Height) + 2;
                    }
                }

                if (!string.IsNullOrEmpty(_message))
                {
                    using (var font = new Font(_style.FontFamily, _style.MessageFontSize, _style.MessageFontStyle, GraphicsUnit.Point))
                    {
                        var size = g.MeasureString(_message, font, Math.Max(1, textWidth));
                        height += (int)Math.Ceiling(size.Height);
                    }
                }
            }

            // Minimum height (accommodate icon)
            if (_style.ShowIcon && _icon != null)
            {
                height = Math.Max(height, _style.PaddingTop + 32 + _style.PaddingBottom);
            }

            // Progress bar
            if (_progressPercent >= 0)
            {
                height += _style.ProgressBarHeight + 4;
            }

            // Bottom border
            if (_style.ShowBottomBorder)
            {
                height += _style.BottomBorderHeight;
            }

            return Math.Max(height, 48);
        }

        #endregion

        #region Fade-In Animation

        private void StartFadeIn()
        {
            _fadeInTimer = new Timer();
            _fadeInTimer.Interval = 20;
            _fadeInTimer.Tick += FadeInTick;
            _fadeInTimer.Start();
        }

        private void FadeInTick(object sender, EventArgs e)
        {
            _currentOpacity += 0.1;
            if (_currentOpacity >= _targetOpacity)
            {
                _currentOpacity = _targetOpacity;
                Opacity = _currentOpacity;
                _fadeInTimer.Stop();
                _fadeInTimer.Dispose();
                _fadeInTimer = null;
            }
            else
            {
                Opacity = _currentOpacity;
            }
        }

        #endregion

        #region Fade-Out and Dismiss

        private void DismissWithFade(string reason)
        {
            if (_disposed) return;

            // Quick fade-out
            var fadeOut = new Timer();
            fadeOut.Interval = 20;
            double opacity = Opacity;
            fadeOut.Tick += (s, e) =>
            {
                opacity -= 0.15;
                if (opacity <= 0)
                {
                    fadeOut.Stop();
                    fadeOut.Dispose();
                    DismissInternal(reason);
                }
                else
                {
                    try { Opacity = opacity; } catch { }
                }
            };
            fadeOut.Start();
        }

        private void DismissInternal(string reason)
        {
            if (_disposed) return;

            StopTimers();
            Dismissed?.Invoke(reason);

            try
            {
                Close();
            }
            catch { }
        }

        private void StopTimers()
        {
            if (_autoCloseTimer != null)
            {
                _autoCloseTimer.Stop();
                _autoCloseTimer.Dispose();
                _autoCloseTimer = null;
            }
            if (_fadeInTimer != null)
            {
                _fadeInTimer.Stop();
                _fadeInTimer.Dispose();
                _fadeInTimer = null;
            }
        }

        #endregion

        #region Mouse Interaction

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_style.ShowCloseButton)
            {
                bool wasHovered = _closeButtonHovered;
                _closeButtonHovered = _closeButtonRect.Contains(e.Location);
                if (wasHovered != _closeButtonHovered)
                {
                    Cursor = _closeButtonHovered ? Cursors.Hand : Cursors.Default;
                    Invalidate(_closeButtonRect);
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (_style.ShowCloseButton && _closeButtonRect.Contains(e.Location))
            {
                DismissWithFade("UserDismissed");
                return;
            }

            // Click anywhere else fires Clicked event and dismisses
            Clicked?.Invoke();
            DismissWithFade("UserDismissed");
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_closeButtonHovered)
            {
                _closeButtonHovered = false;
                Cursor = Cursors.Default;
                Invalidate(_closeButtonRect);
            }
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                StopTimers();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}

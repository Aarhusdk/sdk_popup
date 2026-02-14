using System.Drawing;

namespace sdk_popup
{
    /// <summary>
    /// Registry of predefined notification styles and accent color mapping.
    /// Returns null for "Default" to signal: use DevExpress AlertControl path.
    /// </summary>
    internal static class NotificationStyleRegistry
    {
        /// <summary>
        /// Gets a style by name. Returns null for "Default" or unknown names.
        /// </summary>
        public static NotificationStyle GetStyle(string styleName)
        {
            switch ((styleName ?? "").Trim().ToLower())
            {
                case "dark": return CreateDarkStyle();
                case "light": return CreateLightStyle();
                case "rounded": return CreateRoundedStyle();
                case "minimal": return CreateMinimalStyle();
                default: return null;
            }
        }

        /// <summary>
        /// Maps notification type to accent color.
        /// </summary>
        public static Color GetAccentColor(string notificationType)
        {
            switch ((notificationType ?? "info").Trim().ToLower())
            {
                case "success": return Color.FromArgb(16, 124, 16);    // #107C10
                case "warning": return Color.FromArgb(255, 140, 0);    // #FF8C00
                case "error": return Color.FromArgb(232, 17, 35);      // #E81123
                case "info":
                default: return Color.FromArgb(0, 120, 212);           // #0078D4
            }
        }

        private static NotificationStyle CreateDarkStyle()
        {
            return new NotificationStyle
            {
                Name = "Dark",
                BackgroundColor = Color.FromArgb(45, 45, 48),          // #2D2D30
                TitleColor = Color.White,
                MessageColor = Color.FromArgb(204, 204, 204),          // #CCCCCC
                FontFamily = "Segoe UI",
                TitleFontSize = 12f,
                MessageFontSize = 10f,
                TitleFontStyle = FontStyle.Bold,
                MessageFontStyle = FontStyle.Regular,
                ShowAccentBar = true,
                AccentBarWidth = 4,
                CornerRadius = 8,
                BorderColor = Color.FromArgb(62, 62, 66),              // #3E3E42
                BorderWidth = 1,
                ShowIcon = true,
                ShowCloseButton = true,
                CloseButtonColor = Color.FromArgb(128, 128, 128),      // #808080
                CloseButtonHoverColor = Color.White,
                FormWidth = 340,
                PaddingLeft = 12,
                PaddingRight = 12,
                PaddingTop = 12,
                PaddingBottom = 12,
                ProgressBarBackColor = Color.FromArgb(62, 62, 66),
                ProgressBarFillColor = Color.Empty,                     // Uses accent color
                ProgressBarHeight = 4,
                UseDropShadow = true
            };
        }

        private static NotificationStyle CreateLightStyle()
        {
            return new NotificationStyle
            {
                Name = "Light",
                BackgroundColor = Color.White,
                TitleColor = Color.FromArgb(30, 30, 30),               // #1E1E1E
                MessageColor = Color.FromArgb(68, 68, 68),             // #444444
                FontFamily = "Segoe UI",
                TitleFontSize = 12f,
                MessageFontSize = 10f,
                TitleFontStyle = FontStyle.Bold,
                MessageFontStyle = FontStyle.Regular,
                ShowAccentBar = true,
                AccentBarWidth = 4,
                CornerRadius = 8,
                BorderColor = Color.FromArgb(224, 224, 224),           // #E0E0E0
                BorderWidth = 1,
                ShowIcon = true,
                ShowCloseButton = true,
                CloseButtonColor = Color.FromArgb(150, 150, 150),
                CloseButtonHoverColor = Color.FromArgb(50, 50, 50),
                FormWidth = 340,
                PaddingLeft = 12,
                PaddingRight = 12,
                PaddingTop = 12,
                PaddingBottom = 12,
                ProgressBarBackColor = Color.FromArgb(230, 230, 230),
                ProgressBarFillColor = Color.Empty,
                ProgressBarHeight = 4,
                UseDropShadow = true
            };
        }

        private static NotificationStyle CreateRoundedStyle()
        {
            return new NotificationStyle
            {
                Name = "Rounded",
                UseGradient = true,
                // BackgroundColor/BackgroundColorEnd set at runtime from accent color
                BackgroundColor = Color.Empty,
                BackgroundColorEnd = Color.Empty,
                TitleColor = Color.White,
                MessageColor = Color.FromArgb(240, 240, 240),
                FontFamily = "Segoe UI",
                TitleFontSize = 12f,
                MessageFontSize = 10f,
                TitleFontStyle = FontStyle.Bold,
                MessageFontStyle = FontStyle.Regular,
                ShowAccentBar = false,
                CornerRadius = 16,
                BorderWidth = 0,
                ShowIcon = true,
                ShowCloseButton = true,
                CloseButtonColor = Color.FromArgb(200, 255, 255, 255),
                CloseButtonHoverColor = Color.White,
                FormWidth = 340,
                PaddingLeft = 16,
                PaddingRight = 16,
                PaddingTop = 14,
                PaddingBottom = 14,
                ProgressBarBackColor = Color.FromArgb(80, 255, 255, 255),
                ProgressBarFillColor = Color.White,
                ProgressBarHeight = 4,
                UseDropShadow = true
            };
        }

        private static NotificationStyle CreateMinimalStyle()
        {
            return new NotificationStyle
            {
                Name = "Minimal",
                BackgroundColor = Color.White,
                TitleColor = Color.FromArgb(30, 30, 30),
                MessageColor = Color.FromArgb(102, 102, 102),          // #666666
                FontFamily = "Segoe UI",
                TitleFontSize = 11f,
                MessageFontSize = 9f,
                TitleFontStyle = FontStyle.Bold,
                MessageFontStyle = FontStyle.Regular,
                ShowAccentBar = false,
                ShowBottomBorder = true,
                BottomBorderHeight = 3,
                CornerRadius = 0,
                BorderColor = Color.FromArgb(230, 230, 230),
                BorderWidth = 1,
                ShowIcon = false,
                ShowCloseButton = false,
                FormWidth = 300,
                PaddingLeft = 12,
                PaddingRight = 12,
                PaddingTop = 10,
                PaddingBottom = 10,
                ProgressBarBackColor = Color.FromArgb(230, 230, 230),
                ProgressBarFillColor = Color.Empty,
                ProgressBarHeight = 3,
                UseDropShadow = true
            };
        }
    }
}

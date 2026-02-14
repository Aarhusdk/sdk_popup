using System.Drawing;

namespace sdk_popup
{
    /// <summary>
    /// Defines the visual properties for a notification style.
    /// Used by StyledNotificationForm for GDI+ rendering.
    /// </summary>
    internal class NotificationStyle
    {
        public string Name { get; set; }

        // Background
        public Color BackgroundColor { get; set; }
        public Color BackgroundColorEnd { get; set; }
        public bool UseGradient { get; set; }

        // Text colors
        public Color TitleColor { get; set; }
        public Color MessageColor { get; set; }

        // Fonts
        public string FontFamily { get; set; } = "Segoe UI";
        public float TitleFontSize { get; set; } = 12f;
        public float MessageFontSize { get; set; } = 10f;
        public FontStyle TitleFontStyle { get; set; } = FontStyle.Bold;
        public FontStyle MessageFontStyle { get; set; } = FontStyle.Regular;

        // Accent bar (left side colored strip)
        public bool ShowAccentBar { get; set; }
        public int AccentBarWidth { get; set; } = 4;

        // Bottom border (Minimal style)
        public bool ShowBottomBorder { get; set; }
        public int BottomBorderHeight { get; set; } = 3;

        // Border & shape
        public int CornerRadius { get; set; }
        public Color BorderColor { get; set; }
        public int BorderWidth { get; set; }

        // Icon
        public bool ShowIcon { get; set; } = true;

        // Close button
        public bool ShowCloseButton { get; set; } = true;
        public Color CloseButtonColor { get; set; }
        public Color CloseButtonHoverColor { get; set; }

        // Layout
        public int FormWidth { get; set; } = 340;
        public int PaddingLeft { get; set; } = 12;
        public int PaddingRight { get; set; } = 12;
        public int PaddingTop { get; set; } = 12;
        public int PaddingBottom { get; set; } = 12;

        // Progress bar
        public Color ProgressBarBackColor { get; set; }
        public Color ProgressBarFillColor { get; set; }
        public int ProgressBarHeight { get; set; } = 4;

        // Shadow
        public bool UseDropShadow { get; set; } = true;
    }
}

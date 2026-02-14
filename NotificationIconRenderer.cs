using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace sdk_popup
{
    /// <summary>
    /// Renders modern, vector-style notification icons using GDI+.
    /// Draws colored circles with white symbols (i, checkmark, !, X).
    /// </summary>
    internal static class NotificationIconRenderer
    {
        public static Image CreateIcon(string notificationType, int size = 32)
        {
            Color accentColor = NotificationStyleRegistry.GetAccentColor(notificationType);
            var bmp = new Bitmap(size, size);

            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Filled circle background
                using (var brush = new SolidBrush(accentColor))
                {
                    g.FillEllipse(brush, 1f, 1f, size - 2f, size - 2f);
                }

                float pw = Math.Max(1.5f, size / 12f);
                float cx = size / 2f;
                float cy = size / 2f;
                float s = size / 24f; // scale factor (reference design is 24px)

                string type = (notificationType ?? "info").Trim().ToLower();

                switch (type)
                {
                    case "success":
                        DrawCheckmark(g, cx, cy, s, pw);
                        break;
                    case "warning":
                        DrawExclamation(g, cx, size, pw);
                        break;
                    case "error":
                        DrawCross(g, cx, cy, size, pw);
                        break;
                    default: // "info"
                        DrawInfoSymbol(g, cx, size, pw);
                        break;
                }
            }

            return bmp;
        }

        private static void DrawCheckmark(Graphics g, float cx, float cy, float s, float pw)
        {
            using (var pen = new Pen(Color.White, pw))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                g.DrawLines(pen, new[]
                {
                    new PointF(cx - 5f * s, cy + 0.5f * s),
                    new PointF(cx - 1.5f * s, cy + 4f * s),
                    new PointF(cx + 5.5f * s, cy - 4f * s)
                });
            }
        }

        private static void DrawExclamation(Graphics g, float cx, int size, float pw)
        {
            // Vertical line
            using (var pen = new Pen(Color.White, pw))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx, size * 0.24f, cx, size * 0.56f);
            }

            // Dot below
            float dotRadius = pw * 0.7f;
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush,
                    cx - dotRadius, size * 0.68f - dotRadius,
                    dotRadius * 2, dotRadius * 2);
            }
        }

        private static void DrawCross(Graphics g, float cx, float cy, int size, float pw)
        {
            float arm = size * 0.18f;
            using (var pen = new Pen(Color.White, pw))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx - arm, cy - arm, cx + arm, cy + arm);
                g.DrawLine(pen, cx + arm, cy - arm, cx - arm, cy + arm);
            }
        }

        private static void DrawInfoSymbol(Graphics g, float cx, int size, float pw)
        {
            // Dot on top
            float dotRadius = pw * 0.7f;
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush,
                    cx - dotRadius, size * 0.25f - dotRadius,
                    dotRadius * 2, dotRadius * 2);
            }

            // Vertical line below
            using (var pen = new Pen(Color.White, pw))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx, size * 0.40f, cx, size * 0.76f);
            }
        }
    }
}

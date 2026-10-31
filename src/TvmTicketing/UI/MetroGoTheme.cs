using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TvmTicketing.UI
{
    public static class MetroGoTheme
    {
        public static readonly Color PageBg = Color.FromArgb(244, 247, 251);
        public static readonly Color CardBg = Color.White;
        public static readonly Color SelectedCardBg = Color.FromArgb(236, 244, 254);
        public static readonly Color BorderSubtle = Color.FromArgb(221, 229, 240);
        public static readonly Color PrimaryBlue = Color.FromArgb(37, 109, 181);
        public static readonly Color PrimaryBlueBright = Color.FromArgb(37, 99, 235);
        public static readonly Color TextDark = Color.FromArgb(30, 41, 59);
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
        public static readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);
        public static readonly Color SuccessBg = Color.FromArgb(232, 249, 241);
        public static readonly Color DangerRed = Color.FromArgb(225, 29, 72);

        public static Font FontRegular(float size, FontStyle style = FontStyle.Regular)
            => new Font("Segoe UI", size, style, GraphicsUnit.Point);

        public static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            int d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawRoundedBox(Graphics g, Rectangle rect, int radius, Color fill, Color border, float borderWidth = 1f)
        {
            if (rect.Width <= 1 || rect.Height <= 1) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedPath(rect, radius);
            using (var brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
            }
            if (borderWidth > 0 && border.A > 0)
            {
                using var pen = new Pen(border, borderWidth);
                g.DrawPath(pen, path);
            }
        }

        public static void DrawRadioCircle(Graphics g, Point center, bool selected)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var outer = new Rectangle(center.X - 9, center.Y - 9, 18, 18);
            if (selected)
            {
                using var pen = new Pen(PrimaryBlueBright, 2f);
                g.DrawEllipse(pen, outer);
                using var brush = new SolidBrush(PrimaryBlueBright);
                g.FillEllipse(brush, new Rectangle(center.X - 5, center.Y - 5, 10, 10));
            }
            else
            {
                using var pen = new Pen(Color.FromArgb(185, 198, 216), 1.5f);
                g.DrawEllipse(pen, outer);
            }
        }

        public static void DrawQrMatrix(Graphics g, Rectangle bounds, string seed)
        {
            g.SmoothingMode = SmoothingMode.None;
            int modules = 25;
            int cellSize = Math.Max(2, Math.Min(bounds.Width, bounds.Height) / modules);
            int totalSize = cellSize * modules;
            int startX = bounds.X + (bounds.Width - totalSize) / 2;
            int startY = bounds.Y + (bounds.Height - totalSize) / 2;

            using var darkBrush = new SolidBrush(Color.FromArgb(24, 38, 61));
            using var lightBrush = new SolidBrush(Color.White);
            g.FillRectangle(lightBrush, bounds);

            int hash = seed.GetHashCode();

            void DrawFinder(int gx, int gy)
            {
                for (int r = 0; r < 7; r++)
                {
                    for (int c = 0; c < 7; c++)
                    {
                        bool isBorder = r == 0 || r == 6 || c == 0 || c == 6;
                        bool isCenter = r >= 2 && r <= 4 && c >= 2 && c <= 4;
                        if (isBorder || isCenter)
                        {
                            g.FillRectangle(darkBrush, startX + (gx + c) * cellSize, startY + (gy + r) * cellSize, cellSize, cellSize);
                        }
                    }
                }
            }

            DrawFinder(0, 0);
            DrawFinder(modules - 7, 0);
            DrawFinder(0, modules - 7);

            for (int r = 0; r < modules; r++)
            {
                for (int c = 0; c < modules; c++)
                {
                    if ((r < 8 && c < 8) || (r < 8 && c >= modules - 8) || (r >= modules - 8 && c < 8))
                        continue;

                    int v = (r * 31 + c * 17 + hash) ^ (r * c * 13);
                    if ((Math.Abs(v) % 10) < 5)
                    {
                        g.FillRectangle(darkBrush, startX + c * cellSize, startY + r * cellSize, cellSize, cellSize);
                    }
                }
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
        }

        public static void DrawCode128Barcode(Graphics g, Rectangle bounds, string code)
        {
            g.SmoothingMode = SmoothingMode.None;
            using var brush = new SolidBrush(Color.FromArgb(28, 37, 54));
            int x = bounds.X;
            int maxRight = bounds.Right;
            int idx = 0;
            string safeCode = string.IsNullOrEmpty(code) ? "CODE128-METROGO" : code;
            while (x < maxRight - 3)
            {
                char ch = safeCode[idx % safeCode.Length];
                int barW = ((ch + idx) % 3) + 1;
                int gapW = ((ch * 3 + idx) % 2) + 1;
                if (x + barW <= maxRight)
                    g.FillRectangle(brush, x, bounds.Y, barW, bounds.Height);
                x += barW + gapW;
                idx++;
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
        }
    }

    public class RoundedCardPanel : Panel
    {
        public int CornerRadius { get; set; } = 10;
        public Color FillColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = MetroGoTheme.BorderSubtle;
        public float BorderThickness { get; set; } = 1f;

        public RoundedCardPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            MetroGoTheme.DrawRoundedBox(e.Graphics, rect, CornerRadius, FillColor, BorderColor, BorderThickness);
            base.OnPaint(e);
        }
    }

    public class MetroButton : Control
    {
        public Color FillColor { get; set; } = MetroGoTheme.PrimaryBlue;
        public Color HoverFillColor { get; set; } = Color.FromArgb(29, 92, 156);
        public Color BorderColor { get; set; } = Color.Transparent;
        public Color TextColor { get; set; } = Color.White;
        public int CornerRadius { get; set; } = 7;
        private bool _hover;

        public MetroButton()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Font = MetroGoTheme.FontRegular(9.5f, FontStyle.Bold);
            Size = new Size(160, 40);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (Parent != null)
            {
                using var bg = new SolidBrush(Parent.BackColor == Color.Transparent ? Color.White : Parent.BackColor);
                e.Graphics.FillRectangle(bg, ClientRectangle);
            }
            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            Color fill = !Enabled ? Color.FromArgb(203, 213, 225) : (_hover ? HoverFillColor : FillColor);
            Color fg = !Enabled ? Color.FromArgb(100, 116, 139) : TextColor;
            MetroGoTheme.DrawRoundedBox(e.Graphics, rect, CornerRadius, fill, BorderColor, BorderColor.A > 0 ? 1.2f : 0f);
            TextRenderer.DrawText(e.Graphics, Text, Font, rect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}

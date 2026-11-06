/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BrightIdeasSoftware;
using Klocman.Forms.Theming;

namespace BulkCrapUninstaller.Functions.Tools
{
    /// <summary>
    ///     Theme support for ObjectListView, plugged into <see cref="ThemeManager.CustomStylers" />.
    ///     Only visual properties are changed.
    /// </summary>
    internal static class ObjectListViewTheme
    {
        public static void Register()
        {
            if (!ThemeManager.IsActive) return;
            ThemeManager.CustomStylers.Add(Style);
            BaseRenderer.CheckBoxPainter = PaintCheckBox;
            BaseRenderer.ForegroundColorFilter = color => ThemeManager.MapFore(color, P.ListBackground);
        }

        private static ThemePalette P => ThemeManager.Palette;

        private static bool Style(Control c)
        {
            if (c is not ObjectListView olv) return false;

            olv.BackColor = P.ListBackground;
            olv.ForeColor = P.Text;
            if (olv.BorderStyle == BorderStyle.Fixed3D) olv.BorderStyle = BorderStyle.FixedSingle;

            olv.GridLines = false;
            olv.UseAlternatingBackColors = false;

            olv.SelectedBackColor = P.Selection;
            olv.SelectedForeColor = P.SelectionText;
            olv.UnfocusedSelectedBackColor = P.SelectionUnfocused;
            olv.UnfocusedSelectedForeColor = P.SelectionText;

            olv.UseHotItem = true;
            olv.HotItemStyle = new HotItemStyle { BackColor = P.RowHover };

            olv.HeaderUsesThemes = false;
            var headerStyle = new HeaderFormatStyle();
            headerStyle.Normal.BackColor = P.Background;
            headerStyle.Normal.ForeColor = P.TextMuted;
            headerStyle.Hot.BackColor = P.Hover;
            headerStyle.Hot.ForeColor = P.Text;
            headerStyle.Pressed.BackColor = P.Pressed;
            headerStyle.Pressed.ForeColor = P.Text;
            olv.HeaderFormatStyle = headerStyle;
            AssignHeaderDrawing(olv);

            if (olv.EmptyListMsgOverlay is TextOverlay overlay)
            {
                overlay.TextColor = P.TextMuted;
                overlay.BackColor = P.Surface;
                overlay.BorderColor = P.Border;
                overlay.BorderWidth = 1f;
            }

            ThemeManager.OnHandle(olv, x =>
            {
                AssignHeaderDrawing(olv);
                ThemeManager.ApplyListViewNativeTheme(x.Handle);
            });

            return true;
        }

        private static void AssignHeaderDrawing(ObjectListView olv)
        {
            foreach (var column in olv.AllColumns)
                column.HeaderDrawing = DrawHeader;
        }

        private static bool DrawHeader(Graphics g, Rectangle r, int columnIndex, OLVColumn column, bool isPressed,
            HeaderStateStyle stateStyle)
        {
            // Let OLV draw header check boxes itself
            if (column.HeaderCheckBox) return true;

            var olv = column.ListView as ObjectListView;
            var isHot = olv?.HeaderFormatStyle != null && ReferenceEquals(stateStyle, olv.HeaderFormatStyle.Hot);
            var dpi = olv?.DeviceDpi ?? 96;
            float S(float px) => px * dpi / 96f;

            var back = isPressed ? P.Pressed : isHot ? P.Hover : P.Background;
            using (var b = new SolidBrush(back))
                g.FillRectangle(b, r);

            using (var pen = new Pen(P.Border))
            {
                g.DrawLine(pen, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
                var pad = (int)S(8);
                g.DrawLine(pen, r.Right - 1, r.Top + pad, r.Right - 1, r.Bottom - pad);
            }

            var textRect = Rectangle.FromLTRB(r.Left + (int)S(columnIndex == 0 ? 8 : 6), r.Top, r.Right - (int)S(6), r.Bottom);

            // Sort chevron
            if (olv != null && olv.ShowSortIndicators && olv.PrimarySortColumn == column &&
                olv.PrimarySortOrder != SortOrder.None)
            {
                var size = S(8);
                var cx = textRect.Right - size / 2f - S(2);
                var cy = r.Top + r.Height / 2f;
                var up = olv.PrimarySortOrder == SortOrder.Ascending;
                using var pen = new Pen(isHot ? P.Text : P.TextMuted, Math.Max(1.2f, S(1.5f)))
                {
                    StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
                };
                var old = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var dy = size / 4f * (up ? -1 : 1);
                g.DrawLines(pen, new[]
                {
                    new PointF(cx - size / 2f, cy - dy),
                    new PointF(cx, cy + dy),
                    new PointF(cx + size / 2f, cy - dy)
                });
                g.SmoothingMode = old;
                textRect.Width -= (int)(size + S(6));
            }

            // Filter indicator: small accent dot
            if (column.HasFilterIndicator)
            {
                var d = S(6);
                using var brush = new SolidBrush(P.Accent);
                var old = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(brush, textRect.Right - d, r.Top + (r.Height - d) / 2f, d, d);
                g.SmoothingMode = old;
                textRect.Width -= (int)(d + S(4));
            }

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine |
                        TextFormatFlags.NoPrefix;
            switch (column.HeaderTextAlignOrDefault)
            {
                case HorizontalAlignment.Center:
                    flags |= TextFormatFlags.HorizontalCenter;
                    break;
                case HorizontalAlignment.Right:
                    flags |= TextFormatFlags.Right;
                    break;
            }
            if (olv?.RightToLeftLayout == true) flags |= TextFormatFlags.RightToLeft;

            var font = stateStyle?.Font ?? column.HeaderFont ?? olv?.Font ?? Control.DefaultFont;
            var color = isHot || isPressed ? P.Text : P.TextMuted;
            TextRenderer.DrawText(g, column.Text, font, textRect, color, flags);
            return false;
        }

        /// <summary>
        ///     Row check boxes in the theme style (the default ones are bright white in dark mode).
        /// </summary>
        private static bool PaintCheckBox(Graphics g, Rectangle box, CheckState state, bool enabled)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(box.X + 0.5f, box.Y + 0.5f, box.Width - 1, box.Height - 1);
            var radius = Math.Max(2f, box.Width * 3f / 16f);
            var accent = enabled ? P.Accent : ThemeManager.Blend(P.Accent, P.ListBackground, 0.5);
            if (state == CheckState.Unchecked)
            {
                ThemeManager.FillRounded(g, P.Surface, r, radius);
                ThemeManager.DrawRounded(g, enabled ? P.BorderStrong : P.Border, r, radius);
            }
            else
            {
                ThemeManager.FillRounded(g, accent, r, radius);
                using var pen = new Pen(P.AccentForeground, Math.Max(1.5f, box.Width / 7f))
                {
                    StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
                };
                if (state == CheckState.Checked)
                    g.DrawLines(pen, new[]
                    {
                        new PointF(r.X + r.Width * 0.24f, r.Y + r.Height * 0.52f),
                        new PointF(r.X + r.Width * 0.42f, r.Y + r.Height * 0.70f),
                        new PointF(r.X + r.Width * 0.76f, r.Y + r.Height * 0.32f)
                    });
                else
                    g.DrawLine(pen, r.X + r.Width * 0.28f, r.Y + r.Height / 2f, r.Right - r.Width * 0.28f, r.Y + r.Height / 2f);
            }
            g.SmoothingMode = old;
            return true;
        }
    }
}

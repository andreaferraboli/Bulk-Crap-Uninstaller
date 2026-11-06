/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Klocman.Forms.Theming
{
    /// <summary>
    ///     Flat colors for <see cref="ThemedToolStripRenderer" />.
    /// </summary>
    public sealed class ThemedColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette _p;

        public ThemedColorTable(ThemePalette palette)
        {
            _p = palette;
            UseSystemColors = false;
        }

        public override Color MenuStripGradientBegin => _p.Background;
        public override Color MenuStripGradientEnd => _p.Background;
        public override Color ToolStripGradientBegin => _p.Background;
        public override Color ToolStripGradientMiddle => _p.Background;
        public override Color ToolStripGradientEnd => _p.Background;
        public override Color ToolStripContentPanelGradientBegin => _p.Background;
        public override Color ToolStripContentPanelGradientEnd => _p.Background;
        public override Color ToolStripPanelGradientBegin => _p.Background;
        public override Color ToolStripPanelGradientEnd => _p.Background;
        public override Color StatusStripGradientBegin => _p.Background;
        public override Color StatusStripGradientEnd => _p.Background;

        public override Color ToolStripDropDownBackground => _p.SurfaceElevated;
        public override Color ImageMarginGradientBegin => _p.SurfaceElevated;
        public override Color ImageMarginGradientMiddle => _p.SurfaceElevated;
        public override Color ImageMarginGradientEnd => _p.SurfaceElevated;
        public override Color ImageMarginRevealedGradientBegin => _p.SurfaceElevated;
        public override Color ImageMarginRevealedGradientMiddle => _p.SurfaceElevated;
        public override Color ImageMarginRevealedGradientEnd => _p.SurfaceElevated;

        public override Color MenuBorder => _p.Border;
        public override Color ToolStripBorder => _p.Border;

        public override Color MenuItemSelected => _p.Hover;
        public override Color MenuItemSelectedGradientBegin => _p.Hover;
        public override Color MenuItemSelectedGradientEnd => _p.Hover;
        public override Color ButtonSelectedHighlight => _p.Hover;
        public override Color ButtonSelectedGradientBegin => _p.Hover;
        public override Color ButtonSelectedGradientMiddle => _p.Hover;
        public override Color ButtonSelectedGradientEnd => _p.Hover;
        public override Color MenuItemBorder => _p.Hover;
        public override Color ButtonSelectedBorder => _p.Hover;
        public override Color ButtonSelectedHighlightBorder => _p.Hover;

        public override Color MenuItemPressedGradientBegin => _p.Pressed;
        public override Color MenuItemPressedGradientMiddle => _p.Pressed;
        public override Color MenuItemPressedGradientEnd => _p.Pressed;
        public override Color ButtonPressedGradientBegin => _p.Pressed;
        public override Color ButtonPressedGradientMiddle => _p.Pressed;
        public override Color ButtonPressedGradientEnd => _p.Pressed;
        public override Color ButtonPressedHighlight => _p.Pressed;
        public override Color ButtonPressedBorder => _p.Pressed;
        public override Color ButtonPressedHighlightBorder => _p.Pressed;

        public override Color ButtonCheckedGradientBegin => _p.AccentTint;
        public override Color ButtonCheckedGradientMiddle => _p.AccentTint;
        public override Color ButtonCheckedGradientEnd => _p.AccentTint;
        public override Color ButtonCheckedHighlight => _p.AccentTint;
        public override Color ButtonCheckedHighlightBorder => _p.AccentTint;
        public override Color CheckBackground => _p.AccentTint;
        public override Color CheckSelectedBackground => _p.AccentTint;
        public override Color CheckPressedBackground => _p.AccentTint;

        public override Color SeparatorDark => _p.Border;
        public override Color SeparatorLight => _p.Background;

        public override Color GripDark => _p.BorderStrong;
        public override Color GripLight => _p.Background;

        public override Color OverflowButtonGradientBegin => _p.Background;
        public override Color OverflowButtonGradientMiddle => _p.Background;
        public override Color OverflowButtonGradientEnd => _p.Background;

        public override Color RaftingContainerGradientBegin => _p.Background;
        public override Color RaftingContainerGradientEnd => _p.Background;
    }

    /// <summary>
    ///     Flat, rounded renderer for MenuStrip, ToolStrip, StatusStrip and context menus.
    /// </summary>
    public sealed class ThemedToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemePalette _p;
        private readonly ConditionalWeakTable<ToolStrip, StrongBox<IntPtr>> _decorated = new();

        public ThemedToolStripRenderer(ThemePalette palette) : base(new ThemedColorTable(palette))
        {
            _p = palette;
            RoundedEdges = false;
        }

        private static float Scale(ToolStripItem item, float px) => px * (item.Owner?.DeviceDpi ?? 96) / 96f;
        private static float Scale(ToolStrip ts, float px) => px * ts.DeviceDpi / 96f;

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            var ts = e.ToolStrip;
            if (ts is ToolStripDropDown dd)
            {
                DecorateDropDown(dd);
                using var b = new SolidBrush(_p.SurfaceElevated);
                e.Graphics.FillRectangle(b, e.AffectedBounds);
                return;
            }

            using (var brush = new SolidBrush(_p.Background))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        private void DecorateDropDown(ToolStripDropDown dd)
        {
            if (!dd.IsHandleCreated) return;
            var box = _decorated.GetValue(dd, _ => new StrongBox<IntPtr>(IntPtr.Zero));
            if (box.Value == dd.Handle) return;
            box.Value = dd.Handle;
            ThemeNative.AllowDarkModeForWindow(dd.Handle, _p.IsDark);
            ThemeNative.SetCornerPreference(dd.Handle, ThemeNative.DWMWCP_ROUNDSMALL);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            var ts = e.ToolStrip;
            using var pen = new Pen(_p.Border);
            switch (ts)
            {
                case ToolStripDropDown:
                    e.Graphics.DrawRectangle(pen, 0, 0, ts.Width - 1, ts.Height - 1);
                    break;
                case StatusStrip:
                    e.Graphics.DrawLine(pen, 0, 0, ts.Width, 0);
                    break;
                case MenuStrip:
                    break;
                default:
                    // Plain tool bars get a hairline under them
                    e.Graphics.DrawLine(pen, 0, ts.Height - 1, ts.Width, ts.Height - 1);
                    break;
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using var b = new SolidBrush(_p.SurfaceElevated);
            e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            var active = item.Enabled && (item.Selected || item.Pressed);
            if (!active)
            {
                // Top level items of a non-focused menu strip still draw "checked" if needed
                return;
            }

            var topLevel = item.IsOnDropDown == false;
            var color = topLevel && item.Pressed ? _p.Pressed : _p.Hover;
            var inset = topLevel ? 0f : Scale(item, 4);
            var r = new RectangleF(inset, topLevel ? Scale(item, 2) : 0.5f, item.Width - inset * 2,
                item.Height - (topLevel ? Scale(item, 4) : 1f));
            ThemeManager.FillRounded(e.Graphics, color, r, Scale(item, topLevel ? 4 : 3));
        }

        private void RenderButtonLike(ToolStripItemRenderEventArgs e, bool isChecked)
        {
            var item = e.Item;
            Color? color = null;
            if (item.Pressed) color = _p.Pressed;
            else if (item.Selected && item.Enabled) color = isChecked ? _p.Pressed : _p.Hover;
            else if (isChecked) color = _p.AccentTint;
            if (color == null) return;

            var r = new RectangleF(1, 1, item.Width - 2, item.Height - 2);
            ThemeManager.FillRounded(e.Graphics, color.Value, r, Scale(item, 4));
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            RenderButtonLike(e, e.Item is ToolStripButton { Checked: true });
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            RenderButtonLike(e, false);
        }

        protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
        {
            RenderButtonLike(e, false);
            if (e.Item is ToolStripSplitButton sb)
            {
                var arrowColor = sb.Enabled ? _p.TextMuted : _p.TextDisabled;
                DrawArrow(new ToolStripArrowRenderEventArgs(e.Graphics, sb, sb.DropDownButtonBounds, arrowColor, ArrowDirection.Down));
            }
        }

        protected override void OnRenderOverflowButtonBackground(ToolStripItemRenderEventArgs e)
        {
            RenderButtonLike(e, false);
            var item = e.Item;
            var arrowRect = new Rectangle(0, 0, item.Width, item.Height);
            DrawArrow(new ToolStripArrowRenderEventArgs(e.Graphics, item, arrowRect, _p.TextMuted,
                item.Owner?.Orientation == Orientation.Vertical ? ArrowDirection.Right : ArrowDirection.Down));
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var item = e.Item;
            var isShortcut = item is ToolStripMenuItem && !string.Equals(e.Text, item.Text, StringComparison.Ordinal);
            if (!item.Enabled)
                e.TextColor = _p.TextDisabled;
            else if (isShortcut)
                e.TextColor = _p.TextMuted;
            else if (item.Owner is StatusStrip)
                e.TextColor = _p.TextMuted;
            else if (item is ToolStripButton { Checked: true } && !item.Selected)
                e.TextColor = _p.AccentText;
            else
                e.TextColor = _p.Text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item?.Enabled == false ? _p.TextDisabled : _p.TextMuted;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var g = e.Graphics;
            var r = e.ImageRectangle;
            var item = e.Item;
            var hasImage = item.Image != null && item.DisplayStyle != ToolStripItemDisplayStyle.Text;

            var bg = Rectangle.Inflate(r, (int)Scale(item, 2), (int)Scale(item, 2));
            ThemeManager.FillRounded(g, _p.AccentTint, bg, Scale(item, 3));
            if (hasImage) return;

            var color = item.Enabled ? _p.AccentText : _p.TextDisabled;
            using var pen = new Pen(color, Math.Max(1.5f, Scale(item, 2)))
            {
                StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
            };
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var cx = r.X + r.Width / 2f;
            var cy = r.Y + r.Height / 2f;
            var s = Math.Min(r.Width, r.Height) / 2f * 0.75f;
            g.DrawLines(pen, new[]
            {
                new PointF(cx - s, cy + s * 0.05f),
                new PointF(cx - s * 0.3f, cy + s * 0.7f),
                new PointF(cx + s, cy - s * 0.6f)
            });
            g.SmoothingMode = old;
        }

        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            var image = e.Image ?? e.Item?.Image;
            if (image == null || e.Item == null)
            {
                base.OnRenderItemImage(e);
                return;
            }

            image = ThemeManager.GetThemedImage(image);
            var r = e.ImageRectangle;
            if (e.Item.Enabled)
            {
                e.Graphics.DrawImage(image, r);
            }
            else
            {
                using var attributes = new ImageAttributes();
                var matrix = new ColorMatrix { Matrix33 = 0.35f };
                // Desaturate disabled images
                matrix.Matrix00 = matrix.Matrix01 = matrix.Matrix02 = 0.33f;
                matrix.Matrix10 = matrix.Matrix11 = matrix.Matrix12 = 0.33f;
                matrix.Matrix20 = matrix.Matrix21 = matrix.Matrix22 = 0.33f;
                attributes.SetColorMatrix(matrix);
                e.Graphics.DrawImage(image, r, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var item = e.Item;
            using var pen = new Pen(_p.Border);
            if (e.Vertical)
            {
                var h = Math.Min(item.Height, Scale(item, 20));
                var x = item.Width / 2;
                var y = (item.Height - h) / 2f;
                e.Graphics.DrawLine(pen, x, y, x, y + h);
            }
            else
            {
                var inset = item.IsOnDropDown ? Scale(item, 4) : 0;
                var y = item.Height / 2;
                e.Graphics.DrawLine(pen, inset, y, item.Width - inset, y);
            }
        }

        protected override void OnRenderToolStripStatusLabelBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is not ToolStripStatusLabel label || label.BorderSides == ToolStripStatusLabelBorderSides.None)
                return;

            using var pen = new Pen(_p.Border);
            var r = new Rectangle(0, 0, label.Width - 1, label.Height - 1);
            var pad = (int)Scale(label, 4);
            if ((label.BorderSides & ToolStripStatusLabelBorderSides.Left) != 0)
                e.Graphics.DrawLine(pen, r.Left, r.Top + pad, r.Left, r.Bottom - pad);
            if ((label.BorderSides & ToolStripStatusLabelBorderSides.Right) != 0)
                e.Graphics.DrawLine(pen, r.Right, r.Top + pad, r.Right, r.Bottom - pad);
            if ((label.BorderSides & ToolStripStatusLabelBorderSides.Top) != 0)
                e.Graphics.DrawLine(pen, r.Left, r.Top, r.Right, r.Top);
            if ((label.BorderSides & ToolStripStatusLabelBorderSides.Bottom) != 0)
                e.Graphics.DrawLine(pen, r.Left, r.Bottom, r.Right, r.Bottom);
        }

        protected override void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e)
        {
            // Flat look, no grip
        }

        protected override void OnRenderLabelBackground(ToolStripItemRenderEventArgs e)
        {
            // Transparent
        }

        protected override void OnRenderGrip(ToolStripGripRenderEventArgs e)
        {
            if (e.GripStyle == ToolStripGripStyle.Hidden) return;
            base.OnRenderGrip(e);
        }
    }
}

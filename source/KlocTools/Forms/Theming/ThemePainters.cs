/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Klocman.Forms.Theming
{
    /// <summary>
    ///     Paint hooks for standard controls that can't be recolored through properties alone.
    ///     The hooks only draw; they never change state or handle input.
    /// </summary>
    internal static class ThemePainters
    {
        private static ThemePalette P => ThemeManager.Palette;

        private static readonly MethodInfo SetStyleMethod =
            typeof(Control).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void SetUserPaint(Control c)
        {
            SetStyleMethod?.Invoke(c, new object[]
            {
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true
            });
        }

        #region CheckBox / RadioButton

        public static void HookCheckable(ButtonBase b)
        {
            b.Paint += OnCheckablePaint;
            b.MouseEnter += (s, e) => ((Control)s).Invalidate();
            b.MouseLeave += (s, e) => ((Control)s).Invalidate();
        }

        private static void OnCheckablePaint(object sender, PaintEventArgs e)
        {
            var b = (ButtonBase)sender;
            ContentAlignment align;
            CheckState state;
            var isRadio = b is RadioButton;
            if (b is CheckBox cb)
            {
                align = cb.CheckAlign;
                state = cb.CheckState;
            }
            else
            {
                var rb = (RadioButton)b;
                align = rb.CheckAlign;
                state = rb.Checked ? CheckState.Checked : CheckState.Unchecked;
            }

            var rtl = b.RightToLeft == RightToLeft.Yes;
            var left = align is ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft;
            var right = align is ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight;
            if (!left && !right) return;
            if (rtl) (left, right) = (right, left);

            var size = (int)Math.Round(13 * b.DeviceDpi / 96f);
            var client = b.ClientRectangle;
            var pad = b.Padding;
            int y;
            switch (align)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopRight:
                    y = pad.Top + Math.Max(0, (b.Font.Height - size) / 2);
                    break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomRight:
                    y = client.Bottom - pad.Bottom - size;
                    break;
                default:
                    y = pad.Top + (client.Height - pad.Vertical - size) / 2;
                    break;
            }
            var x = left ? pad.Left : client.Right - pad.Right - size;

            var g = e.Graphics;
            var box = new Rectangle(x, y, size, size);

            // Erase the native glyph
            var back = b.BackColor.A == 255 ? b.BackColor : GetBack(b);
            using (var eraser = new SolidBrush(back))
                g.FillRectangle(eraser, Rectangle.Inflate(box, 1, 1));

            var enabled = b.Enabled;
            var hot = enabled && b.ClientRectangle.Contains(b.PointToClient(Control.MousePosition));
            var checkedOrMixed = state != CheckState.Unchecked;
            var accent = enabled ? P.Accent : ThemeManager.Blend(P.Accent, back, 0.5);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            var boxF = new RectangleF(box.X + 0.5f, box.Y + 0.5f, box.Width - 1, box.Height - 1);
            if (isRadio)
            {
                using (var pen = new Pen(checkedOrMixed ? accent : hot ? P.TextMuted : P.BorderStrong))
                {
                    using var fill = new SolidBrush(P.IsDark ? P.Surface : P.Surface);
                    g.FillEllipse(fill, boxF);
                    g.DrawEllipse(pen, boxF);
                }

                if (checkedOrMixed)
                {
                    var dot = size * 0.46f;
                    using var brush = new SolidBrush(accent);
                    g.FillEllipse(brush, box.X + (size - dot) / 2f, box.Y + (size - dot) / 2f, dot, dot);
                }
            }
            else
            {
                var radius = 3 * b.DeviceDpi / 96f;
                if (checkedOrMixed)
                {
                    ThemeManager.FillRounded(g, accent, boxF, radius);
                    using var pen = new Pen(P.AccentForeground, Math.Max(1.5f, 2f * b.DeviceDpi / 96f))
                    {
                        StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
                    };
                    if (state == CheckState.Checked)
                    {
                        g.DrawLines(pen, new[]
                        {
                            new PointF(boxF.X + boxF.Width * 0.24f, boxF.Y + boxF.Height * 0.52f),
                            new PointF(boxF.X + boxF.Width * 0.42f, boxF.Y + boxF.Height * 0.70f),
                            new PointF(boxF.X + boxF.Width * 0.76f, boxF.Y + boxF.Height * 0.32f)
                        });
                    }
                    else
                    {
                        g.DrawLine(pen, boxF.X + boxF.Width * 0.28f, boxF.Y + boxF.Height / 2f,
                            boxF.Right - boxF.Width * 0.28f, boxF.Y + boxF.Height / 2f);
                    }
                }
                else
                {
                    ThemeManager.FillRounded(g, P.Surface, boxF, radius);
                    ThemeManager.DrawRounded(g, !enabled ? P.Border : hot ? P.TextMuted : P.BorderStrong, boxF, radius);
                }
            }
            g.SmoothingMode = SmoothingMode.Default;
        }

        private static Color GetBack(Control c)
        {
            for (var x = c; x != null; x = x.Parent)
                if (x.BackColor.A == 255) return x.BackColor;
            return P.Background;
        }

        #endregion

        #region Button

        /// <summary>
        ///     Flat buttons draw disabled text almost invisible on dark backgrounds; repaint it in the disabled text color.
        /// </summary>
        public static void HookDisabledButton(ButtonBase b)
        {
            b.Paint += (sender, e) =>
            {
                if (b.Enabled || b.Image != null || string.IsNullOrEmpty(b.Text)) return;
                var g = e.Graphics;
                var r = b.ClientRectangle;
                using (var brush = new SolidBrush(b.BackColor))
                    g.FillRectangle(brush, r);
                using (var pen = new Pen(P.Border))
                    g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);

                var flags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis;
                flags |= b.TextAlign switch
                {
                    ContentAlignment.TopLeft => TextFormatFlags.Top | TextFormatFlags.Left,
                    ContentAlignment.TopCenter => TextFormatFlags.Top | TextFormatFlags.HorizontalCenter,
                    ContentAlignment.TopRight => TextFormatFlags.Top | TextFormatFlags.Right,
                    ContentAlignment.MiddleLeft => TextFormatFlags.VerticalCenter | TextFormatFlags.Left,
                    ContentAlignment.MiddleRight => TextFormatFlags.VerticalCenter | TextFormatFlags.Right,
                    ContentAlignment.BottomLeft => TextFormatFlags.Bottom | TextFormatFlags.Left,
                    ContentAlignment.BottomCenter => TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter,
                    ContentAlignment.BottomRight => TextFormatFlags.Bottom | TextFormatFlags.Right,
                    _ => TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter
                };
                if (!b.UseMnemonic) flags |= TextFormatFlags.NoPrefix;
                else flags |= TextFormatFlags.HidePrefix;
                if (b.RightToLeft == RightToLeft.Yes) flags |= TextFormatFlags.RightToLeft;
                var textRect = Rectangle.Inflate(r, -Math.Max(2, b.Padding.Horizontal / 2 + 2), -2);
                TextRenderer.DrawText(g, b.Text, b.Font, textRect, P.TextDisabled, flags);
            };
        }

        #endregion

        #region GroupBox

        public static void HookGroupBox(GroupBox gb)
        {
            gb.Paint += OnGroupBoxPaint;
        }

        private static void OnGroupBoxPaint(object sender, PaintEventArgs e)
        {
            var gb = (GroupBox)sender;
            var g = e.Graphics;
            var back = GetBack(gb);
            using (var brush = new SolidBrush(back))
                g.FillRectangle(brush, gb.ClientRectangle);

            var textSize = string.IsNullOrEmpty(gb.Text)
                ? Size.Empty
                : TextRenderer.MeasureText(g, gb.Text, gb.Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            var top = gb.Font.Height / 2;
            var frame = new RectangleF(0.5f, top + 0.5f, gb.Width - 1f, gb.Height - top - 1f);
            ThemeManager.DrawRounded(g, P.Border, frame, 6 * gb.DeviceDpi / 96f);

            if (textSize.IsEmpty) return;

            var rtl = gb.RightToLeft == RightToLeft.Yes;
            var textX = rtl ? gb.Width - 8 - textSize.Width : 8;
            var textRect = new Rectangle(textX - 3, 0, textSize.Width + 6, gb.Font.Height);
            using (var brush = new SolidBrush(back))
                g.FillRectangle(brush, textRect);

            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.HorizontalCenter;
            if (rtl) flags |= TextFormatFlags.RightToLeft;
            TextRenderer.DrawText(g, gb.Text, gb.Font, textRect, gb.Enabled ? P.TextMuted : P.TextDisabled, flags);
        }

        #endregion

        #region TabControl

        public static void HookTabControl(TabControl tc)
        {
            // A header-less tab control (wizards) only needs page colors
            SetUserPaint(tc);
            tc.Paint += OnTabControlPaint;
            tc.SelectedIndexChanged += (s, e) => ((Control)s).Invalidate();
            tc.MouseMove += OnTabMouseMove;
            tc.MouseLeave += (s, e) =>
            {
                tc.SetHotTab(-1);
                tc.Invalidate();
            };
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TabControl, System.Runtime.CompilerServices.StrongBox<int>> HotTabs = new();

        private static void SetHotTab(this TabControl tc, int hot)
        {
            HotTabs.GetValue(tc, _ => new System.Runtime.CompilerServices.StrongBox<int>(-1)).Value = hot;
        }

        private static int HotTab(this TabControl tc) => HotTabs.TryGetValue(tc, out var box) ? box.Value : -1;

        private static void OnTabMouseMove(object sender, MouseEventArgs e)
        {
            var tc = (TabControl)sender;
            var hot = -1;
            for (var i = 0; i < tc.TabCount; i++)
            {
                if (tc.GetTabRect(i).Contains(e.Location))
                {
                    hot = i;
                    break;
                }
            }

            if (hot == tc.HotTab()) return;
            tc.SetHotTab(hot);
            tc.Invalidate();
        }

        private static void OnTabControlPaint(object sender, PaintEventArgs e)
        {
            var tc = (TabControl)sender;
            var g = e.Graphics;
            var back = GetBack(tc.Parent ?? tc);
            using (var brush = new SolidBrush(back))
                g.FillRectangle(brush, tc.ClientRectangle);

            var display = tc.DisplayRectangle;
            if (tc.TabCount == 0 || display.Top <= 4 || tc.Alignment != TabAlignment.Top)
                return;

            // Tab strip
            var stripBottom = tc.GetTabRect(0).Bottom;
            for (var i = 0; i < tc.TabCount; i++)
                stripBottom = Math.Max(stripBottom, tc.GetTabRect(i).Bottom);

            using (var pen = new Pen(P.Border))
            {
                g.DrawLine(pen, 0, stripBottom, tc.Width, stripBottom);
                // Frame around the page area
                var page = Rectangle.FromLTRB(0, stripBottom, tc.Width - 1, tc.Height - 1);
                g.DrawRectangle(pen, page);
            }

            var underline = Math.Max(2, (int)Math.Round(2 * tc.DeviceDpi / 96f));
            for (var i = 0; i < tc.TabCount; i++)
            {
                var r = tc.GetTabRect(i);
                var selected = i == tc.SelectedIndex;
                var hot = i == tc.HotTab();
                if (hot && !selected)
                    ThemeManager.FillRounded(g, P.Hover, new RectangleF(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4 - underline), 4 * tc.DeviceDpi / 96f);

                var color = !tc.Enabled ? P.TextDisabled : selected || hot ? P.Text : P.TextMuted;
                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                            TextFormatFlags.EndEllipsis;
                if (tc.RightToLeft == RightToLeft.Yes) flags |= TextFormatFlags.RightToLeft;
                var textRect = new Rectangle(r.X, r.Y, r.Width, r.Height - underline);
                TextRenderer.DrawText(g, tc.TabPages[i].Text, tc.Font, textRect, color, flags);

                if (selected)
                {
                    using var brush = new SolidBrush(P.Accent);
                    g.FillRectangle(brush, r.X + 4, stripBottom - underline + 1, r.Width - 8, underline);
                }
            }
        }

        #endregion

        #region SplitContainer

        public static void HookSplitContainer(SplitContainer sc)
        {
            sc.Paint += (sender, e) =>
            {
                var r = sc.SplitterRectangle;
                using var pen = new Pen(P.Border);
                if (sc.Orientation == Orientation.Vertical)
                {
                    var x = r.X + r.Width / 2;
                    e.Graphics.DrawLine(pen, x, r.Top, x, r.Bottom);
                }
                else
                {
                    var y = r.Y + r.Height / 2;
                    e.Graphics.DrawLine(pen, r.Left, y, r.Right, y);
                }
            };
        }

        #endregion

        #region ToolTip

        public static void HookToolTip(ToolTip tt)
        {
            if (tt.IsBalloon || tt.OwnerDraw) return;
            tt.OwnerDraw = true;
            tt.BackColor = P.SurfaceElevated;
            tt.ForeColor = P.Text;
            tt.Draw += (sender, e) =>
            {
                var g = e.Graphics;
                using (var brush = new SolidBrush(P.SurfaceElevated))
                    g.FillRectangle(brush, e.Bounds);
                using (var pen = new Pen(P.Border))
                    g.DrawRectangle(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
                var textRect = Rectangle.Inflate(e.Bounds, -3, -2);
                TextRenderer.DrawText(g, e.ToolTipText, e.Font, textRect, P.Text,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.WordBreak);
            };
        }

        #endregion

        #region ProgressBar

        public static void HookProgressBar(ProgressBar pb)
        {
            _ = new ProgressBarPainter(pb);
        }

        /// <summary>
        ///     Draws a flat, rounded progress bar in place of the native one (which ignores colors under visual styles).
        /// </summary>
        private sealed class ProgressBarPainter : NativeWindow
        {
            private const int WM_PAINT = 0x000F;
            private const int WM_ERASEBKGND = 0x0014;
            private const int WM_PRINTCLIENT = 0x0318;
            private const int PBM_SETPOS = 0x0402;
            private const int PBM_DELTAPOS = 0x0403;
            private const int PBM_STEPIT = 0x0405;
            private const int PBM_SETRANGE32 = 0x0406;
            private const int PBM_SETSTATE = 0x0410;
            private const int PBM_GETSTATE = 0x0411;

            private readonly ProgressBar _bar;
            private readonly Timer _marqueeTimer = new() { Interval = 30 };

            public ProgressBarPainter(ProgressBar bar)
            {
                _bar = bar;
                _marqueeTimer.Tick += (s, e) =>
                {
                    if (Handle != IntPtr.Zero) InvalidateRect(Handle, IntPtr.Zero, false);
                };
                bar.HandleCreated += (s, e) => Attach();
                bar.HandleDestroyed += (s, e) =>
                {
                    _marqueeTimer.Stop();
                    ReleaseHandle();
                };
                bar.Disposed += (s, e) => _marqueeTimer.Dispose();
                bar.VisibleChanged += (s, e) => UpdateTimer();
                if (bar.IsHandleCreated) Attach();
            }

            private void Attach()
            {
                if (Handle != IntPtr.Zero) ReleaseHandle();
                AssignHandle(_bar.Handle);
                UpdateTimer();
                InvalidateRect(Handle, IntPtr.Zero, false);
            }

            private void UpdateTimer()
            {
                var run = _bar.Style == ProgressBarStyle.Marquee && _bar.Visible && Handle != IntPtr.Zero;
                if (run && !_marqueeTimer.Enabled) _marqueeTimer.Start();
                else if (!run && _marqueeTimer.Enabled) _marqueeTimer.Stop();
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case WM_ERASEBKGND:
                        m.Result = new IntPtr(1);
                        return;
                    case WM_PAINT:
                        var ps = new PAINTSTRUCT();
                        var hdc = BeginPaint(m.HWnd, ref ps);
                        try
                        {
                            using var g = Graphics.FromHdc(hdc);
                            Draw(g);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        finally
                        {
                            EndPaint(m.HWnd, ref ps);
                        }
                        m.Result = IntPtr.Zero;
                        return;
                    case WM_PRINTCLIENT:
                        using (var g = Graphics.FromHdc(m.WParam))
                            Draw(g);
                        m.Result = IntPtr.Zero;
                        return;
                    case PBM_SETPOS:
                    case PBM_DELTAPOS:
                    case PBM_STEPIT:
                    case PBM_SETRANGE32:
                    case PBM_SETSTATE:
                        base.WndProc(ref m);
                        InvalidateRect(m.HWnd, IntPtr.Zero, false);
                        UpdateTimer();
                        return;
                }

                base.WndProc(ref m);
            }

            private void Draw(Graphics g)
            {
                var client = _bar.ClientRectangle;
                if (client.Width <= 0 || client.Height <= 0) return;

                using var buffer = new Bitmap(client.Width, client.Height);
                using (var bg = Graphics.FromImage(buffer))
                {
                    bg.Clear(GetBack(_bar.Parent ?? _bar));
                    bg.SmoothingMode = SmoothingMode.AntiAlias;

                    var height = Math.Min(client.Height, Math.Max(4, (int)Math.Round(8 * _bar.DeviceDpi / 96f)));
                    var track = new RectangleF(0, (client.Height - height) / 2f, client.Width, height);
                    var radius = height / 2f;
                    ThemeManager.FillRounded(bg, P.Secondary, track, radius);

                    var state = Handle != IntPtr.Zero ? SendMessageInt(Handle, PBM_GETSTATE, IntPtr.Zero, IntPtr.Zero) : 1;
                    var fillColor = state == 2 ? P.Destructive : state == 3 ? P.Warning : P.Accent;
                    if (!_bar.Enabled) fillColor = ThemeManager.Blend(fillColor, P.Secondary, 0.5);

                    if (_bar.Style == ProgressBarStyle.Marquee)
                    {
                        var segment = track.Width * 0.3f;
                        var period = 1600d;
                        var t = Environment.TickCount64 % (long)period / period;
                        var x = (float)(-segment + (track.Width + segment) * t);
                        var seg = RectangleF.Intersect(new RectangleF(x, track.Y, segment, track.Height), track);
                        if (seg.Width > 0) ThemeManager.FillRounded(bg, fillColor, seg, radius);
                    }
                    else
                    {
                        var range = _bar.Maximum - _bar.Minimum;
                        var fraction = range <= 0 ? 0 : Math.Max(0, Math.Min(1, (_bar.Value - _bar.Minimum) / (double)range));
                        var width = (float)(track.Width * fraction);
                        if (width > 0)
                            ThemeManager.FillRounded(bg, fillColor, new RectangleF(track.X, track.Y, Math.Max(width, height), track.Height), radius);
                    }
                }
                g.DrawImageUnscaled(buffer, 0, 0);
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct PAINTSTRUCT
            {
                public IntPtr hdc;
                public bool fErase;
                public int left, top, right, bottom;
                public bool fRestore;
                public bool fIncUpdate;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
                public byte[] rgbReserved;
            }

            [DllImport("user32.dll")]
            private static extern IntPtr BeginPaint(IntPtr hwnd, ref PAINTSTRUCT ps);

            [DllImport("user32.dll")]
            private static extern bool EndPaint(IntPtr hwnd, ref PAINTSTRUCT ps);

            [DllImport("user32.dll")]
            private static extern bool InvalidateRect(IntPtr hwnd, IntPtr rect, bool erase);

            [DllImport("user32.dll", EntryPoint = "SendMessage")]
            private static extern int SendMessageInt(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        }

        #endregion
    }
}

/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Klocman.Forms.Theming
{
    /// <summary>
    ///     Central theme engine. Call <see cref="Initialize" /> once at startup, then <see cref="Attach" /> from every
    ///     form's constructor. Only colors, flat styles and owner drawing are changed, never layout or behavior.
    /// </summary>
    public static class ThemeManager
    {
        private static readonly object Marker = new();
        private static readonly ConditionalWeakTable<Control, object> Styled = new();
        private static readonly ConditionalWeakTable<Image, Image> ThemedImages = new();
        private static readonly ConditionalWeakTable<Image, object> ThemedImageResults = new();
        private static HashSet<int> _paletteColors = new();

        [ThreadStatic] private static bool _remapping;

        /// <summary>
        ///     Extra stylers for control types this library doesn't know about (e.g. ObjectListView).
        ///     Return true if the control was fully handled (its children are still walked).
        /// </summary>
        public static readonly List<Func<Control, bool>> CustomStylers = new();

        /// <summary>
        ///     True if the modern theme is in use. False means the classic, unthemed look.
        /// </summary>
        public static bool IsActive { get; private set; }

        public static bool IsDark => IsActive && Palette.IsDark;

        /// <summary>
        ///     The theme mode selected by the user (before resolving <see cref="ThemeMode.System" />).
        /// </summary>
        public static ThemeMode Mode { get; private set; }

        public static ThemePalette Palette { get; private set; } = ThemePalette.Light;

        public static ToolStripRenderer Renderer { get; private set; }

        /// <summary>
        ///     Set up the theme. Should be called once before any window is created.
        /// </summary>
        /// <param name="mode">Selected theme mode</param>
        /// <param name="enabled">If false the classic look is kept and nothing is changed</param>
        public static void Initialize(ThemeMode mode, bool enabled)
        {
            Mode = mode;
            IsActive = enabled;
            if (!enabled) return;

            Palette = ResolveDark(mode) ? ThemePalette.Dark : ThemePalette.Light;
            _paletteColors = new HashSet<int>(typeof(ThemePalette).GetProperties()
                .Where(p => p.PropertyType == typeof(Color) && p.Name != nameof(ThemePalette.AccentForeground))
                .Select(p => ((Color)p.GetValue(Palette)).ToArgb()));

            ThemeNative.InitializeAppMode(Palette.IsDark);

            Renderer = new ThemedToolStripRenderer(Palette);
            ToolStripManager.Renderer = Renderer;
        }

        public static bool ResolveDark(ThemeMode mode)
        {
            return mode switch
            {
                ThemeMode.Dark => true,
                ThemeMode.Light => false,
                _ => !IsWindowsAppThemeLight()
            };
        }

        /// <summary>
        ///     Windows app mode, HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme.
        /// </summary>
        public static bool IsWindowsAppThemeLight()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                if (value is int i) return i != 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return true;
        }

        /// <summary>
        ///     Theme the form: title bar, all current child controls and any controls added later.
        /// </summary>
        public static void Attach(Form form)
        {
            if (!IsActive || form == null) return;

            try
            {
                if (form.IsHandleCreated) OnFormHandleCreated(form, EventArgs.Empty);
                form.HandleCreated += OnFormHandleCreated;

                ApplyTo(form);
                // Some forms add or recolor controls in their constructor or OnLoad after Attach is called
                form.Load += (sender, args) => ApplyTo(form);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void OnFormHandleCreated(object sender, EventArgs e)
        {
            var form = (Form)sender;
            ThemeNative.AllowDarkModeForWindow(form.Handle, Palette.IsDark);
            ThemeNative.ApplyTitleBar(form.Handle, Palette);
        }

        /// <summary>
        ///     Theme a control and all of its children. Safe to call multiple times.
        /// </summary>
        public static void ApplyTo(Control root)
        {
            if (!IsActive || root == null) return;

            try
            {
                StyleRecursive(root);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void StyleRecursive(Control c)
        {
            StyleControl(c);
            foreach (Control child in c.Controls)
                StyleRecursive(child);
        }

        private static void StyleControl(Control c)
        {
            if (Styled.TryGetValue(c, out _)) return;
            Styled.Add(c, Marker);

            c.ControlAdded += (sender, args) => ApplyTo(args.Control);

            var handled = false;
            foreach (var styler in CustomStylers)
            {
                try
                {
                    if (styler(c))
                    {
                        handled = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            if (!handled)
            {
                switch (c)
                {
                    case Form f:
                        f.BackColor = Palette.Background;
                        f.ForeColor = Palette.Text;
                        break;
                    case ToolStrip ts:
                        StyleToolStrip(ts);
                        break;
                    case ButtonBase b:
                        StyleButtonBase(b);
                        break;
                    case TextBoxBase tb:
                        StyleTextBox(tb);
                        break;
                    case ComboBox cb:
                        StyleComboBox(cb);
                        break;
                    case ListBox lb:
                        SetColors(c, Palette.Surface);
                        if (lb.BorderStyle == BorderStyle.Fixed3D) lb.BorderStyle = BorderStyle.FixedSingle;
                        OnHandle(c, x => ThemeNative.TrySetWindowTheme(x.Handle, Palette.IsDark ? "DarkMode_Explorer" : "Explorer"));
                        break;
                    case ListView lv:
                        StyleListView(lv);
                        break;
                    case TreeView tv:
                        SetColors(c, Palette.ListBackground);
                        tv.LineColor = Palette.BorderStrong;
                        OnHandle(c, x => ThemeNative.TrySetWindowTheme(x.Handle, Palette.IsDark ? "DarkMode_Explorer" : "Explorer"));
                        break;
                    case DataGridView dgv:
                        StyleDataGridView(dgv);
                        break;
                    case UpDownBase ud:
                        SetColors(c, Palette.Surface);
                        foreach (Control child in ud.Controls)
                            OnHandle(child, x => ThemeNative.TrySetWindowTheme(x.Handle, Palette.IsDark ? "DarkMode_CFD" : null));
                        break;
                    case GroupBox gb:
                        SetColors(c, null);
                        ThemePainters.HookGroupBox(gb);
                        break;
                    case TabControl tc:
                        SetColors(c, null);
                        ThemePainters.HookTabControl(tc);
                        break;
                    case TabPage tp:
                        tp.UseVisualStyleBackColor = false;
                        tp.BackColor = Palette.Background;
                        tp.ForeColor = Palette.Text;
                        break;
                    case SplitContainer sc:
                        SetColors(c, null);
                        ThemePainters.HookSplitContainer(sc);
                        break;
                    case ProgressBar pb:
                        ThemePainters.HookProgressBar(pb);
                        break;
                    case LinkLabel ll:
                        SetColors(c, null);
                        ll.LinkColor = Palette.AccentText;
                        ll.VisitedLinkColor = Palette.AccentText;
                        ll.ActiveLinkColor = Palette.AccentHover;
                        ll.DisabledLinkColor = Palette.TextDisabled;
                        break;
                    case PictureBox pic:
                        SetColors(c, null);
                        if (pic.Image != null) pic.Image = GetThemedImage(pic.Image);
                        break;
                    case WebBrowser:
                        break;
                    default:
                        SetColors(c, null);
                        if (c is ScrollableControl { AutoScroll: true })
                            OnHandle(c, x => ThemeNative.TrySetWindowTheme(x.Handle, Palette.IsDark ? "DarkMode_Explorer" : "Explorer"));
                        if (c is Panel { BorderStyle: BorderStyle.Fixed3D } panel)
                            panel.BorderStyle = BorderStyle.FixedSingle;
                        break;
                }
            }

            if (c is ContainerControl or UserControl)
                StyleComponentFields(c);

            // Keep colors set later by the application readable
            c.BackColorChanged += OnBackColorChanged;
            c.ForeColorChanged += OnForeColorChanged;
        }

        #region Colors

        /// <summary>
        ///     Remap the control's colors to the palette. Ambient (inherited) colors are left alone so that
        ///     they follow the parent.
        /// </summary>
        /// <param name="c">Control to change</param>
        /// <param name="windowColor">Color to use if the control uses the "Window" system color, null for default</param>
        private static void SetColors(Control c, Color? windowColor)
        {
            try
            {
                if (!IsAmbient(c, nameof(Control.BackColor)))
                {
                    var newBack = MapBack(c.BackColor, windowColor);
                    if (newBack != c.BackColor) c.BackColor = newBack;
                }
            }
            catch (ArgumentException)
            {
                // Control doesn't support this color (e.g. transparent)
            }

            if (!IsAmbient(c, nameof(Control.ForeColor)))
            {
                var newFore = MapFore(c.ForeColor, GetEffectiveBackColor(c));
                if (newFore != c.ForeColor) c.ForeColor = newFore;
            }
        }

        private static bool IsAmbient(Control c, string propertyName)
        {
            // Root controls are never ambient
            if (c.Parent == null) return false;
            try
            {
                var prop = TypeDescriptor.GetProperties(c)[propertyName];
                if (prop == null || prop.ShouldSerializeValue(c)) return false;

                // Some controls (inputs, lists) have their own default color instead of inheriting
                var value = (Color)prop.GetValue(c)!;
                var parentValue = (Color)prop.GetValue(c.Parent)!;
                return value == parentValue;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPaletteColor(Color c) => !c.IsSystemColor && _paletteColors.Contains(c.ToArgb());

        /// <summary>
        ///     Map a background color to the palette. Intentional, colored backgrounds are kept.
        /// </summary>
        public static Color MapBack(Color color, Color? windowColor = null)
        {
            if (!IsActive || color.IsEmpty || color.A == 0 || IsPaletteColor(color)) return color;

            if (color.IsSystemColor)
            {
                switch (color.ToKnownColor())
                {
                    case KnownColor.Window:
                    case KnownColor.ControlLightLight:
                    case KnownColor.Info:
                        return windowColor ?? Palette.Surface;
                    case KnownColor.ControlDark:
                    case KnownColor.ButtonShadow:
                        return Palette.Border;
                    case KnownColor.ControlDarkDark:
                    case KnownColor.WindowFrame:
                        return Palette.BorderStrong;
                    case KnownColor.Highlight:
                    case KnownColor.MenuHighlight:
                    case KnownColor.HotTrack:
                        return Palette.Selection;
                    case KnownColor.WindowText:
                    case KnownColor.ControlText:
                    case KnownColor.MenuText:
                    case KnownColor.InfoText:
                        return Palette.Text;
                    default:
                        return Palette.Background;
                }
            }

            // White and near-white panels become surfaces
            if (color.R >= 0xF0 && color.G >= 0xF0 && color.B >= 0xF0)
                return windowColor ?? Palette.Surface;

            return color;
        }

        /// <summary>
        ///     Map a foreground color to the palette, keeping intentional colors but making sure they stay readable.
        /// </summary>
        public static Color MapFore(Color color, Color background)
        {
            if (!IsActive || color.IsEmpty || IsPaletteColor(color)) return color;

            if (color.IsSystemColor)
            {
                switch (color.ToKnownColor())
                {
                    case KnownColor.GrayText:
                    case KnownColor.ControlDark:
                    case KnownColor.ControlDarkDark:
                    case KnownColor.InactiveCaptionText:
                        return Palette.TextMuted;
                    case KnownColor.HotTrack:
                        return Palette.AccentText;
                    case KnownColor.HighlightText:
                        return Palette.SelectionText;
                    default:
                        return Palette.Text;
                }
            }

            if (color.R == 0 && color.G == 0 && color.B == 0)
                return Palette.Text;

            return EnsureContrast(color, background, 3.5);
        }

        private static Color GetEffectiveBackColor(Control c)
        {
            for (var x = c; x != null; x = x.Parent)
            {
                if (x.BackColor.A == 255) return x.BackColor;
            }
            return Palette.Background;
        }

        private static void OnBackColorChanged(object sender, EventArgs e)
        {
            if (_remapping || !IsActive) return;
            var c = (Control)sender;
            if (IsAmbient(c, nameof(Control.BackColor))) return;
            var mapped = MapBack(c.BackColor, c is TextBoxBase or ComboBox or ListBox or UpDownBase ? Palette.Surface : null);
            if (mapped == c.BackColor) return;
            _remapping = true;
            try { c.BackColor = mapped; }
            catch (ArgumentException) { }
            finally { _remapping = false; }
        }

        private static void OnForeColorChanged(object sender, EventArgs e)
        {
            if (_remapping || !IsActive) return;
            var c = (Control)sender;
            if (c is ButtonBase { FlatStyle: FlatStyle.Flat } b && b.BackColor == Palette.Primary) return;
            if (IsAmbient(c, nameof(Control.ForeColor))) return;
            var mapped = MapFore(c.ForeColor, GetEffectiveBackColor(c));
            if (mapped == c.ForeColor) return;
            _remapping = true;
            try { c.ForeColor = mapped; }
            finally { _remapping = false; }
        }

        public static double Luminance(Color c)
        {
            static double Channel(int v)
            {
                var s = v / 255d;
                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }

        public static double Contrast(Color a, Color b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        /// <summary>
        ///     Blend <paramref name="top" /> over <paramref name="bottom" /> with the given opacity (0-1).
        /// </summary>
        public static Color Blend(Color top, Color bottom, double amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));
            return Color.FromArgb(
                (int)Math.Round(top.R * amount + bottom.R * (1 - amount)),
                (int)Math.Round(top.G * amount + bottom.G * (1 - amount)),
                (int)Math.Round(top.B * amount + bottom.B * (1 - amount)));
        }

        /// <summary>
        ///     Move the color towards the theme's text color until it reaches the minimum contrast ratio.
        /// </summary>
        public static Color EnsureContrast(Color color, Color background, double minRatio)
        {
            if (Contrast(color, background) >= minRatio) return color;
            var target = Luminance(background) > 0.4 ? Color.Black : Color.White;
            for (var i = 1; i <= 10; i++)
            {
                var candidate = Blend(target, color, i / 10d);
                if (Contrast(candidate, background) >= minRatio) return candidate;
            }
            return target;
        }

        #endregion

        #region Specific controls

        private static void StyleToolStrip(ToolStrip ts)
        {
            if (ts.RenderMode != ToolStripRenderMode.ManagerRenderMode || ts.Renderer != Renderer)
                ts.Renderer = Renderer;
            ts.BackColor = ts is ToolStripDropDown ? Palette.SurfaceElevated : Palette.Background;
            ts.ForeColor = Palette.Text;
            if (ts is StatusStrip ss) ss.SizingGrip = false;
        }

        private static void StyleButtonBase(ButtonBase b)
        {
            if (b is CheckBox { Appearance: Appearance.Normal } or RadioButton { Appearance: Appearance.Normal })
            {
                SetColors(b, null);
                if (b.FlatStyle != FlatStyle.System)
                    ThemePainters.HookCheckable(b);
                return;
            }

            if (b.FlatStyle == FlatStyle.System)
            {
                // Native control that can't be recolored (e.g. command links)
                OnHandle(b, x => ThemeNative.TrySetWindowTheme(x.Handle, Palette.IsDark ? "DarkMode_Explorer" : "Explorer"));
                return;
            }

            var parentBack = b.Parent != null ? GetEffectiveBackColor(b.Parent) : Palette.Background;

            var customBack = b.ShouldSerializeColor(nameof(Control.BackColor)) && !b.BackColor.IsSystemColor &&
                             !IsPaletteColor(b.BackColor) && MapBack(b.BackColor) == b.BackColor;

            b.UseVisualStyleBackColor = false;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;

            var isPrimary = b is Button && b.FindForm()?.AcceptButton == b;
            if (isPrimary)
            {
                b.BackColor = Palette.Primary;
                b.ForeColor = Palette.PrimaryForeground;
                b.FlatAppearance.BorderColor = Palette.Primary;
                b.FlatAppearance.MouseOverBackColor = Palette.PrimaryHover;
                b.FlatAppearance.MouseDownBackColor = Palette.PrimaryHover;
            }
            else if (customBack)
            {
                b.FlatAppearance.BorderColor = Blend(Color.Black, b.BackColor, 0.15);
                b.ForeColor = EnsureContrast(b.ForeColor, b.BackColor, 4.5);
            }
            else
            {
                b.BackColor = parentBack;
                b.ForeColor = Palette.Text;
                b.FlatAppearance.BorderColor = Palette.BorderStrong;
                b.FlatAppearance.MouseOverBackColor = Palette.Hover;
                b.FlatAppearance.MouseDownBackColor = Palette.Pressed;
            }

            if (b is CheckBox or RadioButton)
                b.FlatAppearance.CheckedBackColor = Palette.AccentTint;

            if (b.Image != null) b.Image = GetThemedImage(b.Image);
            else ThemePainters.HookDisabledButton(b);
        }

        private static bool ShouldSerializeColor(this Control c, string name)
        {
            try
            {
                return TypeDescriptor.GetProperties(c)[name]?.ShouldSerializeValue(c) == true;
            }
            catch
            {
                return false;
            }
        }

        private static void StyleTextBox(TextBoxBase tb)
        {
            // Read-only text boxes use the "Control" color unless BackColor is set explicitly
            var wasReadOnlyGray = tb.ReadOnly && !tb.ShouldSerializeColor(nameof(Control.BackColor));
            if (wasReadOnlyGray)
                tb.BackColor = Palette.Background;
            else
                SetColors(tb, Palette.Surface);
            if (!IsPaletteColor(tb.ForeColor)) tb.ForeColor = MapFore(tb.ForeColor, tb.BackColor);

            // Fixed3D frames are drawn by the visual style; the dark CFD theme gives them a dark frame
            OnHandle(tb, x =>
            {
                var multiline = tb.Multiline || tb is RichTextBox;
                if (Palette.IsDark)
                    ThemeNative.TrySetWindowTheme(x.Handle, multiline ? "DarkMode_Explorer" : "DarkMode_CFD");
                ThemeNative.AllowDarkModeForWindow(x.Handle, Palette.IsDark);
            });
        }

        private static void StyleComboBox(ComboBox cb)
        {
            SetColors(cb, Palette.Surface);
            OnHandle(cb, x =>
            {
                if (Palette.IsDark)
                    ThemeNative.TrySetWindowTheme(x.Handle, "DarkMode_CFD");
                ThemeNative.AllowDarkModeForWindow(x.Handle, Palette.IsDark);
            });
        }

        private static void StyleListView(ListView lv)
        {
            SetColors(lv, Palette.ListBackground);
            if (lv.BorderStyle == BorderStyle.Fixed3D) lv.BorderStyle = BorderStyle.FixedSingle;
            lv.GridLines = false;
            OnHandle(lv, x => ApplyListViewNativeTheme(x.Handle));
        }

        /// <summary>
        ///     Native dark scrollbars for a list view and a dark native header.
        /// </summary>
        public static void ApplyListViewNativeTheme(IntPtr listViewHandle)
        {
            if (!IsActive) return;
            ThemeNative.AllowDarkModeForWindow(listViewHandle, Palette.IsDark);
            ThemeNative.TrySetWindowTheme(listViewHandle, Palette.IsDark ? "DarkMode_Explorer" : "Explorer");
            var header = ThemeNative.SendMessage(listViewHandle, ThemeNative.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            if (header != IntPtr.Zero)
            {
                ThemeNative.AllowDarkModeForWindow(header, Palette.IsDark);
                ThemeNative.TrySetWindowTheme(header, Palette.IsDark ? "DarkMode_ItemsView" : "ItemsView");
            }
        }

        private static void StyleDataGridView(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.BackgroundColor = Palette.ListBackground;
            dgv.GridColor = Palette.Border;
            if (dgv.BorderStyle == BorderStyle.Fixed3D) dgv.BorderStyle = BorderStyle.FixedSingle;

            void StyleCells(DataGridViewCellStyle s, Color back)
            {
                s.BackColor = back;
                s.ForeColor = Palette.Text;
                s.SelectionBackColor = Palette.Selection;
                s.SelectionForeColor = Palette.SelectionText;
            }

            StyleCells(dgv.DefaultCellStyle, Palette.ListBackground);
            StyleCells(dgv.ColumnHeadersDefaultCellStyle, Palette.Background);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Palette.TextMuted;
            StyleCells(dgv.RowHeadersDefaultCellStyle, Palette.Background);
            if (!dgv.AlternatingRowsDefaultCellStyle.BackColor.IsEmpty)
                dgv.AlternatingRowsDefaultCellStyle.BackColor = Palette.AlternateRow;
            dgv.ForeColor = Palette.Text;
            dgv.BackColor = Palette.ListBackground;
            OnHandle(dgv, x => ThemeNative.TrySetWindowTheme(x.Handle, Palette.IsDark ? "DarkMode_Explorer" : "Explorer"));
        }

        private static readonly Type[] ComponentFieldTypes = { typeof(ToolTip), typeof(ContextMenuStrip) };

        /// <summary>
        ///     Theme components that are not part of the control tree (tooltips, context menus).
        /// </summary>
        private static void StyleComponentFields(Control owner)
        {
            for (var type = owner.GetType(); type != null && type != typeof(ContainerControl) && type != typeof(Control); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (!ComponentFieldTypes.Any(t => t.IsAssignableFrom(field.FieldType))) continue;
                    try
                    {
                        switch (field.GetValue(owner))
                        {
                            case ToolTip tt:
                                ThemePainters.HookToolTip(tt);
                                break;
                            case ContextMenuStrip cms:
                                ApplyTo(cms);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     Run the action now if the handle exists, and every time it is (re)created.
        /// </summary>
        public static void OnHandle(Control c, Action<Control> action)
        {
            if (c.IsHandleCreated) action(c);
            c.HandleCreated += (sender, args) => action((Control)sender);
        }

        #endregion

        #region Images

        /// <summary>
        ///     In dark mode, returns a light version of dark monochrome glyphs so they stay visible.
        ///     Colored images and all images in light mode are returned unchanged. Results are cached.
        /// </summary>
        public static Image GetThemedImage(Image image)
        {
            if (image == null || !IsDark) return image;
            if (ThemedImageResults.TryGetValue(image, out _)) return image;
            if (ThemedImages.TryGetValue(image, out var cached)) return cached;

            Image result = image;
            try
            {
                if (image is Bitmap bmp && IsDarkGlyph(bmp))
                    result = LightenGlyph(bmp);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            ThemedImages.AddOrUpdate(image, result);
            if (!ReferenceEquals(result, image))
                ThemedImageResults.AddOrUpdate(result, Marker);
            return result;
        }

        private static bool IsDarkGlyph(Bitmap bmp)
        {
            if (bmp.Width > 1024 || bmp.Height > 1024 || ImageAnimator.CanAnimate(bmp)) return false;

            var step = Math.Max(1, Math.Max(bmp.Width, bmp.Height) / 64);
            int opaque = 0, darkGray = 0, colored = 0;
            using var copy = new Bitmap(bmp);
            for (var y = 0; y < copy.Height; y += step)
            for (var x = 0; x < copy.Width; x += step)
            {
                var p = copy.GetPixel(x, y);
                if (p.A < 64) continue;
                var sat = p.GetSaturation();
                var bri = p.GetBrightness();
                // White fill (paper, backgrounds) doesn't decide anything
                if (bri > 0.92f && sat < 0.15f) continue;
                opaque++;
                if (sat > 0.3f && bri > 0.15f && bri < 0.9f) colored++;
                else if (bri < 0.45f) darkGray++;
            }

            if (opaque == 0 || darkGray == 0) return false;
            return darkGray >= opaque * 0.45 && colored <= opaque * 0.35;
        }

        private static Bitmap LightenGlyph(Bitmap source)
        {
            var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
            using (var g = Graphics.FromImage(result))
                g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height));

            var rect = new Rectangle(0, 0, result.Width, result.Height);
            var data = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                var bytes = new byte[data.Stride * data.Height];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                var text = Palette.Text;
                var back = Palette.Background;
                for (var i = 0; i < bytes.Length; i += 4)
                {
                    if (bytes[i + 3] == 0) continue;
                    int b = bytes[i], g = bytes[i + 1], r = bytes[i + 2];
                    var max = Math.Max(r, Math.Max(g, b));
                    var min = Math.Min(r, Math.Min(g, b));
                    var sat = max == 0 ? 0 : (max - min) / (double)max;
                    if (sat > 0.35 && max > 60) continue; // Keep colored accents

                    // Invert lightness of gray pixels: black becomes the text color, white becomes the background
                    var inv = 1 - (r + g + b) / 3d / 255d;
                    bytes[i] = (byte)Math.Round(back.B + (text.B - back.B) * inv);
                    bytes[i + 1] = (byte)Math.Round(back.G + (text.G - back.G) * inv);
                    bytes[i + 2] = (byte)Math.Round(back.R + (text.R - back.R) * inv);
                }
                System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
            }
            finally
            {
                result.UnlockBits(data);
            }
            return result;
        }

        #endregion

        #region Drawing helpers

        public static int Scale(Control c, int pixels) => c == null ? pixels : (int)Math.Round(pixels * c.DeviceDpi / 96f);

        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0.5f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, Color color, RectangleF r, float radius)
        {
            using var path = RoundedRect(r, radius);
            using var brush = new SolidBrush(color);
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillPath(brush, path);
            g.SmoothingMode = old;
        }

        public static void DrawRounded(Graphics g, Color color, RectangleF r, float radius, float width = 1f)
        {
            using var path = RoundedRect(r, radius);
            using var pen = new Pen(color, width);
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawPath(pen, path);
            g.SmoothingMode = old;
        }

        #endregion
    }
}

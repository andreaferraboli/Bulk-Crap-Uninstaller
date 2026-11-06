/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System.Drawing;

namespace Klocman.Forms.Theming
{
    /// <summary>
    ///     Design tokens for the modern UI restyle (21st.dev "Linear" theme, e25bc0be-03b2-4200-9b7a-b301143552a3).
    ///     Use <see cref="Dark" /> or <see cref="Light" />, or <see cref="ThemeManager.Palette" /> for the active one.
    /// </summary>
    public sealed class ThemePalette
    {
        public static readonly ThemePalette Dark = new()
        {
            IsDark = true,
            Background = Color.FromArgb(0x13, 0x13, 0x15),
            Sidebar = Color.FromArgb(0x0F, 0x0F, 0x10),
            Surface = Color.FromArgb(0x1D, 0x1D, 0x20),
            ListBackground = Color.FromArgb(0x13, 0x13, 0x15),
            SurfaceElevated = Color.FromArgb(0x22, 0x22, 0x25),
            Border = Color.FromArgb(0x2C, 0x2C, 0x30),
            BorderStrong = Color.FromArgb(0x3A, 0x3A, 0x40),
            Text = Color.FromArgb(0xF2, 0xF2, 0xF2),
            TextMuted = Color.FromArgb(0x94, 0x94, 0x9E),
            TextDisabled = Color.FromArgb(0x61, 0x61, 0x6B),
            Primary = Color.FromArgb(0xF2, 0xF2, 0xF2),
            PrimaryForeground = Color.FromArgb(0x13, 0x13, 0x15),
            PrimaryHover = Color.FromArgb(0xDC, 0xDC, 0xDC),
            Secondary = Color.FromArgb(0x27, 0x27, 0x2B),
            Accent = Color.FromArgb(0x99, 0x56, 0xF5),
            AccentHover = Color.FromArgb(0xA7, 0x6E, 0xF7),
            AccentForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),
            AccentText = Color.FromArgb(0xA7, 0x6E, 0xF7),
            AccentTint = Color.FromArgb(0x27, 0x1D, 0x37),
            Hover = Color.FromArgb(0x27, 0x27, 0x2B),
            RowHover = Color.FromArgb(0x1B, 0x1B, 0x1D),
            Pressed = Color.FromArgb(0x2C, 0x2C, 0x30),
            Selection = Color.FromArgb(0x30, 0x22, 0x46),
            SelectionUnfocused = Color.FromArgb(0x20, 0x1A, 0x2B),
            SelectionText = Color.FromArgb(0xF2, 0xF2, 0xF2),
            Destructive = Color.FromArgb(0xE8, 0x30, 0x30),
            DestructiveHover = Color.FromArgb(0xD3, 0x2D, 0x2D),
            DestructiveText = Color.FromArgb(0xEB, 0x47, 0x47),
            DestructiveTint = Color.FromArgb(0x33, 0x17, 0x19),
            Warning = Color.FromArgb(0xFF, 0x95, 0x00),
            WarningText = Color.FromArgb(0xFB, 0xBF, 0x24),
            WarningTint = Color.FromArgb(0x2A, 0x21, 0x14),
            Success = Color.FromArgb(0x10, 0xB9, 0x81),
            SuccessText = Color.FromArgb(0x34, 0xD3, 0x99),
            SuccessTint = Color.FromArgb(0x13, 0x24, 0x20),
            Info = Color.FromArgb(0x33, 0x99, 0xFF),
            FocusRing = Color.FromArgb(0x99, 0x56, 0xF5),
            FocusHalo = Color.FromArgb(0x56, 0x35, 0x85),
            ScrollThumb = Color.FromArgb(0x3F, 0x3F, 0x46),
            ScrollThumbHover = Color.FromArgb(0x52, 0x52, 0x5B),
            AlternateRow = Color.FromArgb(0x17, 0x17, 0x19),
        };

        public static readonly ThemePalette Light = new()
        {
            IsDark = false,
            Background = Color.FromArgb(0xFA, 0xFA, 0xFA),
            Sidebar = Color.FromArgb(0xF5, 0xF5, 0xF5),
            Surface = Color.FromArgb(0xFF, 0xFF, 0xFF),
            ListBackground = Color.FromArgb(0xFF, 0xFF, 0xFF),
            SurfaceElevated = Color.FromArgb(0xFF, 0xFF, 0xFF),
            Border = Color.FromArgb(0xE4, 0xE4, 0xE7),
            BorderStrong = Color.FromArgb(0xD4, 0xD4, 0xD8),
            Text = Color.FromArgb(0x24, 0x24, 0x28),
            TextMuted = Color.FromArgb(0x6D, 0x6D, 0x78),
            TextDisabled = Color.FromArgb(0xA1, 0xA1, 0xAA),
            Primary = Color.FromArgb(0x24, 0x24, 0x28),
            PrimaryForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),
            PrimaryHover = Color.FromArgb(0x3A, 0x3A, 0x3D),
            Secondary = Color.FromArgb(0xEA, 0xEA, 0xEC),
            Accent = Color.FromArgb(0x8A, 0x3E, 0xF4),
            AccentHover = Color.FromArgb(0x7B, 0x26, 0xF2),
            AccentForeground = Color.FromArgb(0xFF, 0xFF, 0xFF),
            AccentText = Color.FromArgb(0x8A, 0x3E, 0xF4),
            AccentTint = Color.FromArgb(0xF3, 0xEB, 0xFE),
            Hover = Color.FromArgb(0xEF, 0xEF, 0xF0),
            RowHover = Color.FromArgb(0xF7, 0xF7, 0xF8),
            Pressed = Color.FromArgb(0xEA, 0xEA, 0xEC),
            Selection = Color.FromArgb(0xF0, 0xE6, 0xFE),
            SelectionUnfocused = Color.FromArgb(0xF7, 0xF1, 0xFE),
            SelectionText = Color.FromArgb(0x24, 0x24, 0x28),
            Destructive = Color.FromArgb(0xE8, 0x30, 0x30),
            DestructiveHover = Color.FromArgb(0xEA, 0x45, 0x45),
            DestructiveText = Color.FromArgb(0xE8, 0x30, 0x30),
            DestructiveTint = Color.FromArgb(0xFD, 0xEA, 0xEA),
            Warning = Color.FromArgb(0xFF, 0x95, 0x00),
            WarningText = Color.FromArgb(0xB4, 0x53, 0x09),
            WarningTint = Color.FromArgb(0xFE, 0xF0, 0xDA),
            Success = Color.FromArgb(0x10, 0xB9, 0x81),
            SuccessText = Color.FromArgb(0x04, 0x78, 0x57),
            SuccessTint = Color.FromArgb(0xDB, 0xF5, 0xEC),
            Info = Color.FromArgb(0x00, 0x80, 0xFF),
            FocusRing = Color.FromArgb(0x8A, 0x3E, 0xF4),
            FocusHalo = Color.FromArgb(0xCC, 0xAB, 0xFA),
            ScrollThumb = Color.FromArgb(0xC4, 0xC4, 0xCA),
            ScrollThumbHover = Color.FromArgb(0xA4, 0xA4, 0xAD),
            AlternateRow = Color.FromArgb(0xFB, 0xFB, 0xFB),
        };

        private ThemePalette() { }

        public bool IsDark { get; private init; }

        public Color Background { get; private init; }
        public Color Sidebar { get; private init; }
        public Color Surface { get; private init; }
        public Color ListBackground { get; private init; }
        public Color SurfaceElevated { get; private init; }
        public Color Border { get; private init; }
        public Color BorderStrong { get; private init; }
        public Color Text { get; private init; }
        public Color TextMuted { get; private init; }
        public Color TextDisabled { get; private init; }
        public Color Primary { get; private init; }
        public Color PrimaryForeground { get; private init; }
        public Color PrimaryHover { get; private init; }
        public Color Secondary { get; private init; }
        public Color Accent { get; private init; }
        public Color AccentHover { get; private init; }
        public Color AccentForeground { get; private init; }
        public Color AccentText { get; private init; }
        public Color AccentTint { get; private init; }
        public Color Hover { get; private init; }
        public Color RowHover { get; private init; }
        public Color Pressed { get; private init; }
        public Color Selection { get; private init; }
        public Color SelectionUnfocused { get; private init; }
        public Color SelectionText { get; private init; }
        public Color Destructive { get; private init; }
        public Color DestructiveHover { get; private init; }
        public Color DestructiveText { get; private init; }
        public Color DestructiveTint { get; private init; }
        public Color Warning { get; private init; }
        public Color WarningText { get; private init; }
        public Color WarningTint { get; private init; }
        public Color Success { get; private init; }
        public Color SuccessText { get; private init; }
        public Color SuccessTint { get; private init; }
        public Color Info { get; private init; }
        public Color FocusRing { get; private init; }
        public Color FocusHalo { get; private init; }
        public Color ScrollThumb { get; private init; }
        public Color ScrollThumbHover { get; private init; }
        public Color AlternateRow { get; private init; }

        /// <summary>
        ///     Pixel metrics at 96 DPI; scale by DeviceDpi / 96f.
        /// </summary>
        public static class Metrics
        {
            public const int RowHeight = 28, HeaderHeight = 32, ToolbarHeight = 40, MenuHeight = 32,
                MenuItemHeight = 28, StatusHeight = 26, ControlHeight = 32, SidebarWidth = 256,
                Radius = 6, RadiusMd = 4, RadiusSm = 3, Border = 1, FocusRing = 2,
                DialogPadding = 24, Gap = 8, IconSize = 16;
        }
    }
}

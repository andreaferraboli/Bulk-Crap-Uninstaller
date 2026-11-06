/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Klocman.Forms.Theming
{
    /// <summary>
    ///     Native calls used by the theme engine. Every call is best-effort and never throws.
    /// </summary>
    internal static class ThemeNative
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        public const int DWMWCP_ROUND = 2;
        public const int DWMWCP_ROUNDSMALL = 3;

        public const int LVM_GETHEADER = 0x101F;

        public static readonly int OsBuild = Environment.OSVersion.Version.Build;
        public static bool IsWindows10 => Environment.OSVersion.Version.Major >= 10;
        public static bool IsWindows11 => IsWindows10 && OsBuild >= 22000;
        public static bool SupportsDarkMode => IsWindows10 && OsBuild >= 17763;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr ordinal);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int SetPreferredAppModeDelegate(int mode);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool AllowDarkModeForWindowDelegate(IntPtr hwnd, bool allow);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void FlushMenuThemesDelegate();

        private static AllowDarkModeForWindowDelegate _allowDarkModeForWindow;
        private static bool _uxthemeLoaded;

        /// <summary>
        ///     Undocumented uxtheme ordinals (Windows 10 1809+): SetPreferredAppMode (#135), FlushMenuThemes (#136).
        ///     Makes native popup menus and scrollbars follow dark mode.
        /// </summary>
        public static void InitializeAppMode(bool dark)
        {
            if (!SupportsDarkMode) return;
            try
            {
                var uxtheme = LoadLibraryEx("uxtheme.dll", IntPtr.Zero, 0x00000800 /*LOAD_LIBRARY_SEARCH_SYSTEM32*/);
                if (uxtheme == IntPtr.Zero) return;

                var allowForWindow = GetProcAddress(uxtheme, new IntPtr(133));
                if (allowForWindow != IntPtr.Zero)
                    _allowDarkModeForWindow = Marshal.GetDelegateForFunctionPointer<AllowDarkModeForWindowDelegate>(allowForWindow);

                // On 1809 ordinal 135 is AllowDarkModeForApp(bool), from 1903 it's SetPreferredAppMode(mode).
                // Passing 1 works for both (true / AllowDark), 2 is ForceDark and 3 is ForceLight.
                var setMode = GetProcAddress(uxtheme, new IntPtr(135));
                if (setMode != IntPtr.Zero)
                {
                    var fn = Marshal.GetDelegateForFunctionPointer<SetPreferredAppModeDelegate>(setMode);
                    fn(OsBuild >= 18362 ? (dark ? 2 : 3) : (dark ? 1 : 0));
                }

                var flush = GetProcAddress(uxtheme, new IntPtr(136));
                if (flush != IntPtr.Zero)
                    Marshal.GetDelegateForFunctionPointer<FlushMenuThemesDelegate>(flush)();

                _uxthemeLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void AllowDarkModeForWindow(IntPtr hwnd, bool allow)
        {
            if (!_uxthemeLoaded || _allowDarkModeForWindow == null || hwnd == IntPtr.Zero) return;
            try { _allowDarkModeForWindow(hwnd, allow); }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        public static void TrySetWindowTheme(IntPtr hwnd, string subApp)
        {
            if (hwnd == IntPtr.Zero) return;
            try { SetWindowTheme(hwnd, subApp, null); }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        /// <summary>
        ///     Immersive dark title bar (attribute 20, 19 on old Windows 10 builds) plus Windows 11 caption, border and text colors.
        /// </summary>
        public static void ApplyTitleBar(IntPtr hwnd, ThemePalette palette)
        {
            if (hwnd == IntPtr.Zero || !IsWindows10) return;
            try
            {
                var dark = palette.IsDark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int)) != 0)
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref dark, sizeof(int));

                if (IsWindows11)
                {
                    var caption = ToColorRef(palette.Background);
                    DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
                    var border = ToColorRef(palette.Border);
                    DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref border, sizeof(int));
                    var text = ToColorRef(palette.Text);
                    DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, sizeof(int));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void SetCornerPreference(IntPtr hwnd, int preference)
        {
            if (hwnd == IntPtr.Zero || !IsWindows11) return;
            try { DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int)); }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private static int ToColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);
    }
}

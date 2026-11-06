/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

namespace Klocman.Forms.Theming
{
    /// <summary>
    ///     Color theme of the modern UI.
    /// </summary>
    public enum ThemeMode
    {
        Dark = 0,
        Light = 1,

        /// <summary>
        ///     Follow the Windows "app mode" (HKCU\...\Themes\Personalize\AppsUseLightTheme).
        /// </summary>
        System = 2
    }
}

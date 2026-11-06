/*
    Copyright (c) 2018 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System.Drawing;

namespace BulkCrapUninstaller.Functions.ApplicationList
{
    internal class ApplicationListColors
    {
        public static ApplicationListColors Normal = new(
            Color.FromArgb(unchecked((int)0xffccffcc)), Color.FromArgb(unchecked((int)0xffbbddff)),
            Color.FromArgb(unchecked((int)0xffE0E0E0)), Color.FromArgb(unchecked((int) 0xffffdbcd)),
            Color.FromArgb(unchecked((int) 0xffe7cfff)), Color.FromArgb(unchecked((int)0xffa3ffff)));

        public static ApplicationListColors ColorBlind = new(
            Color.FromArgb(unchecked((int) 0xfff6382d)), Color.FromArgb(unchecked((int)0xfffc8d59)),
            Color.FromArgb(unchecked((int) 0xff5189d3)), Color.FromArgb(unchecked((int)0xff91bfdb)),
            Color.FromArgb(unchecked((int)0xfffee090)), Color.FromArgb(unchecked((int) 0xffc9dade)));

        // Dark theme variants: the same hues as above, toned down to dark tints so that light text stays readable.

        /// <summary>
        /// Same meaning as <see cref="Normal"/>: green verified, blue unverified, gray invalid,
        /// salmon unregistered, lavender Windows feature, cyan Store app.
        /// </summary>
        public static ApplicationListColors NormalDark = new(
            Color.FromArgb(0x19, 0x36, 0x2A), Color.FromArgb(0x1E, 0x2E, 0x45),
            Color.FromArgb(0x2E, 0x2E, 0x33), Color.FromArgb(0x42, 0x26, 0x24),
            Color.FromArgb(0x35, 0x28, 0x45), Color.FromArgb(0x14, 0x3A, 0x3E));

        /// <summary>
        /// Same meaning as <see cref="ColorBlind"/>: red verified, orange unverified, blue invalid,
        /// light blue unregistered, yellow Windows feature, gray-blue Store app.
        /// </summary>
        public static ApplicationListColors ColorBlindDark = new(
            Color.FromArgb(0x6A, 0x22, 0x1E), Color.FromArgb(0x6B, 0x41, 0x2C),
            Color.FromArgb(0x26, 0x3E, 0x62), Color.FromArgb(0x3B, 0x55, 0x66),
            Color.FromArgb(0x5E, 0x55, 0x2E), Color.FromArgb(0x4A, 0x52, 0x55));

        public ApplicationListColors(Color verifiedColor, Color unverifiedColor, Color invalidColor,
            Color unregisteredColor, Color windowsFeatureColor, Color windowsStoreAppColor)
        {
            VerifiedColor = verifiedColor;
            UnverifiedColor = unverifiedColor;
            InvalidColor = invalidColor;
            UnregisteredColor = unregisteredColor;
            WindowsFeatureColor = windowsFeatureColor;
            WindowsStoreAppColor = windowsStoreAppColor;
        }

        public Color VerifiedColor { get; }
        public Color UnverifiedColor { get; }
        public Color InvalidColor { get; }
        public Color UnregisteredColor { get; }
        public Color WindowsFeatureColor { get; }
        public Color WindowsStoreAppColor { get; }
    }
}
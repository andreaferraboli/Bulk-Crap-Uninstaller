/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Klocman.Extensions;

namespace UninstallTools.Controls
{
    public class UninstallerIconGetter : Component
    {
        private static readonly string ApplicationIconKey = "applicationIcon";
        private static readonly string InvalidIconKey = "invalidIcon";
        private static readonly string MsiexecIconKey = "msiexecIcon";
        private static readonly string UpdateIconKey = "updateIcon";
        // The name "OperatingSystem" is important
        private static readonly string WindowsIconKey = "OperatingSystem";

        public UninstallerIconGetter()
        {
            Disposed += DisposeHandler;
        }

        public ImageList IconList { get; private set; }

        /// <summary>
        ///     Size of the images in <see cref="IconList" />. Empty keeps the default image list size (16x16).
        ///     Takes effect on the next <see cref="UpdateIconList" />.
        /// </summary>
        public Size ImageSize { get; set; } = Size.Empty;

        /// <summary>
        ///     Where the icon is drawn inside each image when <see cref="ImageSize" /> is set.
        ///     The rest of the image is transparent (used for padding).
        /// </summary>
        public Rectangle IconBounds { get; set; } = Rectangle.Empty;

        public object ColumnImageGetter(object rowObj)
        {
            if (rowObj is not ApplicationUninstallerEntry entry || IconList == null)
                return null;

            if (IconListContainsKey(entry.DisplayName))
                return entry.DisplayName;

            if (entry.ParentKeyName.IsNotEmpty())
            {
                if (entry.ParentKeyName.Equals(WindowsIconKey))
                    return WindowsIconKey;

                if (IconListContainsKey(entry.ParentKeyName))
                    return entry.ParentKeyName;
            }

            if (entry.IsUpdate || entry.UninstallerKind == UninstallerType.WindowsFeature)
                return UpdateIconKey;

            if (entry.UninstallerKind == UninstallerType.Msiexec || entry.UninstallerKind == UninstallerType.SdbInst)
                return MsiexecIconKey;

            if (entry.IsValid)
                return ApplicationIconKey;

            return InvalidIconKey;
        }

        /// <exception cref="PlatformNotSupportedException">The current platform is not supported.</exception>
        public void UpdateIconList(IEnumerable<ApplicationUninstallerEntry> objList)
        {
            var imageSize = ImageSize;
            var iconBounds = IconBounds;
            var useLargeIcons = !imageSize.IsEmpty && !iconBounds.IsEmpty;

            IconList = new ImageList();
            if (useLargeIcons)
            {
                IconList.ColorDepth = ColorDepth.Depth32Bit;
                IconList.ImageSize = imageSize;
            }

            void AddIcon(string key, Icon icon)
            {
                if (useLargeIcons)
                {
                    Bitmap rendered = null;
                    try
                    {
                        rendered = RenderIcon(icon, imageSize, iconBounds);
                    }
                    catch (Exception ex) when (ex is ArgumentException or ExternalException or InvalidOperationException)
                    {
                        // Fall back to letting the image list scale the icon
                    }

                    if (rendered != null)
                    {
                        IconList.Images.Add(key, rendered);
                        return;
                    }
                }

                IconList.Images.Add(key, icon);
            }

            var windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            AddIcon(ApplicationIconKey, SystemIcons.Application);
            AddIcon(InvalidIconKey, SystemIcons.Exclamation);
            AddIcon(WindowsIconKey, SystemIcons.Shield); //SystemIcons.WinLogo); winlogo not working on xp?
            AddIcon(UpdateIconKey, SystemIcons.Shield);
            var msiIcon = UninstallToolsGlobalConfig.TryExtractAssociatedIcon(windowsPath + @"\msiexec.exe");
            AddIcon(MsiexecIconKey, msiIcon ?? SystemIcons.Application);

            foreach (var obj in objList)
            {
                if (IconListContainsKey(obj.DisplayName))
                    continue;

                try
                {
                    var image = obj.GetIcon();
                    if (image != null)
                    {
                        AddIcon(obj.DisplayName, image);
                    }
                }
                catch
                {
                    // Revert to default icon
                }
            }
        }

        /// <summary>
        ///     Draw the icon into a transparent bitmap of the given size, scaled into iconBounds with high quality.
        ///     The icon frame closest to the target size is used when the icon has several.
        /// </summary>
        private static Bitmap RenderIcon(Icon icon, Size imageSize, Rectangle iconBounds)
        {
            using var sized = new Icon(icon, iconBounds.Size);
            using var source = sized.ToBitmap();
            var result = new Bitmap(imageSize.Width, imageSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(result);
            g.Clear(Color.Transparent);
            if (source.Size == iconBounds.Size)
            {
                g.DrawImageUnscaled(source, iconBounds.Location);
            }
            else
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(source, iconBounds);
            }
            return result;
        }

        private void DisposeHandler(object x, EventArgs y)
        {
            if (IconList != null)
            {
                IconList.Dispose();
                IconList = null;
            }
        }

        private bool IconListContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (IconList == null)
                throw new InvalidOperationException("IconListContainsKey called when IconList is null");

            return IconList.Images.Keys.Cast<string>().Any(x => x.Equals(key));
        }
    }
}
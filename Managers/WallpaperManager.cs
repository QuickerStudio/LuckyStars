using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LuckyStars.Managers
{
    public class WallpaperManager
    {
        // 保存原始壁纸设置的变量
        private string originalWallpaperPath = string.Empty;
        private int originalWallpaperStyle;
        private bool originalSlideshowEnabled;
        private string originalSlideshowPath = string.Empty;
        private int originalSlideshowInterval;

        // 注册表路径
        private const string DesktopRegistryPath = @"Control Panel\Desktop";
        private const string SlideshowRegistryPath = @"Control Panel\Personalization\Desktop Slideshow";

        // 注册表键名
        private const string WallpaperKey = "Wallpaper";
        private const string WallpaperStyleKey = "WallpaperStyle";
        private const string SlideshowEnabledKey = "SlideshowEnabled";
        private const string SlideshowDirectoryKey = "SlideshowDirectory";
        private const string SlideshowIntervalKey = "SlideshowInterval";

        // Win32 API 用于刷新桌面
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string? lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        /// <summary>
        /// 禁用系统壁纸幻灯片放映
        /// </summary>
        public void DisableSystemWallpaperSlideshow()
        {
            // 保存原始壁纸设置
            SaveOriginalWallpaperSettings();

            // 禁用幻灯片放映
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SlideshowRegistryPath, true))
            {
                if (key != null)
                {
                    key.SetValue(SlideshowEnabledKey, 0, RegistryValueKind.DWord);
                }
            }

            // 刷新桌面设置
            RefreshDesktop();
        }

        /// <summary>
        /// 恢复系统壁纸幻灯片放映
        /// </summary>
        public void RestoreSystemWallpaperSlideshow()
        {
            // 恢复幻灯片放映设置
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SlideshowRegistryPath, true))
            {
                if (key != null && originalSlideshowEnabled)
                {
                    key.SetValue(SlideshowEnabledKey, 1, RegistryValueKind.DWord);

                    if (!string.IsNullOrEmpty(originalSlideshowPath))
                    {
                        key.SetValue(SlideshowDirectoryKey, originalSlideshowPath, RegistryValueKind.String);
                    }

                    key.SetValue(SlideshowIntervalKey, originalSlideshowInterval, RegistryValueKind.DWord);
                }
            }

            // 恢复壁纸设置
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DesktopRegistryPath, true))
            {
                if (key != null)
                {
                    if (!string.IsNullOrEmpty(originalWallpaperPath))
                    {
                        key.SetValue(WallpaperKey, originalWallpaperPath, RegistryValueKind.String);
                    }

                    key.SetValue(WallpaperStyleKey, originalWallpaperStyle, RegistryValueKind.String);
                }
            }

            // 刷新桌面设置 - 直接使用原始壁纸路径确保壁纸被正确设置
            if (!string.IsNullOrEmpty(originalWallpaperPath) && File.Exists(originalWallpaperPath))
            {
                // 使用原始壁纸路径直接设置壁纸
                _ = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, originalWallpaperPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            else
            {
                // 如果没有原始壁纸路径或文件不存在，使用普通刷新
                RefreshDesktop();
            }

            // 壁纸已恢复，不显示通知
        }



        /// <summary>
        /// 保存原始壁纸设置
        /// </summary>
        private void SaveOriginalWallpaperSettings()
        {
            // 保存桌面壁纸设置
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DesktopRegistryPath, false))
            {
                if (key != null)
                {
                    originalWallpaperPath = key.GetValue(WallpaperKey) as string ?? string.Empty;

                    var styleValue = key.GetValue(WallpaperStyleKey);
                    originalWallpaperStyle = styleValue != null ? Convert.ToInt32(styleValue) : 0;
                }
            }

            // 保存幻灯片放映设置
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SlideshowRegistryPath, false))
            {
                if (key != null)
                {
                    var enabledValue = key.GetValue(SlideshowEnabledKey);
                    originalSlideshowEnabled = enabledValue != null && Convert.ToInt32(enabledValue) == 1;

                    originalSlideshowPath = key.GetValue(SlideshowDirectoryKey) as string ?? string.Empty;

                    var intervalValue = key.GetValue(SlideshowIntervalKey);
                    originalSlideshowInterval = intervalValue != null ? Convert.ToInt32(intervalValue) : 1800; // 默认30分钟
                }
            }
        }

        /// <summary>
        /// 刷新桌面设置
        /// </summary>
        private static void RefreshDesktop()
        {
 
            _ = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, null, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
    }
}

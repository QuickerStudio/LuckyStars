using System;
using System.IO;

namespace LuckyStars.Core
{
    /// <summary>
    /// 应用程序常量定义
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// 应用程序名称
        /// </summary>
        public const string AppName = "LuckyStars";
        
        /// <summary>
        /// 应用程序版本
        /// </summary>
        public const string AppVersion = "1.0.0";
        
        /// <summary>
        /// 应用程序官方网站
        /// </summary>
        public const string AppWebsite = "https://github.com/QuickerStudio/LuckyStars";
        
        /// <summary>
        /// FFmpeg相对路径
        /// </summary>
        public const string FFmpegRelativePath = "FFmpeg\\bin\\ffmpeg.exe";
        
        /// <summary>
        /// FFprobe相对路径
        /// </summary>
        public const string FFprobeRelativePath = "FFmpeg\\bin\\ffprobe.exe";
        
        /// <summary>
        /// 图片壁纸目录名
        /// </summary>
        public const string PictureWallpaperDir = "picture";
        
        /// <summary>
        /// HTML壁纸目录名
        /// </summary>
        public const string HtmlWallpaperDir = "html";
        
        /// <summary>
        /// 视频壁纸目录名
        /// </summary>
        public const string VideoWallpaperDir = "video";
        
        /// <summary>
        /// 缩略图目录名
        /// </summary>
        public const string ThumbnailDir = "thumbnails";
        
        /// <summary>
        /// 临时文件目录名
        /// </summary>
        public const string TempDir = "Temp";
        
        /// <summary>
        /// 日志目录名
        /// </summary>
        public const string LogDir = "Logs";
        
        /// <summary>
        /// 壁纸配置文件名
        /// </summary>
        public const string WallpaperConfigFile = "wallpaper_config.json";
        
        /// <summary>
        /// 应用程序设置文件名
        /// </summary>
        public const string AppSettingsFile = "app_settings.json";
        
        /// <summary>
        /// 默认幻灯片间隔(分钟)
        /// </summary>
        public const int DefaultSlideshowInterval = 5;
        
        /// <summary>
        /// 默认缩略图宽度
        /// </summary>
        public const int DefaultThumbnailWidth = 192;
        
        /// <summary>
        /// 默认缩略图高度
        /// </summary>
        public const int DefaultThumbnailHeight = 108;
        
        /// <summary>
        /// CPU使用率检查间隔(毫秒)
        /// </summary>
        public const int CpuUsageCheckInterval = 3000;
        
        /// <summary>
        /// 默认CPU使用阈值(%)
        /// </summary>
        public const int DefaultCpuThreshold = 85;
        
        /// <summary>
        /// 最大备份版本数
        /// </summary>
        public const int MaxBackupVersions = 3;
        
        /// <summary>
        /// 日志保留天数
        /// </summary>
        public const int LogRetentionDays = 7;
        
        /// <summary>
        /// 获取应用程序数据目录
        /// </summary>
        /// <returns>应用程序数据目录路径</returns>
        public static string GetAppDataDirectory()
        {
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                AppName);
                
            Directory.CreateDirectory(appDataDir);
            return appDataDir;
        }
        
        /// <summary>
        /// 获取壁纸存储目录
        /// </summary>
        /// <returns>壁纸存储目录路径</returns>
        public static string GetWallpaperDirectory()
        {
            string wallpaperDir = Path.Combine(GetAppDataDirectory(), "Wallpapers");
            Directory.CreateDirectory(wallpaperDir);
            return wallpaperDir;
        }
    }
}
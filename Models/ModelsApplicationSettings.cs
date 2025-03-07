using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using LuckyStars.Core;

namespace LuckyStars.Models
{
    /// <summary>
    /// 应用设置模型，存储和管理应用程序配置
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// 设置版本号
        /// </summary>
        public int SettingsVersion { get; set; } = 1;
        
        /// <summary>
        /// 应用程序启动时自动运行
        /// </summary>
        public bool RunAtStartup { get; set; } = false;
        
        /// <summary>
        /// 程序启动时自动应用上次的壁纸
        /// </summary>
        public bool ApplyLastWallpaper { get; set; } = true;
        
        /// <summary>
        /// 上次使用的壁纸路径
        /// </summary>
        public string LastWallpaperPath { get; set; }
        
        /// <summary>
        /// 性能设置
        /// </summary>
        public WallpaperPerformanceManager.PerformanceSettings Performance { get; set; } = new WallpaperPerformanceManager.PerformanceSettings();
        
        /// <summary>
        /// 幻灯片播放设置
        /// </summary>
        public SlideshowManager.SlideshowSettings Slideshow { get; set; } = new SlideshowManager.SlideshowSettings();
        
        /// <summary>
        /// 声音设置
        /// </summary>
        public SoundSettings Sound { get; set; } = new SoundSettings();
        
        /// <summary>
        /// 界面设置
        /// </summary>
        public UISettings UI { get; set; } = new UISettings();
        
        /// <summary>
        /// 壁纸设置
        /// </summary>
        public WallpaperSettings Wallpaper { get; set; } = new WallpaperSettings();
        
        /// <summary>
        /// 网络设置
        /// </summary>
        public NetworkSettings Network { get; set; } = new NetworkSettings();
        
        /// <summary>
        /// 记住的壁纸历史记录（最近使用的壁纸）
        /// </summary>
        public List<string> RecentWallpapers { get; set; } = new List<string>();
        
        /// <summary>
        /// 收藏的壁纸列表
        /// </summary>
        public List<string> FavoriteWallpapers { get; set; } = new List<string>();
        
        /// <summary>
        /// 显示器特定的壁纸设置
        /// </summary>
        public Dictionary<string, MonitorWallpaperSettings> MonitorSettings { get; set; } = new Dictionary<string, MonitorWallpaperSettings>();
        
        /// <summary>
        /// 上次检查更新的时间
        /// </summary>
        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;
        
        /// <summary>
        /// 是否显示系统托盘通知
        /// </summary>
        public bool ShowTrayNotifications { get; set; } = true;
        
        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel LoggingLevel { get; set; } = LogLevel.Normal;
        
        /// <summary>
        /// 日志级别枚举
        /// </summary>
        public enum LogLevel
        {
            /// <summary>
            /// 最小日志（仅错误）
            /// </summary>
            Minimal,
            
            /// <summary>
            /// 正常日志（错误和警告）
            /// </summary>
            Normal,
            
            /// <summary>
            /// 详细日志（所有信息）
            /// </summary>
            Verbose,
            
            /// <summary>
            /// 调试日志（开发用）
            /// </summary>
            Debug
        }
        
        /// <summary>
        /// 声音设置类
        /// </summary>
        public class SoundSettings
        {
            /// <summary>
            /// 启用声音
            /// </summary>
            public bool EnableSound { get; set; } = true;
            
            /// <summary>
            /// 主音量（0-100）
            /// </summary>
            public int Volume { get; set; } = 80;
            
            /// <summary>
            /// 壁纸切换时播放声音
            /// </summary>
            public bool PlaySoundOnChange { get; set; } = false;
            
            /// <summary>
            /// 启动时播放声音
            /// </summary>
            public bool PlaySoundOnStartup { get; set; } = false;
        }
        
        /// <summary>
        /// 界面设置类
        /// </summary>
        public class UISettings
        {
            /// <summary>
            /// 缩略图大小
            /// </summary>
            public int ThumbnailSize { get; set; } = 160;
            
            /// <summary>
            /// 界面语言
            /// </summary>
            public string Language { get; set; } = "zh-CN";
            
            /// <summary>
            /// 界面主题（浅色/深色）
            /// </summary>
            public string Theme { get; set; } = "Auto";
            
            /// <summary>
            /// 托盘图标主题
            /// </summary>
            public string TrayIconTheme { get; set; } = "Default";
            
            /// <summary>
            /// 显示文件拖放提示
            /// </summary>
            public bool ShowDragDropHint { get; set; } = true;
            
            /// <summary>
            /// 壁纸排序方式
            /// </summary>
            public SortMode WallpaperSortMode { get; set; } = SortMode.DateAdded;
            
            /// <summary>
            /// 排序方式枚举
            /// </summary>
            public enum SortMode
            {
                /// <summary>
                /// 按名称排序
                /// </summary>
                Name,
                
                /// <summary>
                /// 按添加日期排序
                /// </summary>
                DateAdded,
                
                /// <summary>
                /// 按上次使用时间排序
                /// </summary>
                LastUsed,
                
                /// <summary>
                /// 按使用频率排序
                /// </summary>
                UsageCount,
                
                /// <summary>
                /// 按类型排序
                /// </summary>
                Type
            }
        }
        
        /// <summary>
        /// 壁纸设置类
        /// </summary>
        public class WallpaperSettings
        {
            /// <summary>
            /// 默认适应模式
            /// </summary>
            public MultiMonitorWallpaperManager.WallpaperScaleMode DefaultScaleMode { get; set; } = MultiMonitorWallpaperManager.WallpaperScaleMode.Fill;
            
            /// <summary>
            /// 允许视频壁纸
            /// </summary>
            public bool AllowVideoWallpaper { get; set; } = true;
            
            /// <summary>
            /// 允许HTML壁纸
            /// </summary>
            public bool AllowWebWallpaper { get; set; } = true;
            
            /// <summary>
            /// 视频壁纸静音
            /// </summary>
            public bool MuteVideoWallpaper { get; set; } = true;
            
            /// <summary>
            /// 视频壁纸循环播放
            /// </summary>
            public bool LoopVideoWallpaper { get; set; } = true;
            
            /// <summary>
            /// HTML壁纸允许JavaScript
            /// </summary>
            public bool AllowJavaScript { get; set; } = true;
            
            /// <summary>
            /// HTML壁纸允许访问本地资源
            /// </summary>
            public bool AllowLocalAccess { get; set; } = false;
        }
        
        /// <summary>
        /// 网络设置类
        /// </summary>
        public class NetworkSettings
        {
            /// <summary>
            /// 启用代理
            /// </summary>
            public bool EnableProxy { get; set; } = false;
            
            /// <summary>
            /// 代理主机
            /// </summary>
            public string ProxyHost { get; set; }
            
            /// <summary>
            /// 代理端口
            /// </summary>
            public int ProxyPort { get; set; } = 0;
            
            /// <summary>
            /// 启用自动更新检查
            /// </summary>
            public bool EnableUpdateCheck { get; set; } = true;
            
            /// <summary>
            /// 更新检查间隔（天）
            /// </summary>
            public int UpdateCheckInterval { get; set; } = 7;
        }
        
        /// <summary>
        /// 监视器壁纸设置类
        /// </summary>
        public class MonitorWallpaperSettings
        {
            /// <summary>
            /// 显示器名称
            /// </summary>
            public string MonitorName { get; set; }
            
            /// <summary>
            /// 显示器标识符
            /// </summary>
            public string MonitorId { get; set; }
            
            /// <summary>
            /// 壁纸路径
            /// </summary>
            public string WallpaperPath { get; set; }
            
            /// <summary>
            /// 缩放模式
            /// </summary>
            public MultiMonitorWallpaperManager.WallpaperScaleMode ScaleMode { get; set; } = MultiMonitorWallpaperManager.WallpaperScaleMode.Fill;
        }
        
        /// <summary>
        /// 将应用设置保存到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否保存成功</returns>
        public bool SaveToFile(string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存应用设置失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从文件加载应用设置
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>加载的应用设置，失败返回null</returns>
        public static ApplicationSettings LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new ApplicationSettings();
                    
                string json = File.ReadAllText(filePath);
                var settings = JsonConvert.DeserializeObject<ApplicationSettings>(json);
                
                // 确保所有集合初始化
                settings.RecentWallpapers ??= new List<string>();
                settings.FavoriteWallpapers ??= new List<string>();
                settings.MonitorSettings ??= new Dictionary<string, MonitorWallpaperSettings>();
                
                return settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载应用设置失败: {ex.Message}");
                return new ApplicationSettings();
            }
        }
        
        /// <summary>
        /// 添加最近使用的壁纸
        /// </summary>
        /// <param name="wallpaperPath">壁纸路径</param>
        public void AddRecentWallpaper(string wallpaperPath)
        {
            if (string.IsNullOrEmpty(wallpaperPath))
                return;
                
            // 从列表中移除，如果已存在
            RecentWallpapers.Remove(wallpaperPath);
            
            // 添加到列表开头
            RecentWallpapers.Insert(0, wallpaperPath);
            
            // 限制列表大小
            if (RecentWallpapers.Count > Constants.Settings.MaxHistoryItems)
            {
                RecentWallpapers = RecentWallpapers.Take(Constants.Settings.MaxHistoryItems).ToList();
            }
        }
        
        /// <summary>
        /// 添加或删除收藏壁纸
        /// </summary>
        /// <param name="wallpaperPath">壁纸路径</param>
        /// <param name="isFavorite">是否为收藏</param>
        public void SetFavoriteWallpaper(string wallpaperPath, bool isFavorite)
        {
            if (string.IsNullOrEmpty(wallpaperPath))
                return;
                
            if (isFavorite && !FavoriteWallpapers.Contains(wallpaperPath))
            {
                FavoriteWallpapers.Add(wallpaperPath);
            }
            else if (!isFavorite)
            {
                FavoriteWallpapers.Remove(wallpaperPath);
            }
        }
        
        /// <summary>
        /// 检查壁纸是否为收藏
        /// </summary>
        /// <param name="wallpaperPath">壁纸路径</param>
        /// <returns>是否为收藏</returns>
        public bool IsWallpaperFavorite(string wallpaperPath)
        {
            return !string.IsNullOrEmpty(wallpaperPath) && FavoriteWallpapers.Contains(wallpaperPath);
        }
        
        /// <summary>
        /// 保存监视器特定的壁纸设置
        /// </summary>
        /// <param name="monitorId">监视器ID</param>
        /// <param name="monitorName">监视器名称</param>
        /// <param name="wallpaperPath">壁纸路径</param>
        /// <param name="scaleMode">缩放模式</param>
        public void SaveMonitorWallpaperSetting(string monitorId, string monitorName, string wallpaperPath, MultiMonitorWallpaperManager.WallpaperScaleMode scaleMode)
        {
            if (string.IsNullOrEmpty(monitorId))
                return;
                
            if (!MonitorSettings.ContainsKey(monitorId))
            {
                MonitorSettings[monitorId] = new MonitorWallpaperSettings
                {
                    MonitorId = monitorId,
                    MonitorName = monitorName
                };
            }
            
            MonitorSettings[monitorId].WallpaperPath = wallpaperPath;
            MonitorSettings[monitorId].ScaleMode = scaleMode;
        }
        
        /// <summary>
        /// 获取监视器的壁纸路径
        /// </summary>
        /// <param name="monitorId">监视器ID</param>
        /// <returns>壁纸路径，不存在则返回null</returns>
        public string GetMonitorWallpaperPath(string monitorId)
        {
            if (string.IsNullOrEmpty(monitorId) || !MonitorSettings.ContainsKey(monitorId))
                return null;
                
            return MonitorSettings[monitorId].WallpaperPath;
        }
    }
}
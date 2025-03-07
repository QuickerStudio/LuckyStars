using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using LuckyStars.Utils;

namespace LuckyStars.Core
{
    /// <summary>
    /// 壁纸管理核心类，负责壁纸的加载、保存和切换
    /// </summary>
    public class WallpaperManager : IDisposable
    {
        // 事件: 壁纸变更
        public event EventHandler<string> WallpaperChanged;
        
        // 事件: 暂停状态变更
        public event EventHandler<bool> PauseStateChanged;
        
        // 壁纸存储目录
        private readonly string _wallpaperDirectory;
        
        // 图片壁纸目录
        private readonly string _pictureDirectory;
        
        // HTML壁纸目录
        private readonly string _htmlDirectory;
        
        // 多显示器管理器
        private readonly MultiMonitorWallpaperManager _monitorManager;
        
        // FFmpeg管理器
        private readonly FFmpegManager _ffmpegManager;
        
        // 性能管理器
        private readonly WallpaperPerformanceManager _performanceManager;
        
        // 当前壁纸路径
        private string _currentWallpaperPath;
        
        // 是否暂停
        private bool _isPaused = false;
        
        // 格式转换器
        private EnhancedFormatConverter _formatConverter;
        
        /// <summary>
        /// 获取是否暂停
        /// </summary>
        public bool IsPaused => _isPaused;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="wallpaperDirectory">壁纸存储目录</param>
        /// <param name="monitorManager">多显示器管理器</param>
        /// <param name="ffmpegManager">FFmpeg管理器</param>
        /// <param name="performanceManager">性能管理器</param>
        public WallpaperManager(
            string wallpaperDirectory,
            MultiMonitorWallpaperManager monitorManager,
            FFmpegManager ffmpegManager,
            WallpaperPerformanceManager performanceManager)
        {
            _wallpaperDirectory = wallpaperDirectory;
            _pictureDirectory = Path.Combine(wallpaperDirectory, "picture");
            _htmlDirectory = Path.Combine(wallpaperDirectory, "html");
            
            _monitorManager = monitorManager;
            _ffmpegManager = ffmpegManager;
            _performanceManager = performanceManager;
            
            // 创建格式转换器
            _formatConverter = new EnhancedFormatConverter(_ffmpegManager);
            
            // 确保目录存在
            Directory.CreateDirectory(_pictureDirectory);
            Directory.CreateDirectory(_htmlDirectory);
        }
        
        /// <summary>
        /// 从文件设置壁纸
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SetWallpaperFromFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"文件不存在: {filePath}");
                return false;
            }
            
            try
            {
                // 检测文件类型
                var fileType = await FileTypeDetector.DetectFileTypeAsync(filePath);
                
                string destinationPath = null;
                
                switch (fileType)
                {
                    case FileTypeDetector.FileType.Image:
                        // 复制图片文件
                        destinationPath = await CopyFileToWallpaperDirectoryAsync(filePath, _pictureDirectory);
                        break;
                        
                    case FileTypeDetector.FileType.HTML:
                        // 复制HTML文件
                        destinationPath = await CopyFileToWallpaperDirectoryAsync(filePath, _htmlDirectory);
                        break;
                        
                    case FileTypeDetector.FileType.Video:
                        // 转换视频到图片
                        destinationPath = await _formatConverter.ConvertVideoToImageAsync(filePath, _pictureDirectory);
                        break;
                        
                    case FileTypeDetector.FileType.Document:
                        // 转换文档到图片
                        destinationPath = await _formatConverter.ConvertDocumentToImageAsync(filePath, _pictureDirectory);
                        break;
                        
                    default:
                        System.Diagnostics.Debug.WriteLine($"不支持的文件类型: {filePath}");
                        return false;
                }
                
                if (string.IsNullOrEmpty(destinationPath))
                {
                    System.Diagnostics.Debug.WriteLine("文件处理失败，未获取到目标路径");
                    return false;
                }
                
                // 设置壁纸
                SetWallpaper(destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置壁纸时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从文件设置壁纸（同步版本）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        public bool SetWallpaperFromFile(string filePath)
        {
            try
            {
                return SetWallpaperFromFileAsync(filePath).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置壁纸时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 设置壁纸
        /// </summary>
        /// <param name="wallpaperPath">壁纸路径</param>
        public void SetWallpaper(string wallpaperPath)
        {
            if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
            {
                System.Diagnostics.Debug.WriteLine($"壁纸文件不存在: {wallpaperPath}");
                return;
            }
            
            try
            {
                // 更新当前壁纸路径
                _currentWallpaperPath = wallpaperPath;
                
                // 触发壁纸变更事件
                WallpaperChanged?.Invoke(this, wallpaperPath);
                
                // 如果当前已暂停，则更新暂停状态
                if (_isPaused)
                {
                    PauseStateChanged?.Invoke(this, _isPaused);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置壁纸时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清除当前壁纸
        /// </summary>
        public void ClearWallpaper()
        {
            try
            {
                // 更新当前壁纸路径
                _currentWallpaperPath = null;
                
                // 触发壁纸变更事件
                WallpaperChanged?.Invoke(this, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清除壁纸时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置暂停状态
        /// </summary>
        /// <param name="isPaused">是否暂停</param>
        /// <param name="reason">暂停原因</param>
        public void SetPaused(bool isPaused, string reason = "用户操作")
        {
            if (_isPaused != isPaused)
            {
                _isPaused = isPaused;
                System.Diagnostics.Debug.WriteLine($"壁纸{(isPaused ? "暂停" : "继续")}，原因: {reason}");
                
                // 触发暂停状态变更事件
                PauseStateChanged?.Invoke(this, isPaused);
            }
        }
        
        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            SetPaused(!_isPaused, "用户切换");
        }
        
        /// <summary>
        /// 复制文件到壁纸目录
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destinationDirectory">目标目录</param>
        /// <returns>目标文件路径</returns>
        private async Task<string> CopyFileToWallpaperDirectoryAsync(string sourceFilePath, string destinationDirectory)
        {
            try
            {
                // 生成唯一文件名
                string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                string extension = Path.GetExtension(sourceFilePath);
                string uniqueName = $"{fileName}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
                string destinationPath = Path.Combine(destinationDirectory, uniqueName);
                
                // 复制文件
                await Task.Run(() => File.Copy(sourceFilePath, destinationPath, true));
                
                return destinationPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"复制文件时出错: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取当前壁纸路径
        /// </summary>
        /// <returns>当前壁纸路径</returns>
        public string GetCurrentWallpaperPath()
        {
            return _currentWallpaperPath;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 清理壁纸
                ClearWallpaper();
                
                // 释放格式转换器
                _formatConverter?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"释放壁纸管理器资源时出错: {ex.Message}");
            }
        }
    }
}
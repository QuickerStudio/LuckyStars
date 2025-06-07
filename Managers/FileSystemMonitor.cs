using System;
using System.IO;
using LuckyStars.Utils;

namespace LuckyStars.Managers
{
    /// <summary>
    /// 文件系统监控器，用于监控目标文件夹中的文件变化
    /// 支持多格式文件检索和子目录监控
    /// </summary>
    public class FileSystemMonitor : IDisposable
    {
        private FileSystemWatcher? folderWatcher;
        private readonly string targetFolder;
        private readonly Action resetIdleTimer;
        private readonly Action loadMediaPaths;

        // 添加定时器，定期检查根目录是否存在
        private System.Timers.Timer? _directoryCheckTimer;

        public FileSystemMonitor(string targetFolder, Action resetIdleTimer, Action loadMediaPaths)
        {
            this.targetFolder = targetFolder;
            this.resetIdleTimer = resetIdleTimer;
            this.loadMediaPaths = loadMediaPaths;

            // 初始化定时器，每分钟检查一次根目录是否存在
            _directoryCheckTimer = new System.Timers.Timer(60000); // 60秒
            _directoryCheckTimer.Elapsed += (sender, e) => EnsureRootDirectoryExists();
            _directoryCheckTimer.AutoReset = true;
            _directoryCheckTimer.Enabled = true;

            // 立即检查并设置文件监视器
            SetupFolderWatcher();
        }

        /// <summary>
        /// 设置文件夹监视器，监控文件变化
        /// </summary>
        public void SetupFolderWatcher()
        {
            try
            {
                // 确保根目录存在
                EnsureRootDirectoryExists();

                // 如果已经存在监视器，先释放资源
                if (folderWatcher != null)
                {
                    folderWatcher.EnableRaisingEvents = false;
                    folderWatcher.Created -= OnFolderChanged;
                    folderWatcher.Deleted -= OnFolderChanged;
                    folderWatcher.Renamed -= OnFolderRenamed;
                    folderWatcher.Dispose();
                }

                folderWatcher = new FileSystemWatcher(targetFolder)
                {
                    // 使用通配符监控所有文件
                    Filter = "*.*",
                    // 设置通知过滤器，监控文件名、目录名、最后写入时间、创建时间和大小变化
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
                    // 启用事件触发
                    EnableRaisingEvents = true,
                    // 启用子目录监控
                    IncludeSubdirectories = true
                };

                // 注册事件处理程序
                folderWatcher.Created += OnFolderChanged;
                folderWatcher.Deleted += OnFolderChanged;
                folderWatcher.Renamed += OnFolderRenamed;
                folderWatcher.Changed += OnFolderChanged;

                // 立即加载媒体路径，确保初始状态正确
                loadMediaPaths();

                Console.WriteLine($"文件系统监控器已设置，监控目录: {targetFolder}，包含子目录");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置文件夹监视器失败: {ex.Message}");
                folderWatcher = null;
            }
        }

        private void OnFolderChanged(object sender, FileSystemEventArgs e)
        {
            // 使用SupportedFormats统一接口检查文件是否为支持的格式
            if (SupportedFormats.IsSupportedFile(e.FullPath))
            {
                resetIdleTimer();
                // 立即更新播放目录索引
                loadMediaPaths();
            }
        }

        private void OnFolderRenamed(object sender, RenamedEventArgs e)
        {
            // 使用SupportedFormats统一接口检查文件是否为支持的格式
            if (SupportedFormats.IsSupportedFile(e.FullPath))
            {
                resetIdleTimer();
                // 立即更新播放目录索引
                loadMediaPaths();
            }
        }

        /// <summary>
        /// 确保根目录存在，如果不存在则创建
        /// </summary>
        private void EnsureRootDirectoryExists()
        {
            if (!Directory.Exists(targetFolder))
            {
                try
                {
                    // 创建目录，包括所有必需但不存在的父目录
                    Directory.CreateDirectory(targetFolder);
                    Console.WriteLine($"已创建根目录: {targetFolder}");

                    // 目录创建成功后，立即重新设置文件监视器
                    if (folderWatcher == null)
                    {
                        Console.WriteLine("根目录创建成功，正在重新设置文件监视器...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"创建根目录失败: {ex.Message}");
                    // 记录更详细的异常信息
                    Console.WriteLine($"异常详情: {ex}");
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 释放文件监视器资源
            if (folderWatcher != null)
            {
                try
                {
                    folderWatcher.EnableRaisingEvents = false;
                    folderWatcher.Created -= OnFolderChanged;
                    folderWatcher.Deleted -= OnFolderChanged;
                    folderWatcher.Renamed -= OnFolderRenamed;
                    folderWatcher.Changed -= OnFolderChanged;
                    folderWatcher.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"释放文件监视器资源时出错: {ex.Message}");
                }
                finally
                {
                    folderWatcher = null;
                }
            }

            // 释放定时器资源
            if (_directoryCheckTimer != null)
            {
                try
                {
                    _directoryCheckTimer.Enabled = false;
                    _directoryCheckTimer.Elapsed -= (sender, e) => EnsureRootDirectoryExists();
                    _directoryCheckTimer.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"释放定时器资源时出错: {ex.Message}");
                }
                finally
                {
                    _directoryCheckTimer = null;
                }
            }

            // 防止派生类需要重新实现 IDisposable
            GC.SuppressFinalize(this);

            Console.WriteLine("文件系统监控器已释放资源");
        }
    }
}

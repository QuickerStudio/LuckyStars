using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using LuckyStars.Core;
using LuckyStars.Utils;
using LuckyStars.UI;

namespace LuckyStars
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        // 应用程序实例
        public static App Instance => (App)Current;
        
        // 应用程序数据目录
        public string AppDataDirectory { get; private set; }
        
        // 壁纸存储目录
        public string WallpaperDirectory { get; private set; }
        
        // FFmpeg管理器
        public FFmpegManager FFmpegManager { get; private set; }
        
        // 壁纸管理器
        public WallpaperManager WallpaperManager { get; private set; }
        
        // 系统托盘管理器
        public NotifyIconManager NotifyIconManager { get; private set; }
        
        // 性能管理器
        public WallpaperPerformanceManager PerformanceManager { get; private set; }
        
        // 多显示器管理器
        public MultiMonitorWallpaperManager MonitorManager { get; private set; }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化异常处理
            InitializeExceptionHandling();
            
            // 初始化应用程序目录
            InitializeDirectories();
            
            // 初始化核心管理器
            InitializeManagers();
            
            // 初始化系统托盘
            InitializeNotifyIcon();
            
            // 启动后台服务
            StartBackgroundServices();
        }
        
        /// <summary>
        /// 初始化异常处理
        /// </summary>
        private void InitializeExceptionHandling()
        {
            // UI线程未捕获异常处理
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            // 非UI线程未捕获异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            // Task未捕获异常处理
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }
        
        /// <summary>
        /// 初始化应用程序目录
        /// </summary>
        private void InitializeDirectories()
        {
            // 应用数据根目录
            AppDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LuckyStarsWallpaper");
                
            // 壁纸存储目录
            WallpaperDirectory = Path.Combine(AppDataDirectory, "Wallpapers");
            
            // 确保目录存在
            Directory.CreateDirectory(AppDataDirectory);
            Directory.CreateDirectory(WallpaperDirectory);
            Directory.CreateDirectory(Path.Combine(WallpaperDirectory, "picture"));
            Directory.CreateDirectory(Path.Combine(WallpaperDirectory, "html"));
            Directory.CreateDirectory(Path.Combine(AppDataDirectory, "Logs"));
            Directory.CreateDirectory(Path.Combine(AppDataDirectory, "Temp"));
        }
        
        /// <summary>
        /// 初始化核心管理器
        /// </summary>
        private void InitializeManagers()
        {
            try
            {
                // 初始化FFmpeg管理器
                FFmpegManager = new FFmpegManager(AppDataDirectory);
                
                // 检查FFmpeg是否需要回退到备份版本
                FFmpegManager.CheckForRollback();
                
                // 初始化多显示器管理器
                MonitorManager = new MultiMonitorWallpaperManager();
                
                // 初始化性能管理器
                var performanceSettings = new WallpaperPerformanceManager.PerformanceSettings
                {
                    PauseOnBattery = false,
                    PauseOnFullscreen = true,
                    PauseOnHighCpu = true,
                    CpuThreshold = 85,
                    Mode = WallpaperPerformanceManager.PerformanceMode.Balanced
                };
                
                PerformanceManager = new WallpaperPerformanceManager(performanceSettings);
                PerformanceManager.WallpaperPauseStateChanged += PerformanceManager_WallpaperPauseStateChanged;
                
                // 初始化壁纸管理器
                WallpaperManager = new WallpaperManager(
                    WallpaperDirectory,
                    MonitorManager,
                    FFmpegManager,
                    PerformanceManager);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化核心管理器时出错: {ex.Message}", "初始化错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 初始化系统托盘
        /// </summary>
        private void InitializeNotifyIcon()
        {
            try
            {
                NotifyIconManager = new NotifyIconManager();
                NotifyIconManager.Initialize();
                
                // 注册托盘事件
                NotifyIconManager.ExitRequested += NotifyIconManager_ExitRequested;
                NotifyIconManager.TogglePlayPauseRequested += NotifyIconManager_TogglePlayPauseRequested;
                NotifyIconManager.FileDropped += NotifyIconManager_FileDropped;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化系统托盘时出错: {ex.Message}", "初始化错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 启动后台服务
        /// </summary>
        private void StartBackgroundServices()
        {
            try
            {
                // 检查FFmpeg更新
                FFmpegManager.CheckForUpdatesAsync();
                
                // 初始化多显示器检测
                MonitorManager.Initialize();
                
                // 启动性能监控
                PerformanceManager.StartMonitoring();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动后台服务时出错: {ex.Message}", "初始化错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 性能管理器壁纸暂停状态变更
        /// </summary>
        private void PerformanceManager_WallpaperPauseStateChanged(object sender, WallpaperPerformanceManager.PauseReason reason)
        {
            try
            {
                bool shouldPause = (reason != WallpaperPerformanceManager.PauseReason.None);
                
                WallpaperManager.SetPaused(shouldPause, reason.ToString());
                
                // 更新托盘图标状态
                NotifyIconManager?.UpdatePauseState(shouldPause);
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                System.Diagnostics.Debug.WriteLine($"处理暂停状态变更时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 托盘文件拖放处理
        /// </summary>
        private void NotifyIconManager_FileDropped(object sender, string[] files)
        {
            if (files == null || files.Length == 0)
                return;
                
            WallpaperManager.SetWallpaperFromFile(files[0]);
        }
        
        /// <summary>
        /// 托盘切换播放/暂停状态
        /// </summary>
        private void NotifyIconManager_TogglePlayPauseRequested(object sender, EventArgs e)
        {
            WallpaperManager.TogglePause();
            NotifyIconManager.UpdatePauseState(WallpaperManager.IsPaused);
        }
        
        /// <summary>
        /// 托盘退出请求
        /// </summary>
        private void NotifyIconManager_ExitRequested(object sender, EventArgs e)
        {
            Shutdown();
        }
        
        /// <summary>
        /// 应用程序退出
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 清理托盘图标
                NotifyIconManager?.Dispose();
                
                // 停止性能监控
                PerformanceManager?.StopMonitoring();
                PerformanceManager?.Dispose();
                
                // 清理多显示器管理器
                MonitorManager?.Dispose();
                
                // 清理壁纸
                WallpaperManager?.Dispose();
            }
            catch (Exception ex)
            {
                // 记录错误但不中断退出
                System.Diagnostics.Debug.WriteLine($"应用退出时出错: {ex.Message}");
            }
            
            base.OnExit(e);
        }
        
        #region 异常处理
        
        /// <summary>
        /// UI线程未捕获异常处理
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException("UI线程异常", e.Exception);
            e.Handled = true; // 标记为已处理，防止应用崩溃
        }
        
        /// <summary>
        /// 非UI线程未捕获异常处理
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException("非UI线程异常", e.ExceptionObject as Exception);
        }
        
        /// <summary>
        /// Task未捕获异常处理
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            HandleException("Task异常", e.Exception);
            e.SetObserved(); // 标记为已观察，防止应用崩溃
        }
        
        /// <summary>
        /// 统一异常处理
        /// </summary>
        private void HandleException(string source, Exception ex)
        {
            if (ex == null) return;
            
            try
            {
                // 记录异常到日志
                string logDir = Path.Combine(AppDataDirectory, "Logs");
                Directory.CreateDirectory(logDir);
                
                string logFile = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log");
                string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}: {ex.Message}\r\n{ex.StackTrace}\r\n\r\n";
                
                File.AppendAllText(logFile, errorMessage);
                
                // 在Debug模式显示错误消息框
                #if DEBUG
                MessageBox.Show($"{source}:\n{ex.Message}\n\n{ex.StackTrace}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                #endif
            }
            catch
            {
                // 如果异常处理本身出错，确保不会循环
            }
        }
        
        #endregion
    }
}
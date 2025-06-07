using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using LuckyStars.Managers;
using LuckyStars.Players;
using LuckyStars.Utils;

namespace LuckyStars
{
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("windows10.0.17763.0")]
    public partial class MainWindow : Window
    {
        private readonly string targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "LuckyStarsWallpaper");
        private readonly double[] allowedRatios =
        [
            21.0 / 9.0,
            16.0 / 9.0,
            6.0 / 4.0,
            4.0 / 3.0
        ];
        private IntPtr _hwnd = IntPtr.Zero;

        // 模块化组件
        private MediaManager? mediaManager;
        private FileSystemMonitor? fileSystemMonitor;
        private TimerManager? timerManager;
        private InteractivePlayer? interactivePlayer;
        private MouseCoordinateManager? mouseCoordinateManager;
        private PowerManager? powerManager;
        [SupportedOSPlatform("windows7.0")]
        private readonly MusicPlayer musicPlayer = new();

        public MainWindow()
        {
            InitializeComponent();

            // 确保目标文件夹存在
            EnsureDirectoryExists(targetFolder);

            // 初始化各个模块
            InitializeModules();
        }

        private void InitializeModules()
        {
            // 初始化媒体管理器
            mediaManager = new MediaManager(mediaPlayer, BackgroundImageBrush, musicPlayer, targetFolder, allowedRatios);

            // 加载媒体路径
            mediaManager.LoadMediaPaths();

            // 初始化定时器管理器
            timerManager = new TimerManager(
                () => Dispatcher.Invoke(mediaManager.ShowMedia),
                () => Dispatcher.Invoke(mediaManager.LoadMediaPaths)
            );

            // 从注册表加载定时器间隔
            int timerInterval = RegistryManager.LoadTimerIntervalFromRegistry();
            timerManager.SetupTimer(timerInterval);

            // 初始化文件系统监控器
            fileSystemMonitor = new FileSystemMonitor(
                targetFolder,
                timerManager.ResetIdleTimer,
                () => Dispatcher.Invoke(mediaManager.LoadMediaPaths)
            );

            // 初始化互动播放器
            interactivePlayer = new InteractivePlayer(webView);
            // 异步初始化WebView
            InitializeWebView();

            // 初始化电源管理器
            powerManager = new PowerManager(this);
            powerManager.PowerStateChanged += OnPowerStateChanged;

            // 尝试加载上次使用的壁纸
            bool lastWallpaperLoaded = mediaManager.LoadLastWallpaperState();

            // 如果没有上次使用的壁纸或加载失败，则显示新的媒体
            if (!lastWallpaperLoaded)
            {
                mediaManager.ShowMedia();
            }
        }

        /// <summary>
        /// 处理电源状态变化
        /// </summary>
        /// <param name="isPowerSavingMode">是否进入节能模式</param>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows10.0.17763.0")]
        private void OnPowerStateChanged(bool isPowerSavingMode)
        {
            Dispatcher.Invoke(() =>
            {
                if (isPowerSavingMode)
                {
                    EnterPowerSavingMode();
                }
                else
                {
                    ExitPowerSavingMode();
                }
            });
        }

        /// <summary>
        /// 进入节能模式
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows10.0.17763.0")]
        private void EnterPowerSavingMode()
        {
            // 1. 暂停或降低星空动画的帧率
            if (interactivePlayer?.GetWebView()?.CoreWebView2 != null)
            {
                interactivePlayer.GetWebView().CoreWebView2.ExecuteScriptAsync(
                    "if(typeof setLowPowerMode === 'function') { setLowPowerMode(true); }");
            }

            // 2. 暂停视频播放（如果正在播放视频）
            if (mediaManager != null)
            {
                // 保存当前状态以便恢复
                _wasShowingVideo = mediaManager.IsShowingVideo();
                if (_wasShowingVideo)
                {
                    // 暂停视频播放，显示静态图像
                    mediaManager.PauseVideo();
                    mediaManager.ShowImage();
                }
            }
        }

        /// <summary>
        /// 退出节能模式
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows10.0.17763.0")]
        private void ExitPowerSavingMode()
        {
            // 1. 恢复星空动画的帧率
            if (interactivePlayer?.GetWebView()?.CoreWebView2 != null)
            {
                interactivePlayer.GetWebView().CoreWebView2.ExecuteScriptAsync(
                    "if(typeof setLowPowerMode === 'function') { setLowPowerMode(false); }");
            }

            // 2. 恢复视频播放（如果之前正在播放视频）
            if (mediaManager != null && _wasShowingVideo)
            {
                mediaManager.ResumeVideo();
                _wasShowingVideo = false;
            }
        }

        // 记录节能模式前的状态
        private bool _wasShowingVideo = false;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 在 WPF 窗口创建完成后再获取 hwnd (避免 hwnd 为 0 的问题)
            WindowInteropHelper helper = new(this);
            _hwnd = helper.Handle;
            // 确保窗口句柄有效
            if (_hwnd == IntPtr.Zero)
            {
                // 窗口句柄获取失败
                return;
            }

            // 句柄有效后，初始化鼠标坐标管理器
            if (interactivePlayer != null)
            {
                mouseCoordinateManager = new MouseCoordinateManager(
                    _hwnd,
                    interactivePlayer.GetWebView(),
                    interactivePlayer
                );
                mouseCoordinateManager.StartMouseCoordSender();
            }
        }

        /// <summary>
        /// 确保目录存在，如果不存在则创建
        /// </summary>
        /// <param name="directory">要检查的目录路径</param>
        /// <remarks>
        /// 注意：对于根目录 LuckyStarsWallpaper，应该使用 FileSystemMonitor 来创建，
        /// 以确保根目录的唯一性。此方法仅用于其他目录。
        /// </remarks>
        private static void EnsureDirectoryExists(string directory)
        {
            // 检查是否是根目录
            string rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "LuckyStarsWallpaper"
            );

            // 如果是根目录，则不在这里创建，而是依赖 FileSystemMonitor
            if (string.Equals(directory, rootDir, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // 对于其他目录，正常创建
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// 初始化WebView
        /// </summary>
        [SupportedOSPlatform("windows10.0.17763.0")]
        private async void InitializeWebView()
        {
            try
            {
                // 异步初始化WebView
                await interactivePlayer!.InitializeWebView();

                // 在初始化完成后，确保WebView可见
                interactivePlayer.SetWebViewVisibility(true);
            }
            catch (Exception ex)
            {
                // 记录错误，但不中断程序执行
                Console.WriteLine($"初始化WebView时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置定时器间隔并保存到注册表
        /// </summary>
        /// <param name="interval">定时器间隔（毫秒）</param>
        [SupportedOSPlatform("windows")]
        public void SetTimerInterval(int interval)
        {
            RegistryManager.SaveTimerIntervalToRegistry(interval);
            timerManager?.SetupTimer(interval);
        }

        /// <summary>
        /// 循环切换定时器状态
        /// </summary>
        /// <returns>下一个定时器状态</returns>
        [SupportedOSPlatform("windows")]
        public RegistryManager.TimerState CycleTimerState()
        {
            var nextState = RegistryManager.CycleTimerState();
            SetTimerInterval((int)nextState);
            return nextState;
        }

        /// <summary>
        /// 处理窗口大小变化事件
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows10.0.17763.0")]
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            interactivePlayer?.ApplyFullScreenToWebView();
            interactivePlayer?.ApplyDpiScale();
        }

        /// <summary>
        /// 窗口关闭时释放所有资源
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows7.0")]
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 释放资源
            fileSystemMonitor?.Dispose();
            timerManager?.Dispose();
            mediaManager?.Dispose();
            powerManager?.Dispose();
            mouseCoordinateManager?.StopMouseCoordSender();
            musicPlayer.Dispose();
        }

        #region 公共接口方法

        // 这些方法提供给外部调用，转发到相应的模块

        /// <summary>
        /// 切换到下一张图片
        /// </summary>
        [SupportedOSPlatform("windows")]
        public void NextImage()
        {
            mediaManager?.NextImage();
        }

        /// <summary>
        /// 切换到下一个视频
        /// </summary>
        [SupportedOSPlatform("windows")]
        public void NextVideo()
        {
            mediaManager?.NextVideo();
        }

        /// <summary>
        /// 播放/暂停音乐
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows7.0")]
        public void Webview2_playmusic()
        {
            mediaManager?.TogglePlayPauseMusic();
        }

        /// <summary>
        /// 处理音乐文件拖放
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows7.0")]
        public void HandleMusicFileDrop(string musicPath)
        {
            mediaManager?.HandleMusicFileDrop(musicPath);
        }

        /// <summary>
        /// 处理文件夹拖放
        /// </summary>
        [SupportedOSPlatform("windows")]
        public void HandleFolderDrop(string folderPath)
        {
            mediaManager?.HandleFolderDrop(folderPath);
        }

        /// <summary>
        /// 静音视频播放器
        /// </summary>
        [SupportedOSPlatform("windows")]
        public void MuteVideoPlayer()
        {
            mediaManager?.MuteVideoPlayer();
        }

        /// <summary>
        /// 取消静音视频播放器
        /// </summary>
        [SupportedOSPlatform("windows")]
        public void UnmuteVideoPlayer()
        {
            mediaManager?.UnmuteVideoPlayer();
        }

        /// <summary>
        /// 设置WebView可见性
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows10.0.17763.0")]
        public void SetWebViewVisibility(bool isVisible)
        {
            interactivePlayer?.SetWebViewVisibility(isVisible);
        }

        /// <summary>
        /// 获取WebView实例
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows10.0.17763.0")]
        public WebView2? GetWebView()
        {
            return interactivePlayer?.GetWebView();
        }

        /// <summary>
        /// 获取互动播放器
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows10.0.17763.0")]
        public InteractivePlayer? GetInteractivePlayer()
        {
            return interactivePlayer;
        }

        #endregion
    }
}

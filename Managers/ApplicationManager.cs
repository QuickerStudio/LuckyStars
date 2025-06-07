using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace LuckyStars.Managers
{
    public class ApplicationManager
    {
        private static Mutex? _mutex;
        private static readonly object _mutexLock = new();
        private static bool _mutexAcquired = false;
        private bool _isExiting = false;

        private readonly WallpaperManager _wallpaperManager;

        // 定义应用程序退出事件
        public event EventHandler? ApplicationExit;

        public ApplicationManager()
        {
            _wallpaperManager = new WallpaperManager();
        }

        public bool Initialize()
        {
            try
            {
                lock (_mutexLock)
                {
                    _mutex = new Mutex(true, "LuckyStarsWallpaper", out bool createdNew);
                    _mutexAcquired = createdNew;

                    if (!_mutexAcquired)
                    {
                        MessageBox.Show("程序已在运行！");
                        return false;
                    }
                }

                // 禁用系统壁纸幻灯片放映
                _wallpaperManager.DisableSystemWallpaperSlideshow();

                return true;
            }
            catch (Exception ex)
            {            
                return false;
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void CleanupAndExit()
        {
            if (_isExiting) return;
            _isExiting = true;

            // 首先触发退出事件，让其他组件有机会清理资源
            ApplicationExit?.Invoke(this, EventArgs.Empty);

            // 先恢复系统壁纸
            _wallpaperManager.RestoreSystemWallpaperSlideshow();

            // 确认已经恢复系统壁纸，确保在程序退出前完成
            EnsureWallpaperRestored();

            // 释放其他资源
            ReleaseResources();

            // 延迟一小段时间确保通知显示完成
            Task.Delay(500).ContinueWith(_ =>
            {
                // 在UI线程上执行关闭操作
                Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
            });
        }

        /// <summary>
        /// 确保系统壁纸已经恢复
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private void EnsureWallpaperRestored()
        {
            try
            {
                // 检查注册表中的壁纸设置是否已恢复
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false);
                if (key != null)
                {
                    // 读取当前壁纸路径
                    var currentWallpaper = key.GetValue("Wallpaper") as string;

                    // 如果壁纸路径为空或不存在，尝试再次恢复
                    if (string.IsNullOrEmpty(currentWallpaper) || !File.Exists(currentWallpaper))
                    {

                        _wallpaperManager.RestoreSystemWallpaperSlideshow();
                    }
                }
            }
            catch
            {
                // 出错时尝试再次恢复
                _wallpaperManager.RestoreSystemWallpaperSlideshow();
            }
        }

        private static void ReleaseResources()
        {
            lock (_mutexLock)
            {
                if (_mutex != null && _mutexAcquired)
                {
                    if (_mutex.WaitOne(0))
                    {
                        _mutex.ReleaseMutex();
                    }
                    _mutex.Dispose();
                    _mutex = null;
                    _mutexAcquired = false;
                }
            }
        }
    }
}

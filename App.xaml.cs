#nullable enable

using Microsoft.Win32;
using System;
using System.Runtime.Versioning;
using System.Windows;

using LuckyStars.Managers;
using LuckyStars.Utils;

namespace LuckyStars
{
    [SupportedOSPlatform("windows")]
    public partial class App : Application
    {
        private TrayManager? _trayManager;
        private MainWindow? _mainWindow;
        private WindowManager? _windowManager;
        private ApplicationManager? _appManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化应用程序管理器
            _appManager = new ApplicationManager();
            _appManager.ApplicationExit += (s, args) => _trayManager?.Dispose();

            // 检查是否可以启动应用程序
            if (!_appManager.Initialize())
            {
                Shutdown();
                return;
            }

            // 初始化窗口管理器
            _windowManager = new WindowManager();

            // 创建主窗口
            _mainWindow = new MainWindow();
            _trayManager = new TrayManager(_mainWindow);

            // 设置窗口初始化事件
            _mainWindow.SourceInitialized += (s, e2) => _windowManager.SetupWindowAsDesktopChild(_mainWindow);

            // 显示主窗口和托盘图标
            _mainWindow.Show();
            _trayManager.Show();

            // 监听显示设置变化事件
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            _windowManager?.ResetWindowPosition(_mainWindow!);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 清理资源并退出
            _appManager?.CleanupAndExit();

            // 移除事件监听
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

            base.OnExit(e);
        }
    }
}

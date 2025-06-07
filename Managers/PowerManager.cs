using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using WinForms = System.Windows.Forms;

namespace LuckyStars.Managers
{
    public class PowerManager : IDisposable
    {
        // 委托和事件，用于通知应用程序电源状态变化
        public delegate void PowerStateChangedEventHandler(bool isPowerSavingMode);
        public event PowerStateChangedEventHandler? PowerStateChanged;

        // 电源状态检查定时器
        private readonly System.Timers.Timer _powerCheckTimer;

        // 配置选项
        private bool _enablePowerSaving = true;
        private bool _pauseOnBatteryLow = true;
        private bool _pauseOnFullscreen = true;
        private int _lowBatteryThreshold = 20; // 低电量阈值（百分比）
        private int _checkIntervalSeconds = 5; // 检查间隔（秒）

        // 当前状态
        private bool _isCurrentlyInPowerSavingMode = false;
        private Window? _mainWindow;

        // Win32 API 用于检测全屏应用程序
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public PowerManager(Window mainWindow)
        {
            _mainWindow = mainWindow;

            // 初始化定时器
            _powerCheckTimer = new System.Timers.Timer(_checkIntervalSeconds * 1000);
            _powerCheckTimer.Elapsed += OnPowerCheckTimerElapsed;
            _powerCheckTimer.AutoReset = true;

            // 如果启用了电源管理，则启动定时器
            if (_enablePowerSaving)
            {
                _powerCheckTimer.Start();
            }

            // 初始检查一次电源状态
            CheckPowerState();
        }

        // 定时器回调，检查电源状态
        private void OnPowerCheckTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            CheckPowerState();
        }

        // 检查电源状态
        private void CheckPowerState()
        {
            bool shouldBePowerSaving = false;

            // 检查是否使用电池以及电池电量是否低
            if (_pauseOnBatteryLow)
            {
                WinForms.PowerStatus powerStatus = SystemInformation.PowerStatus;
                bool isOnBattery = powerStatus.PowerLineStatus == WinForms.PowerLineStatus.Offline;
                float batteryLifePercent = powerStatus.BatteryLifePercent * 100;

                if (isOnBattery && batteryLifePercent <= _lowBatteryThreshold)
                {
                    shouldBePowerSaving = true;
                    // 电池电量低，启用节能模式
                }
            }

            // 检查是否有全屏应用程序运行
            if (_pauseOnFullscreen && !shouldBePowerSaving)
            {
                if (IsAnyApplicationFullscreen())
                {
                    shouldBePowerSaving = true;
                    // 检测到全屏应用程序，启用节能模式
                }
            }

            // 如果状态发生变化，触发事件
            if (shouldBePowerSaving != _isCurrentlyInPowerSavingMode)
            {
                _isCurrentlyInPowerSavingMode = shouldBePowerSaving;
                PowerStateChanged?.Invoke(shouldBePowerSaving);
            }
        }

        // 检查是否有全屏应用程序运行
        private bool IsAnyApplicationFullscreen()
        {
            try
            {
                // 获取前台窗口句柄
                IntPtr foregroundWindow = GetForegroundWindow();

                // 如果前台窗口是我们的应用程序，则不考虑
                if (_mainWindow != null)
                {
                    IntPtr mainWindowHandle = new WindowInteropHelper(_mainWindow).Handle;
                    if (foregroundWindow == mainWindowHandle)
                    {
                        return false;
                    }
                }

                // 获取窗口标题，用于调试
                var windowTitle = new System.Text.StringBuilder(256);
                GetWindowText(foregroundWindow, windowTitle, 256);

                // 获取窗口矩形
                if (GetWindowRect(foregroundWindow, out RECT windowRect))
                {
                    // 获取屏幕尺寸
                    int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                    int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                    // 计算窗口尺寸
                    int windowWidth = windowRect.Right - windowRect.Left;
                    int windowHeight = windowRect.Bottom - windowRect.Top;

                    // 如果窗口尺寸接近或等于屏幕尺寸，则认为是全屏
                    bool isFullscreen = (Math.Abs(windowWidth - screenWidth) <= 10 &&
                                        Math.Abs(windowHeight - screenHeight) <= 10 &&
                                        windowRect.Left <= 10 && windowRect.Top <= 10);

                    if (isFullscreen)
                    {
                        // 全屏应用程序
                    }

                    return isFullscreen;
                }
            }
            catch (Exception ex)
            {
                // 检测全屏应用程序时出错
            }

            return false;
        }

        // 启用或禁用电源管理
        public void EnablePowerSaving(bool enable)
        {
            _enablePowerSaving = enable;

            if (enable)
            {
                _powerCheckTimer.Start();
                CheckPowerState(); // 立即检查一次
            }
            else
            {
                _powerCheckTimer.Stop();

                // 如果当前处于节能模式，则退出节能模式
                if (_isCurrentlyInPowerSavingMode)
                {
                    _isCurrentlyInPowerSavingMode = false;
                    PowerStateChanged?.Invoke(false);
                }
            }
        }

        // 设置低电量阈值
        public void SetLowBatteryThreshold(int percentThreshold)
        {
            if (percentThreshold >= 0 && percentThreshold <= 100)
            {
                _lowBatteryThreshold = percentThreshold;
                CheckPowerState(); // 立即检查一次
            }
        }

        // 设置是否在电池电量低时暂停
        public void SetPauseOnBatteryLow(bool pause)
        {
            _pauseOnBatteryLow = pause;
            CheckPowerState(); // 立即检查一次
        }

        // 设置是否在全屏应用程序运行时暂停
        public void SetPauseOnFullscreen(bool pause)
        {
            _pauseOnFullscreen = pause;
            CheckPowerState(); // 立即检查一次
        }

        // 设置检查间隔
        public void SetCheckInterval(int seconds)
        {
            if (seconds >= 1)
            {
                _checkIntervalSeconds = seconds;
                _powerCheckTimer.Interval = seconds * 1000;
            }
        }

        // 获取当前是否处于节能模式
        public bool IsInPowerSavingMode()
        {
            return _isCurrentlyInPowerSavingMode;
        }

        // 释放资源
        public void Dispose()
        {
            _powerCheckTimer.Stop();
            _powerCheckTimer.Elapsed -= OnPowerCheckTimerElapsed;
            _powerCheckTimer.Dispose();
            _mainWindow = null;
            GC.SuppressFinalize(this);
        }
    }
}

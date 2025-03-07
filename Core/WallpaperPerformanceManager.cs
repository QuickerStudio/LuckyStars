using System;
using System.Diagnostics;
using System.Timers;
using System.Runtime.InteropServices;
using System.Management;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Collections.Generic;

namespace LuckyStars.Core
{
    /// <summary>
    /// 壁纸性能管理器，监控系统资源并自动管理壁纸的性能
    /// </summary>
    public class WallpaperPerformanceManager : IDisposable
    {
        /// <summary>
        /// 壁纸暂停原因
        /// </summary>
        public enum PauseReason
        {
            None,
            UserPaused,
            OnBattery,
            FullScreenAppRunning,
            HighCpuUsage,
            SystemLocked,
            ScreenSleep
        }
        
        /// <summary>
        /// 性能模式
        /// </summary>
        public enum PerformanceMode
        {
            PowerSave,
            Balanced,
            Performance
        }
        
        /// <summary>
        /// 性能设置
        /// </summary>
        public class PerformanceSettings
        {
            /// <summary>
            /// 电池状态暂停
            /// </summary>
            public bool PauseOnBattery { get; set; } = true;
            
            /// <summary>
            /// 全屏应用暂停
            /// </summary>
            public bool PauseOnFullscreen { get; set; } = true;
            
            /// <summary>
            /// 高CPU使用暂停
            /// </summary>
            public bool PauseOnHighCpu { get; set; } = true;
            
            /// <summary>
            /// CPU使用率阈值
            /// </summary>
            public int CpuThreshold { get; set; } = 85;
            
            /// <summary>
            /// 锁屏时暂停
            /// </summary>
            public bool PauseOnLock { get; set; } = true;
            
            /// <summary>
            /// 超级省电模式设置
            /// </summary>
            public bool SuperSavingMode { get; set; } = false;
            
            /// <summary>
            /// 性能模式
            /// </summary>
            public PerformanceMode Mode { get; set; } = PerformanceMode.Balanced;
            
            /// <summary>
            /// 排除的应用列表（不会在这些应用全屏时暂停）
            /// </summary>
            public List<string> ExcludedApps { get; set; } = new List<string>();
        }
        
        // 壁纸暂停状态变化事件
        public event EventHandler<PauseReason> WallpaperPauseStateChanged;
        
        // 当前暂停原因
        private PauseReason _currentPauseReason = PauseReason.None;
        
        // 性能设置
        private PerformanceSettings _settings;
        
        // 监控定时器
        private Timer _monitorTimer;
        
        // CPU计数器
        private PerformanceCounter _cpuCounter;
        
        // 电源状态检测
        private PowerStatus _powerStatus;
        
        // 是否正在监控
        private bool _isMonitoring = false;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">性能设置</param>
        public WallpaperPerformanceManager(PerformanceSettings settings)
        {
            _settings = settings ?? new PerformanceSettings();
            
            // 初始化监控定时器
            _monitorTimer = new Timer();
            _monitorTimer.Interval = 3000; // 每3秒检测一次
            _monitorTimer.Elapsed += MonitorTimer_Elapsed;
            
            try
            {
                // 初始化CPU计数器
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                // 预热CPU计数器
                _cpuCounter.NextValue();
                
                // 获取电源状态
                _powerStatus = SystemInformation.PowerStatus;
                
                // 注册系统事件
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
                SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化性能管理器失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 启动监控
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
                return;
                
            try
            {
                _monitorTimer.Start();
                _isMonitoring = true;
                
                Debug.WriteLine("性能监控已启动");
                
                // 立即执行一次检查
                CheckSystemState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动性能监控失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 停止监控
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;
                
            try
            {
                _monitorTimer.Stop();
                _isMonitoring = false;
                
                Debug.WriteLine("性能监控已停止");
                
                // 恢复暂停状态
                SetPauseState(PauseReason.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"停止性能监控失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 定时器事件处理
        /// </summary>
        private void MonitorTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckSystemState();
        }
        
        /// <summary>
        /// 更新性能设置
        /// </summary>
        /// <param name="settings">新的设置</param>
        public void UpdateSettings(PerformanceSettings settings)
        {
            if (settings == null)
                return;
                
            _settings = settings;
            
            // 根据新设置调整监控频率
            AdjustMonitoringInterval();
            
            // 立即应用新设置
            CheckSystemState();
        }
        
        /// <summary>
        /// 根据性能模式调整监控频率
        /// </summary>
        private void AdjustMonitoringInterval()
        {
            switch (_settings.Mode)
            {
                case PerformanceMode.PowerSave:
                    _monitorTimer.Interval = 5000; // 5秒
                    break;
                    
                case PerformanceMode.Balanced:
                    _monitorTimer.Interval = 3000; // 3秒
                    break;
                    
                case PerformanceMode.Performance:
                    _monitorTimer.Interval = 1500; // 1.5秒
                    break;
            }
        }
        
        /// <summary>
        /// 检查系统状态
        /// </summary>
        private void CheckSystemState()
        {
            try
            {
                // 查询暂停原因
                PauseReason reason = GetPauseReason();
                
                // 如果与当前暂停原因不同，则更新状态
                if (reason != _currentPauseReason)
                {
                    SetPauseState(reason);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查系统状态失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取暂停原因
        /// </summary>
        /// <returns>暂停原因</returns>
        private PauseReason GetPauseReason()
        {
            // 用户手动暂停优先级最高
            if (_currentPauseReason == PauseReason.UserPaused)
                return PauseReason.UserPaused;
                
            // 检查电池状态
            if (_settings.PauseOnBattery && IsOnBatteryPower())
                return PauseReason.OnBattery;
                
            // 检查是否有全屏应用运行
            if (_settings.PauseOnFullscreen && IsFullScreenAppRunning())
                return PauseReason.FullScreenAppRunning;
                
            // 检查CPU使用率
            if (_settings.PauseOnHighCpu && IsCpuUsageHigh())
                return PauseReason.HighCpuUsage;
                
            // 检查系统锁屏状态
            if (_settings.PauseOnLock && IsSystemLocked())
                return PauseReason.SystemLocked;
                
            // 超级省电模式
            if (_settings.SuperSavingMode && (IsOnBatteryPower() && GetBatteryPercentage() < 20))
                return PauseReason.OnBattery;
                
            // 无需暂停
            return PauseReason.None;
        }
        
        /// <summary>
        /// 设置暂停状态
        /// </summary>
        /// <param name="reason">暂停原因</param>
        private void SetPauseState(PauseReason reason)
        {
            _currentPauseReason = reason;
            
            // 触发事件
            WallpaperPauseStateChanged?.Invoke(this, reason);
            
            if (reason == PauseReason.None)
            {
                Debug.WriteLine("壁纸已恢复播放");
            }
            else
            {
                Debug.WriteLine($"壁纸已暂停，原因: {reason}");
            }
        }
        
        /// <summary>
        /// 手动设置暂停/恢复
        /// </summary>
        /// <param name="pause">是否暂停</param>
        public void SetUserPause(bool pause)
        {
            if (pause)
            {
                SetPauseState(PauseReason.UserPaused);
            }
            else
            {
                // 如果当前是用户暂停，则恢复
                if (_currentPauseReason == PauseReason.UserPaused)
                {
                    SetPauseState(PauseReason.None);
                    CheckSystemState(); // 立即检查是否有其他暂停原因
                }
            }
        }
        
        #region 系统状态检测
        
        /// <summary>
        /// 检查是否使用电池电源
        /// </summary>
        /// <returns>是否使用电池电源</returns>
        private bool IsOnBatteryPower()
        {
            try
            {
                // 刷新电源状态
                _powerStatus = SystemInformation.PowerStatus;
                
                // 当电源状态为在线(1),且不是在充电,则为使用电池电源
                return _powerStatus.PowerLineStatus == PowerLineStatus.Offline;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查电池状态失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取电池电量百分比
        /// </summary>
        /// <returns>电池电量百分比</returns>
        private float GetBatteryPercentage()
        {
            try
            {
                // 刷新电源状态
                _powerStatus = SystemInformation.PowerStatus;
                
                // 返回电池电量百分比
                return _powerStatus.BatteryLifePercent * 100;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取电池电量失败: {ex.Message}");
                return 100;
            }
        }
        
        /// <summary>
        /// 检查CPU使用率是否过高
        /// </summary>
        /// <returns>CPU使用率是否过高</returns>
        private bool IsCpuUsageHigh()
        {
            try
            {
                // 获取CPU使用率
                float cpuUsage = _cpuCounter.NextValue();
                
                // 如果CPU使用率超过阈值，则认为过高
                return cpuUsage > _settings.CpuThreshold;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查CPU使用率失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查是否有全屏应用运行
        /// </summary>
        /// <returns>是否有全屏应用运行</returns>
        private bool IsFullScreenAppRunning()
        {
            try
            {
                // 获取前台窗口句柄
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                    return false;
                    
                // 获取窗口标题
                string windowTitle = GetWindowTitle(foregroundWindow);
                
                // 检查是否在排除列表中
                if (!string.IsNullOrEmpty(windowTitle) && IsAppExcluded(windowTitle))
                    return false;
                
                // 获取窗口位置和大小
                RECT windowRect = new RECT();
                GetWindowRect(foregroundWindow, ref windowRect);
                
                // 获取屏幕大小
                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                
                // 窗口宽度和高度
                int windowWidth = windowRect.Right - windowRect.Left;
                int windowHeight = windowRect.Bottom - windowRect.Top;
                
                // 检查窗口是否为全屏
                // 窗口大小与屏幕大小接近，且窗口位置靠近屏幕边缘
                bool isNearlyFullScreen = 
                    Math.Abs(windowWidth - screenWidth) < 10 &&
                    Math.Abs(windowHeight - screenHeight) < 10 &&
                    windowRect.Left < 10 && windowRect.Top < 10;
                
                // 获取窗口样式
                int style = GetWindowLong(foregroundWindow, GWL_STYLE);
                
                // 检查窗口是否有标题栏和边框
                bool hasWindowDecorations = (style & WS_CAPTION) != 0 || (style & WS_THICKFRAME) != 0;
                
                // 全屏应用通常没有标题栏和边框，并且窗口大小接近屏幕大小
                return isNearlyFullScreen && !hasWindowDecorations;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查全屏应用失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 判断应用是否在排除列表中
        /// </summary>
        /// <param name="windowTitle">窗口标题</param>
        /// <returns>是否在排除列表中</returns>
        private bool IsAppExcluded(string windowTitle)
        {
            if (string.IsNullOrEmpty(windowTitle) || _settings.ExcludedApps == null)
                return false;
                
            foreach (string app in _settings.ExcludedApps)
            {
                if (windowTitle.Contains(app, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取窗口标题
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>窗口标题</returns>
        private string GetWindowTitle(IntPtr hWnd)
        {
            try
            {
                // 获取窗口标题长度
                int length = GetWindowTextLength(hWnd);
                if (length == 0)
                    return string.Empty;
                    
                // 获取窗口标题
                StringBuilder builder = new StringBuilder(length + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                
                return builder.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 检查系统是否锁屏
        /// </summary>
        /// <returns>系统是否锁屏</returns>
        private bool IsSystemLocked()
        {
            // 需要使用注册表或系统API检查锁屏状态
            // 因为当前没有直接的API获取锁屏状态，此处简化实现
            return false;
        }
        
        #endregion
        
        #region 系统事件处理
        
        /// <summary>
        /// 电源模式变更事件处理
        /// </summary>
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            Debug.WriteLine($"电源模式变更: {e.Mode}");
            
            // 立即检查系统状态
            CheckSystemState();
        }
        
        /// <summary>
        /// 会话切换事件处理（锁屏、解锁等）
        /// </summary>
        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            Debug.WriteLine($"会话切换: {e.Reason}");
            
            // 检查是否是锁屏或解锁
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                if (_settings.PauseOnLock)
                {
                    SetPauseState(PauseReason.SystemLocked);
                }
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                if (_currentPauseReason == PauseReason.SystemLocked)
                {
                    SetPauseState(PauseReason.None);
                    CheckSystemState();
                }
            }
        }
        
        /// <summary>
        /// 显示设置变更事件处理
        /// </summary>
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("显示设置变更");
            
            // 立即检查系统状态
            CheckSystemState();
        }
        
        #endregion
        
        #region Win32 API
        
        // 窗口结构体
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        // 常量定义
        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        
        // API导入
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        #endregion
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 停止监控
                StopMonitoring();
                
                // 释放定时器
                if (_monitorTimer != null)
                {
                    _monitorTimer.Elapsed -= MonitorTimer_Elapsed;
                    _monitorTimer.Dispose();
                    _monitorTimer = null;
                }
                
                // 释放CPU计数器
                if (_cpuCounter != null)
                {
                    _cpuCounter.Dispose();
                    _cpuCounter = null;
                }
                
                // 取消注册系统事件
                SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放性能管理器资源失败: {ex.Message}");
            }
        }
    }
}
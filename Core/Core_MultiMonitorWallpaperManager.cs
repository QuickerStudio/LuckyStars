using LuckyStars.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace LuckyStars.Core
{
    /// <summary>
    /// 多显示器壁纸管理器，负责管理多个显示器的壁纸设置和同步
    /// </summary>
    public class MultiMonitorWallpaperManager
    {
        private readonly WallpaperManager _wallpaperManager;
        private readonly List<MonitorInfo> _monitors = new List<MonitorInfo>();
        
        /// <summary>
        /// 检测到显示器配置变化时触发
        /// </summary>
        public event EventHandler<List<MonitorInfo>> MonitorsChanged;
        
        /// <summary>
        /// 壁纸更改时触发
        /// </summary>
        public event EventHandler<(MonitorInfo Monitor, string WallpaperPath, WallpaperType Type)> WallpaperChanged;

        /// <summary>
        /// 获取监视器列表
        /// </summary>
        public IReadOnlyList<MonitorInfo> Monitors => _monitors.AsReadOnly();

        /// <summary>
        /// 初始化多显示器壁纸管理器
        /// </summary>
        /// <param name="wallpaperManager">底层壁纸管理器</param>
        public MultiMonitorWallpaperManager(WallpaperManager wallpaperManager)
        {
            _wallpaperManager = wallpaperManager ?? throw new ArgumentNullException(nameof(wallpaperManager));
            
            // 首次检测显示器
            DetectMonitors();
            
            // 注册显示设置更改事件
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        /// <summary>
        /// 检测系统显示器
        /// </summary>
        public void DetectMonitors()
        {
            _monitors.Clear();
            
            // 枚举所有显示器
            WinAPI.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);
            
            // 确保显示器索引正确
            for (int i = 0; i < _monitors.Count; i++)
            {
                _monitors[i].Index = i;
            }
            
            // 触发监视器变化事件
            MonitorsChanged?.Invoke(this, _monitors);
        }

        /// <summary>
        /// 为特定显示器设置壁纸
        /// </summary>
        /// <param name="monitorIndex">显示器索引</param>
        /// <param name="wallpaperPath">壁纸路径</param>
        /// <param name="wallpaperType">壁纸类型</param>
        /// <returns>异步任务</returns>
        public async Task SetWallpaperAsync(int monitorIndex, string wallpaperPath, WallpaperType wallpaperType)
        {
            if (monitorIndex < 0 || monitorIndex >= _monitors.Count)
                throw new ArgumentOutOfRangeException(nameof(monitorIndex), "无效的显示器索引");

            if (string.IsNullOrEmpty(wallpaperPath))
                throw new ArgumentNullException(nameof(wallpaperPath), "壁纸路径不能为空");

            if (!File.Exists(wallpaperPath))
                throw new FileNotFoundException("壁纸文件不存在", wallpaperPath);

            // 获取对应显示器
            MonitorInfo monitor = _monitors[monitorIndex];
            
            // 更新显示器壁纸信息
            monitor.WallpaperPath = wallpaperPath;
            monitor.WallpaperType = wallpaperType;
            
            // 根据壁纸类型选择不同的渲染方式
            await _wallpaperManager.RenderWallpaperAsync(monitor, wallpaperPath, wallpaperType);
            
            // 触发壁纸变化事件
            WallpaperChanged?.Invoke(this, (monitor, wallpaperPath, wallpaperType));
        }

        /// <summary>
        /// 为所有显示器设置相同的壁纸
        /// </summary>
        /// <param name="wallpaperPath">壁纸路径</param>
        /// <param name="wallpaperType">壁纸类型</param>
        /// <returns>异步任务</returns>
        public async Task SetWallpaperToAllAsync(string wallpaperPath, WallpaperType wallpaperType)
        {
            if (string.IsNullOrEmpty(wallpaperPath))
                throw new ArgumentNullException(nameof(wallpaperPath), "壁纸路径不能为空");

            if (!File.Exists(wallpaperPath))
                throw new FileNotFoundException("壁纸文件不存在", wallpaperPath);

            // 为每个显示器设置壁纸
            for (int i = 0; i < _monitors.Count; i++)
            {
                await SetWallpaperAsync(i, wallpaperPath, wallpaperType);
            }
        }

        /// <summary>
        /// 暂停所有显示器的壁纸播放
        /// </summary>
        public void PauseAll()
        {
            _wallpaperManager.PauseRendering();
        }

        /// <summary>
        /// 恢复所有显示器的壁纸播放
        /// </summary>
        public void ResumeAll()
        {
            _wallpaperManager.ResumeRendering();
        }

        /// <summary>
        /// 获取鼠标所在的显示器
        /// </summary>
        /// <param name="cursorPosition">鼠标位置</param>
        /// <returns>鼠标所在的显示器，如果未找到则返回主显示器</returns>
        public MonitorInfo GetMonitorFromCursor(Point cursorPosition)
        {
            // 查找包含鼠标位置的显示器
            foreach (var monitor in _monitors)
            {
                if (monitor.ContainsPoint(cursorPosition))
                {
                    return monitor;
                }
            }
            
            // 如果未找到，返回主显示器
            return _monitors.FirstOrDefault(m => m.IsPrimary) ?? _monitors.FirstOrDefault();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 取消注册显示设置更改事件
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            
            // 清空显示器列表
            _monitors.Clear();
        }

        /// <summary>
        /// 显示设置更改事件处理
        /// </summary>
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // 重新检测显示器
            DetectMonitors();
            
            // 重新应用壁纸设置
            Task.Run(async () =>
            {
                for (int i = 0; i < _monitors.Count; i++)
                {
                    var monitor = _monitors[i];
                    if (!string.IsNullOrEmpty(monitor.WallpaperPath) && File.Exists(monitor.WallpaperPath))
                    {
                        await SetWallpaperAsync(i, monitor.WallpaperPath, monitor.WallpaperType);
                    }
                }
            });
        }

        /// <summary>
        /// 监视器枚举回调
        /// </summary>
        private bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref WinAPI.RECT lprcMonitor, IntPtr dwData)
        {
            // 获取监视器信息
            var monitorInfo = new WinAPI.MONITORINFOEX();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(WinAPI.MONITORINFOEX));
            
            if (WinAPI.GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                // 创建监视器信息模型
                var monitor = new MonitorInfo
                {
                    Handle = hMonitor,
                    DeviceName = new string(monitorInfo.szDevice).TrimEnd('\0'),
                    DeviceId = GetMonitorDeviceId(new string(monitorInfo.szDevice).TrimEnd('\0')),
                    Bounds = new Rect(
                        lprcMonitor.Left, 
                        lprcMonitor.Top, 
                        lprcMonitor.Right - lprcMonitor.Left, 
                        lprcMonitor.Bottom - lprcMonitor.Top
                    ),
                    WorkingArea = new Rect(
                        monitorInfo.rcWork.Left,
                        monitorInfo.rcWork.Top,
                        monitorInfo.rcWork.Right - monitorInfo.rcWork.Left,
                        monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top
                    ),
                    IsPrimary = (monitorInfo.dwFlags & WinAPI.MONITORINFOF_PRIMARY) != 0,
                    DpiScale = GetMonitorDpiScale(hMonitor)
                };
                
                _monitors.Add(monitor);
            }
            
            // 继续枚举
            return true;
        }

        /// <summary>
        /// 获取监视器DPI缩放比例
        /// </summary>
        private double GetMonitorDpiScale(IntPtr hMonitor)
        {
            try
            {
                uint dpiX = 96, dpiY = 96;
                
                // 尝试使用Per-Monitor DPI API
                if (WinAPI.GetDpiForMonitor(hMonitor, WinAPI.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out dpiX, out dpiY) == 0)
                {
                    return dpiX / 96.0;
                }
            }
            catch
            {
                // API可能不可用（Windows 8.1之前的系统）
            }
            
            // 回退到系统DPI
            using (var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = graphics.DpiX;
                return dpiX / 96.0;
            }
        }

        /// <summary>
        /// 从设备名称获取唯一设备ID
        /// </summary>
        private string GetMonitorDeviceId(string deviceName)
        {
            try
            {
                // 从设备名称中提取ID，通常格式为：\\.\DISPLAY1
                string id = deviceName.Replace("\\", "").Replace(".", "").Replace("\0", "").Trim();
                
                // 尝试获取更好的设备ID，但这需要更高级的API
                // 此处仅使用简单的提取方式
                return id;
            }
            catch
            {
                // 如果提取失败，使用原始设备名
                return deviceName;
            }
        }

        /// <summary>
        /// 用于系统事件的静态类
        /// </summary>
        private static class SystemEvents
        {
            /// <summary>
            /// 显示设置更改事件
            /// </summary>
            public static event EventHandler DisplaySettingsChanged;

            static SystemEvents()
            {
                // 注册消息
                ComponentDispatcher.ThreadFilterMessage += ComponentDispatcher_ThreadFilterMessage;
            }

            private static void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
            {
                // 监听显示设置更改消息
                if (msg.message == WinAPI.WM_DISPLAYCHANGE)
                {
                    DisplaySettingsChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }
    }
}
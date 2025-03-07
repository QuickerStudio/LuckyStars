using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Management;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Security.Principal;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 系统辅助工具类，提供系统相关的功能
    /// </summary>
    public static class SystemHelper
    {
        /// <summary>
        /// 判断应用程序是否以管理员权限运行
        /// </summary>
        /// <returns>是否以管理员权限运行</returns>
        public static bool IsRunAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查管理员权限失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重启应用程序并请求管理员权限
        /// </summary>
        /// <param name="args">启动参数</param>
        /// <returns>是否成功请求重启</returns>
        public static bool RestartAsAdmin(string args = null)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args ?? string.Empty,
                    UseShellExecute = true,
                    Verb = "runas" // 请求管理员权限
                };

                Process.Start(startInfo);
                Application.Current.Shutdown();
                return true;
            }
            catch (Win32Exception)
            {
                   catch (Win32Exception)
            {
                // 用户取消了UAC提示
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"以管理员权限重启应用程序失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置应用程序开机自启动
        /// </summary>
        /// <param name="enable">是否启用开机自启动</param>
        /// <param name="appName">应用程序名称</param>
        /// <returns>是否设置成功</returns>
        public static bool SetAutoStart(bool enable, string appName = "LuckyStars")
        {
            try
            {
                string execPath = Process.GetCurrentProcess().MainModule.FileName;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            key.SetValue(appName, execPath);
                        }
                        else
                        {
                            if (key.GetValue(appName) != null)
                            {
                                key.DeleteValue(appName);
                            }
                        }
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置开机自启动失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断应用程序是否设置为开机自启动
        /// </summary>
        /// <param name="appName">应用程序名称</param>
        /// <returns>是否设置为开机自启动</returns>
        public static bool IsAutoStartEnabled(string appName = "LuckyStars")
        {
            try
            {
                string execPath = Process.GetCurrentProcess().MainModule.FileName;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        string value = key.GetValue(appName) as string;
                        return value != null && value.Equals(execPath, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查开机自启动设置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取当前Windows系统版本信息
        /// </summary>
        /// <returns>系统版本信息</returns>
        public static OSVersionInfo GetOSVersion()
        {
            try
            {
                OSVersionInfo info = new OSVersionInfo();
                
                // 获取操作系统信息
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption, Version, OSArchitecture FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        info.Name = obj["Caption"] as string;
                        info.Version = obj["Version"] as string;
                        info.Architecture = obj["OSArchitecture"] as string;
                        break;
                    }
                }

                // 判断Windows 11
                if (!string.IsNullOrEmpty(info.Version))
                {
                    Version version = Version.Parse(info.Version);
                    if (version.Major >= 10 && version.Build >= 22000)
                    {
                        info.Name = "Windows 11";
                    }
                }

                // 检查Windows功能更新版本
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (key != null)
                        {
                            object displayVersion = key.GetValue("DisplayVersion");
                            if (displayVersion != null)
                            {
                                info.FeatureUpdateVersion = displayVersion.ToString();
                            }
                            else
                            {
                                // 尝试获取ReleaseId (旧版Windows 10)
                                object releaseId = key.GetValue("ReleaseId");
                                if (releaseId != null)
                                {
                                    info.FeatureUpdateVersion = releaseId.ToString();
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // 忽略注册表读取错误
                }

                return info;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取操作系统版本信息失败: {ex.Message}");
                return new OSVersionInfo
                {
                    Name = "Unknown Windows",
                    Version = Environment.OSVersion.Version.ToString(),
                    Architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"
                };
            }
        }

        /// <summary>
        /// 操作系统版本信息类
        /// </summary>
        public class OSVersionInfo
        {
            /// <summary>
            /// 操作系统名称
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 版本号
            /// </summary>
            public string Version { get; set; }

            /// <summary>
            /// 系统架构
            /// </summary>
            public string Architecture { get; set; }

            /// <summary>
            /// 功能更新版本
            /// </summary>
            public string FeatureUpdateVersion { get; set; }

            /// <summary>
            /// 转换为字符串表示
            /// </summary>
            public override string ToString()
            {
                string result = $"{Name} {Version}";
                if (!string.IsNullOrEmpty(Architecture))
                {
                    result += $" ({Architecture})";
                }
                if (!string.IsNullOrEmpty(FeatureUpdateVersion))
                {
                    result += $", 版本 {FeatureUpdateVersion}";
                }
                return result;
            }
        }

        /// <summary>
        /// 获取系统内存信息
        /// </summary>
        /// <returns>内存信息</returns>
        public static MemoryInfo GetMemoryInfo()
        {
            try
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (!GlobalMemoryStatusEx(ref memStatus))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return new MemoryInfo
                {
                    TotalPhysicalMemory = memStatus.ullTotalPhys,
                    AvailablePhysicalMemory = memStatus.ullAvailPhys,
                    MemoryLoad = memStatus.dwMemoryLoad,
                    TotalVirtualMemory = memStatus.ullTotalVirtual,
                    AvailableVirtualMemory = memStatus.ullAvailVirtual
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取系统内存信息失败: {ex.Message}");
                return new MemoryInfo();
            }
        }

        /// <summary>
        /// 内存信息类
        /// </summary>
        public class MemoryInfo
        {
            /// <summary>
            /// 物理内存总量（字节）
            /// </summary>
            public ulong TotalPhysicalMemory { get; set; }

            /// <summary>
            /// 可用物理内存（字节）
            /// </summary>
            public ulong AvailablePhysicalMemory { get; set; }

            /// <summary>
            /// 内存使用率（百分比）
            /// </summary>
            public uint MemoryLoad { get; set; }

            /// <summary>
            /// 虚拟内存总量（字节）
            /// </summary>
            public ulong TotalVirtualMemory { get; set; }

            /// <summary>
            /// 可用虚拟内存（字节）
            /// </summary>
            public ulong AvailableVirtualMemory { get; set; }

            /// <summary>
            /// 已使用物理内存（字节）
            /// </summary>
            public ulong UsedPhysicalMemory => TotalPhysicalMemory - AvailablePhysicalMemory;

            /// <summary>
            /// 已使用虚拟内存（字节）
            /// </summary>
            public ulong UsedVirtualMemory => TotalVirtualMemory - AvailableVirtualMemory;

            /// <summary>
            /// 以可读格式获取物理内存总量
            /// </summary>
            public string TotalPhysicalMemoryString => FileUtils.GetReadableSize((long)TotalPhysicalMemory);

            /// <summary>
            /// 以可读格式获取可用物理内存
            /// </summary>
            public string AvailablePhysicalMemoryString => FileUtils.GetReadableSize((long)AvailablePhysicalMemory);

            /// <summary>
            /// 以可读格式获取已使用物理内存
            /// </summary>
            public string UsedPhysicalMemoryString => FileUtils.GetReadableSize((long)UsedPhysicalMemory);
        }

        /// <summary>
        /// 检测当前操作系统是否支持透明窗口
        /// </summary>
        /// <returns>是否支持透明窗口</returns>
        public static bool IsAeroEnabled()
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6) // Vista或更高版本
                {
                    DwmIsCompositionEnabled(out bool enabled);
                    return enabled;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查Aero支持失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断系统是否为Windows 10及以上版本
        /// </summary>
        /// <returns>是否为Windows 10及以上版本</returns>
        public static bool IsWindows10OrNewer()
        {
            return Environment.OSVersion.Version.Major >= 10;
        }

        /// <summary>
        /// 判断系统是否为Windows 11及以上版本
        /// </summary>
        /// <returns>是否为Windows 11及以上版本</returns>
        public static bool IsWindows11OrNewer()
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;
        }

        /// <summary>
        /// 打开系统文件位置并选中指定文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功打开</returns>
        public static bool OpenFileInExplorer(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开文件位置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 打开系统文件夹
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>是否成功打开</returns>
        public static bool OpenFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    return false;

                Process.Start("explorer.exe", folderPath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开文件夹失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用默认应用程序打开文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功打开</returns>
        public static bool OpenFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 打开链接（URL或文件）
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <returns>是否成功打开</returns>
        public static bool OpenUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return false;

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开链接失败: {url}, 错误: {ex.Message}");
                
                // 备选方法，尝试使用cmd调用默认浏览器
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start {url.Replace("&", "^&")}",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 获取系统桌面壁纸路径
        /// </summary>
        /// <returns>当前桌面壁纸路径</returns>
        public static string GetCurrentDesktopWallpaper()
        {
            try
            {
                StringBuilder path = new StringBuilder(260);
                if (SystemParametersInfo(SPI_GETDESKWALLPAPER, path.Capacity, path, 0))
                {
                    return path.ToString();
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取桌面壁纸路径失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 恢复系统桌面壁纸
        /// </summary>
        /// <returns>是否恢复成功</returns>
        public static bool RestoreDesktopWallpaper()
        {
            try
            {
                return SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, null, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"恢复桌面壁纸失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置系统桌面壁纸
        /// </summary>
        /// <param name="wallpaperPath">壁纸路径</param>
        /// <returns>是否设置成功</returns>
        public static bool SetDesktopWallpaper(string wallpaperPath)
        {
            try
            {
                if (!File.Exists(wallpaperPath))
                    return false;

                return SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wallpaperPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置桌面壁纸失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检测屏幕保护程序是否激活
        /// </summary>
        /// <returns>屏幕保护程序是否激活</returns>
        public static bool IsScreensaverActive()
        {
            try
            {
                bool result = false;
                if (SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref result, 0))
                {
                    return result;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测屏幕保护程序状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取显示器数量
        /// </summary>
        /// <returns>显示器数量</returns>
        public static int GetMonitorCount()
        {
            return Screen.AllScreens.Length;
        }

        /// <summary>
        /// 获取所有显示器信息
        /// </summary>
        /// <returns>显示器信息列表</returns>
        public static List<MonitorInfo> GetMonitors()
        {
            try
            {
                Screen[] screens = Screen.AllScreens;
                List<MonitorInfo> monitors = new List<MonitorInfo>();

                for (int i = 0; i < screens.Length; i++)
                {
                    Screen screen = screens[i];
                    MonitorInfo monitor = new MonitorInfo
                    {
                        DeviceName = screen.DeviceName,
                        Bounds = screen.Bounds,
                        WorkingArea = screen.WorkingArea,
                        IsPrimary = screen.Primary,
                        Index = i
                    };

                    // 获取显示器DPI
                    try
                    {
                        Point pt = new Point(screen.Bounds.X + screen.Bounds.Width / 2, screen.Bounds.Y + screen.Bounds.Height / 2);
                        IntPtr hmon = MonitorFromPoint(new POINT { x = pt.X, y = pt.Y }, MONITOR_DEFAULTTONEAREST);
                        if (hmon != IntPtr.Zero && GetDpiForMonitor(hmon, 0, out uint dpiX, out uint dpiY) == 0)
                        {
                            monitor.DpiX = dpiX;
                            monitor.DpiY = dpiY;
                            monitor.DpiScale = dpiX / 96.0f;
                        }
                    }
                    catch
                    {
                        // 如果获取DPI失败，使用默认值
                        monitor.DpiX = 96;
                        monitor.DpiY = 96;
                        monitor.DpiScale = 1.0f;
                    }

                    monitors.Add(monitor);
                }

                return monitors;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取显示器信息失败: {ex.Message}");
                return new List<MonitorInfo>();
            }
        }

        /// <summary>
        /// 显示器信息类
        /// </summary>
        public class MonitorInfo
        {
            public string DeviceName { get; set; }
            public Rectangle Bounds { get; set; }
            public Rectangle WorkingArea { get; set; }
            public bool IsPrimary { get; set; }
            public int Index { get; set; }
            public float DpiScale { get; set; } = 1.0f;
            public uint DpiX { get; set; } = 96;
            public uint DpiY { get; set; } = 96;
        }

        /// <summary>
        /// 检查电源状态
        /// </summary>
        /// <returns>电源状态信息</returns>
        public static PowerStatusInfo GetPowerStatus()
        {
            try
            {
                PowerStatus status = SystemInformation.PowerStatus;
                return new PowerStatusInfo
                {
                    BatteryChargeStatus = status.BatteryChargeStatus,
                    BatteryLifePercent = status.BatteryLifePercent,
                    BatteryLifeRemaining = status.BatteryLifeRemaining,
                    PowerLineStatus = status.PowerLineStatus
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取电源状态失败: {ex.Message}");
                return new PowerStatusInfo();
            }
        }

        /// <summary>
        /// 电源状态信息类
        /// </summary>
        public class PowerStatusInfo
        {
            /// <summary>
            /// 电池充电状态
            /// </summary>
            public BatteryChargeStatus BatteryChargeStatus { get; set; }

            /// <summary>
            /// 电池电量百分比（0.0-1.0）
            /// </summary>
            public float BatteryLifePercent { get; set; }

            /// <summary>
            /// 剩余电池寿命（秒），-1表示未知
            /// </summary>
            public int BatteryLifeRemaining { get; set; }

            /// <summary>
            /// 电源线状态
            /// </summary>
            public PowerLineStatus PowerLineStatus { get; set; }

            /// <summary>
            /// 是否处于电池供电模式
            /// </summary>
            public bool IsRunningOnBattery => PowerLineStatus == PowerLineStatus.Offline;

            /// <summary>
            /// 电池电量百分比（0-100）
            /// </summary>
            public int BatteryPercentage => (int)(BatteryLifePercent * 100);

            /// <summary>
            /// 剩余电池寿命的可读表示
            /// </summary>
            public string BatteryLifeRemainingString
            {
                get
                {
                    if (BatteryLifeRemaining < 0)
                        return "未知";

                    TimeSpan time = TimeSpan.FromSeconds(BatteryLifeRemaining);
                    return time.TotalHours >= 1
                        ? $"{time.Hours}小时{time.Minutes}分钟"
                        : $"{time.Minutes}分钟{time.Seconds}秒";
                }
            }
        }

        /// <summary>
        /// 尝试释放内存
        /// </summary>
        public static void TryReleaseMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放内存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取应用程序路径
        /// </summary>
        /// <returns>应用程序路径</returns>
        public static string GetApplicationPath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        /// <summary>
        /// 获取应用程序目录
        /// </summary>
        /// <returns>应用程序目录</returns>
        public static string GetApplicationDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// 获取应用程序数据目录
        /// </summary>
        /// <param name="appName">应用程序名称</param>
        /// <returns>应用程序数据目录</returns>
        public static string GetAppDataDirectory(string appName = "LuckyStars")
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            return path;
        }

        #region Win32 API
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        private const int SPI_GETDESKWALLPAPER = 0x0073;
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPI_GETSCREENSAVERRUNNING = 0x0072;
        private const int SPIF_UPDATEINIFILE = 0x0001;
        private const int SPIF_SENDCHANGE = 0x0002;
        private const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, StringBuilder pvParam, uint fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref bool pvParam, uint fWinIni);

        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(out bool enabled);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, int flags);

        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
        #endregion
    }
}
            
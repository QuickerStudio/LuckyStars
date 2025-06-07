using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using LuckyStars.Utils;

namespace LuckyStars.Managers.TrayManagement.Utils
{
    /// <summary>
    /// 托盘图标工具类，提供与托盘图标相关的实用方法
    /// </summary>
    public static class TrayIconUtils
    {
        /// <summary>
        /// 从嵌入资源中获取图标
        /// </summary>
        /// <param name="resourceName">资源名称</param>
        /// <returns>图标</returns>
        public static Icon GetEmbeddedIcon(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new ArgumentException("Resource not found: " + resourceName);

                return new Icon(stream);
            }
        }

        /// <summary>
        /// 获取托盘图标区域
        /// </summary>
        public static Rect GetTrayIconRect()
        {
            try
            {
                // 找到任务栏
                IntPtr taskBar = Win32Helper.FindWindow("Shell_TrayWnd", null);
                if (taskBar != IntPtr.Zero)
                {
                    // 找到通知区域
                    IntPtr trayNotify = Win32Helper.FindWindowEx(taskBar, IntPtr.Zero, "TrayNotifyWnd", null);
                    if (trayNotify != IntPtr.Zero)
                    {
                        // 获取通知区域位置
                        if (Win32Helper.GetWindowRect(trayNotify, out Win32Helper.RECT trayRect))
                        {
                            // 获取屏幕工作区
                            var workArea = SystemParameters.WorkArea;

                            // 计算托盘图标的预期位置
                            double iconX = trayRect.Left;
                            double iconY = trayRect.Top;
                            double iconWidth = trayRect.Right - trayRect.Left;
                            double iconHeight = trayRect.Bottom - trayRect.Top;

                            return new Rect(iconX, iconY, iconWidth, iconHeight);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 如果无法获取准确位置，使用默认位置
                var screen = SystemParameters.WorkArea;
                return new Rect(
                    screen.Right - 200,
                    screen.Bottom - 40,
                    32,
                    32
                );
            }

            // 如果无法获取准确位置，使用默认位置
            var defaultScreen = SystemParameters.WorkArea;
            return new Rect(
                defaultScreen.Right - 200,
                defaultScreen.Bottom - 40,
                32,
                32
            );
        }

        /// <summary>
        /// 获取状态文本
        /// </summary>
        public static string GetTimerStateText(RegistryManager.TimerState state)
        {
            return state switch
            {
                RegistryManager.TimerState.FiveMinutes => "5分钟",
                RegistryManager.TimerState.TenMinutes => "10分钟",
                RegistryManager.TimerState.TwentyMinutes => "20分钟",
                RegistryManager.TimerState.Disabled => "已停用",
                _ => "未知",
            };
        }
    }
}

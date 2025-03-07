using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LuckyStars
{
    public class WallpaperManager
    {
        // Win32 API 常量和方法
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int HWND_BOTTOM = 1;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        
        private IntPtr progmanHandle;
        private IntPtr workerWHandle;
        private IntPtr originalParent;

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string windowName);
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string windowName);
        
        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
        
        public void SetAsWallpaper(Window window)
        {
            try
            {
                // 获取窗口句柄
                IntPtr windowHandle = new WindowInteropHelper(window).Handle;
                
                // 设置窗口样式，使其不会激活
                int exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                SetWindowLong(windowHandle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
                
                // 查找桌面窗口
                progmanHandle = FindWindow("Progman", null);
                
                // 发送消息给Progman，使其创建WorkerW
                IntPtr result;
                SendMessageTimeout(progmanHandle, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out result);
                
                // 查找包含桌面图标的WorkerW窗口
                IntPtr shellDefView = IntPtr.Zero;
                workerWHandle = IntPtr.Zero;
                
                EnumWorkerW();
                
                if (workerWHandle != IntPtr.Zero)
                {
                    // 保存原始父窗口
                    originalParent = SetParent(windowHandle, workerWHandle);
                    
                    // 调整窗口位置
                    SetWindowPos(windowHandle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
                else
                {
                    MessageBox.Show("无法找到桌面壁纸窗口", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置壁纸模式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void EnumWorkerW()
        {
            IntPtr workerW = IntPtr.Zero;
            do
            {
                // 查找下一个WorkerW窗口
                workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
                
                // 检查该WorkerW是否包含SHELLDLL_DefView
                IntPtr shellDefView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDefView != IntPtr.Zero)
                {
                    // 找到桌面图标的窗口，下一个WorkerW就是我们要找的
                    workerWHandle = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
                    break;
                }
            } while (workerW != IntPtr.Zero);
        }
        
        public void RestoreWallpaper()
        {
            // 清理资源，恢复原始状态
            // 这里可以添加恢复原始壁纸的代码
        }
    }
}
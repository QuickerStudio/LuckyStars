using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

using LuckyStars.Utils;

namespace LuckyStars.Managers
{
    /// <summary>
    /// 窗口管理器，负责将应用程序窗口设置为桌面子窗口
    /// </summary>
    public class WindowManager
    {
        private readonly IntPtr _desktopHandle;
        private readonly object _lockObject = new();

        /// <summary>
        /// 初始化窗口管理器
        /// </summary>
        public WindowManager()
        {
            _desktopHandle = GetDesktopParentHandle();
            if (_desktopHandle == IntPtr.Zero)
            {
                // 如果获取桌面窗口句柄失败，尝试使用桌面窗口作为备选
                _desktopHandle = Utils.NativeMethods.GetDesktopWindow();
            }
        }

        /// <summary>
        /// 将窗口设置为桌面子窗口
        /// </summary>
        /// <param name="window">要设置的窗口</param>
        /// <returns>设置是否成功</returns>
        public bool SetupWindowAsDesktopChild(Window window)
        {
            lock (_lockObject)
            {
                if (_desktopHandle == IntPtr.Zero || window == null)
                {
                    return false;
                }

                var windowHelper = new WindowInteropHelper(window);
                if (windowHelper.Handle == IntPtr.Zero)
                {
                    return false;
                }

                // 设置父窗口
                IntPtr result = Utils.NativeMethods.SetParent(windowHelper.Handle, _desktopHandle);
                if (result == IntPtr.Zero)
                {
                    return false;
                }

                // 设置窗口样式
                Utils.NativeMethods.SetWindowLong(windowHelper.Handle, Utils.NativeMethods.GWL_STYLE, Utils.NativeMethods.WS_CHILD);
                Utils.NativeMethods.SetWindowLong(windowHelper.Handle, Utils.NativeMethods.GWL_EXSTYLE,
                    Utils.NativeMethods.WS_EX_TOOLWINDOW | Utils.NativeMethods.WS_EX_NOACTIVATE);

                // 设置窗口位置
                bool posResult = Utils.NativeMethods.SetWindowPos(
                    windowHelper.Handle,
                    new IntPtr(Utils.NativeMethods.HWND_BOTTOM),
                    0, 0, 0, 0,
                    Utils.NativeMethods.SWP_NOSIZE | Utils.NativeMethods.SWP_NOMOVE | Utils.NativeMethods.SWP_NOACTIVATE);

                return posResult;
            }
        }

        /// <summary>
        /// 重置窗口位置（用于显示设置变化后）
        /// </summary>
        /// <param name="window">要重置的窗口</param>
        /// <returns>重置是否成功</returns>
        public bool ResetWindowPosition(Window window)
        {
            if (_desktopHandle == IntPtr.Zero || window == null)
            {
                return false;
            }

            var windowHelper = new WindowInteropHelper(window);
            if (windowHelper.Handle == IntPtr.Zero)
            {
                return false;
            }

            // 重新设置父窗口
            IntPtr result = Utils.NativeMethods.SetParent(windowHelper.Handle, _desktopHandle);
            if (result == IntPtr.Zero)
            {
                return false;
            }

            // 重新设置窗口位置
            bool posResult = Utils.NativeMethods.SetWindowPos(
                windowHelper.Handle,
                new IntPtr(Utils.NativeMethods.HWND_BOTTOM),
                0, 0, 0, 0,
                Utils.NativeMethods.SWP_NOSIZE | Utils.NativeMethods.SWP_NOMOVE | Utils.NativeMethods.SWP_NOACTIVATE);

            return posResult;
        }

        /// <summary>
        /// 获取桌面窗口句柄
        /// </summary>
        /// <returns>桌面窗口句柄</returns>
        public IntPtr GetDesktopHandle()
        {
            return _desktopHandle;
        }

        /// <summary>
        /// 获取桌面父窗口句柄
        /// 这个方法尝试找到Windows桌面的正确父窗口，以便将我们的窗口设置为其子窗口
        /// </summary>
        /// <returns>桌面父窗口句柄，如果失败则返回桌面窗口</returns>
        private static IntPtr GetDesktopParentHandle()
        {
            try
            {
                // 首先尝试找到Progman窗口
                IntPtr progman = Utils.NativeMethods.FindWindow("Progman", null);
                if (progman == IntPtr.Zero)
                {
                    // 如果Progman窗口不存在，直接返回桌面窗口
                    return Utils.NativeMethods.GetDesktopWindow();
                }

                // 发送消息以创建WorkerW窗口
                const uint SPIF_SENDCHANGE = 0x0002;
                Utils.NativeMethods.SendMessageTimeout(
                    progman,
                    0x052C,  // 这是一个特殊消息，用于创建WorkerW窗口
                    IntPtr.Zero,
                    IntPtr.Zero,
                    SPIF_SENDCHANGE,
                    1000,
                    out _);

                // 查找WorkerW窗口
                IntPtr workerW1 = IntPtr.Zero;
                int retryCount = 0;
                const int MAX_RETRY = 3;

                while (retryCount < MAX_RETRY)
                {
                    // 查找所有WorkerW窗口
                    while ((workerW1 = Utils.NativeMethods.FindWindowEx(IntPtr.Zero, workerW1, "WorkerW", null)) != IntPtr.Zero)
                    {
                        // 查找SHELLDLL_DefView子窗口
                        IntPtr defView = Utils.NativeMethods.FindWindowEx(workerW1, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (defView != IntPtr.Zero)
                        {
                            // 找到包含桌面图标的WorkerW窗口后，查找下一个WorkerW窗口
                            IntPtr workerW2 = Utils.NativeMethods.FindWindowEx(IntPtr.Zero, workerW1, "WorkerW", null);
                            if (workerW2 != IntPtr.Zero)
                            {
                                return workerW2;
                            }
                        }
                    }

                    // 如果没有找到，重试
                    retryCount++;
                    if (retryCount < MAX_RETRY)
                    {
                        Thread.Sleep(500);
                        Utils.NativeMethods.SendMessageTimeout(
                            progman,
                            0x052C,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            SPIF_SENDCHANGE,
                            1000,
                            out _);
                    }
                }

                // 如果找不到WorkerW窗口，使用Progman
                return progman;
            }
            catch
            {
                // 如果发生任何异常，返回桌面窗口作为备选
                return Utils.NativeMethods.GetDesktopWindow();
            }
        }
    }
}

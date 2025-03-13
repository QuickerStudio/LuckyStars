#nullable enable

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace LuckyStars
{
    public partial class App : Application
    {
        // 窗口样式常量
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_CHILD = 0x40000000;
        private const int HWND_BOTTOM = 1;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        // 壁纸恢复API
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        // 指定正确的库导入
        [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static partial IntPtr FindWindow(
            [MarshalAs(UnmanagedType.LPWStr)] string? lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpWindowName);

        [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static partial IntPtr FindWindowEx(
            IntPtr parentHandle,
            IntPtr childAfter,
            [MarshalAs(UnmanagedType.LPWStr)] string? className,
            [MarshalAs(UnmanagedType.LPWStr)] string? windowTitle);

        [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static partial int SystemParametersInfo(
            int uAction,
            int uParam,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpvParam,
            int fuWinIni);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [LibraryImport("user32.dll", EntryPoint = "SetParent", SetLastError = true)]
        private static partial IntPtr SetParent(
            IntPtr hWndChild,
            IntPtr hWndNewParent);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        private static partial int SetWindowLong(
            IntPtr hWnd,
            int nIndex,
            int dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "GetDesktopWindow", SetLastError = true)]
        private static partial IntPtr GetDesktopWindow();

        [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
        private static partial uint SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        private MainWindow? mainWindow;
        private IntPtr _desktopHandle = IntPtr.Zero;
        private static Mutex? _mutex;
        private static readonly object _mutexLock = new();
        private static bool _mutexAcquired = false;

        protected override void OnStartup(StartupEventArgs e)
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
                        Current.Shutdown();
                        return;
                    }
                }

                base.OnStartup(e);

                // 获取桌面父窗口句柄
                _desktopHandle = GetDesktopParentHandle();
                if (_desktopHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("无法获取桌面窗口句柄");
                }

                mainWindow = new MainWindow();

                mainWindow.SourceInitialized += (s, e2) =>
                {
                    var mainHelper = new WindowInteropHelper(mainWindow);

                    if (mainHelper.Handle == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("主窗口句柄无效");
                    }

                    // 附加到桌面
                    if (SetParent(mainHelper.Handle, _desktopHandle) == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("无法设置父窗口");
                    }

                    // 设置窗口样式
                    _ = SetWindowLong(mainHelper.Handle, GWL_STYLE, WS_CHILD);
                    _ = SetWindowLong(mainHelper.Handle, GWL_EXSTYLE, WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

                    // 设置窗口位置
                    if (!SetWindowPos(
                        mainHelper.Handle,
                        new IntPtr(HWND_BOTTOM),
                        0,
                        0,
                        0,
                        0,
                        SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE))
                    {
                        throw new InvalidOperationException("无法设置窗口层级");
                    }
                };

                mainWindow.Show();

                // 托盘程序窗口
                var trayHost = new TrayIconHostWindow(mainWindow, new System.Windows.Forms.Timer());
                trayHost.Show();

                // 注册窗口重置事件
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序启动失败: {ex.Message}");
                Current.Shutdown();
            }
        }

        // 将此方法标记为 static。它不使用实例字段或方法
        private static IntPtr GetDesktopParentHandle()
        {
            try
            {
                IntPtr progman = FindWindow("Progman", null);
                if (progman == IntPtr.Zero)
                {
                    MessageBox.Show("无法找到Progman窗口，尝试直接使用桌面窗口");
                    return GetDesktopWindow();
                }

                _ = SendMessageTimeout(
                    progman,
                    0x052C,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    1000,
                    out IntPtr result);

                // 内联变量声明 (IDE0018)
                IntPtr workerW1 = IntPtr.Zero, workerW2 = IntPtr.Zero;
                int retryCount = 0;

                while (retryCount < 3)
                {
                    while ((workerW1 = FindWindowEx(IntPtr.Zero, workerW1, "WorkerW", null)) != IntPtr.Zero)
                    {
                        IntPtr defView = FindWindowEx(workerW1, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (defView != IntPtr.Zero)
                        {
                            workerW2 = FindWindowEx(IntPtr.Zero, workerW1, "WorkerW", null);
                            if (workerW2 != IntPtr.Zero)
                            {
                                return workerW2;
                            }
                        }
                    }

                    retryCount++;
                    if (retryCount < 3)
                    {
                        Thread.Sleep(500);
                        _ = SendMessageTimeout(
                            progman,
                            0x052C,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            0,
                            1000,
                            out result);
                    }
                }

                return progman;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取桌面句柄失败: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            try
            {
                if (mainWindow != null && _desktopHandle != IntPtr.Zero)
                {
                    var mainHelper = new WindowInteropHelper(mainWindow);

                    if (SetParent(mainHelper.Handle, _desktopHandle) == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("无法重新设置父窗口");
                    }

                    SetWindowPos(
                        mainHelper.Handle,
                        new IntPtr(HWND_BOTTOM),
                        0,
                        0,
                        0,
                        0,
                        SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"窗口重置失败: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 静态方法调用
                RestoreWallpaper();
                mainWindow?.Close();
                ReleaseResources();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"程序退出时发生错误: {ex.Message}");
            }
            finally
            {
                base.OnExit(e);
            }
        }

        // 将此方法标记为 static（不访问实例字段）
        private static void RestoreWallpaper()
        {
            try
            {
                int retryCount = 0;
                while (retryCount < 3)
                {
                    if (SystemParametersInfo(
                        SPI_SETDESKWALLPAPER,
                        0,
                        null,
                        SPIF_UPDATEINIFILE | SPIF_SENDCHANGE) != 0)
                    {
                        Debug.WriteLine("壁纸恢复成功");
                        break;
                    }
                    retryCount++;
                    Thread.Sleep(500);
                }

                if (retryCount >= 3)
                {
                    Debug.WriteLine("壁纸恢复失败");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"壁纸恢复失败: {ex.Message}");
            }
        }

        // 将此方法也标记为 static（仅访问静态字段/方法）
        private static void ReleaseResources()
        {
            lock (_mutexLock)
            {
                try
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
                        Debug.WriteLine("Mutex已释放");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"释放Mutex失败: {ex.Message}");
                }
            }

            TerminateBackgroundThreads();
        }

        // 已标记为 static
        private static void TerminateBackgroundThreads()
        {
            try
            {
                var threads = Process.GetCurrentProcess().Threads;
                foreach (ProcessThread thread in threads)
                {
                    try
                    {
                        if (thread.Id != Environment.CurrentManagedThreadId)
                        {
                            Debug.WriteLine($"终止线程: {thread.Id}");
                            thread.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"终止线程失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查线程失败: {ex.Message}");
            }
        }
    }
}
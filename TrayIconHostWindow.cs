using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Diagnostics;

namespace LuckyStars
{
    public class TrayIconHostWindow : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly MainWindow _mainWindow;
        private bool _isWebViewActive = true;
        private bool _needReloadContent = false;
        private bool _isWaitingForSecondClick = false;
        private System.Windows.Forms.Timer? _clickTimer;  // 添加 ? 表示可空
        private const int DoubleClickTime = 300;
        private TrayDropWindow? _dropWindow;
        private DispatcherTimer _monitorTimer;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int VK_LBUTTON = 0x01;

        public TrayIconHostWindow(MainWindow mainWindow, System.Windows.Forms.Timer mouseClickTimer)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            _notifyIcon = new NotifyIcon
            {
                Icon = GetEmbeddedIcon("LuckyStars.Resource.UI.app.ico"),
                Visible = true,
                Text = "壁纸切换工具"
            };

            // 初始化计时器，但不启动
            _clickTimer = mouseClickTimer;
            _clickTimer.Interval = DoubleClickTime;
            _clickTimer.Tick += OnClickTimerElapsed;

            // 鼠标按下事件
            _notifyIcon.MouseDown += (sender, e) =>
            {
                // 同时按下鼠标左右键：调用 ExitApplication() 清理图标并退出程序
                if ((Control.MouseButtons & MouseButtons.Left) != 0 &&
                    (Control.MouseButtons & MouseButtons.Right) != 0)
                {
                    ExitApplication();
                    return;
                }

                // 右键点击：循环切换定时器状态
                if (e.Button == MouseButtons.Right)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow.TimerState newState = _mainWindow.CycleTimerState();
                        ShowTimerStateNotification(newState);
                    });
                }
                // 左键点击处理（延迟处理以区分单击和双击）
                else if (e.Button == MouseButtons.Left)
                {
                    if (_isWaitingForSecondClick)
                    {
                        // 这是双击的第二次点击，停止计时器
                        _clickTimer.Stop();
                        _isWaitingForSecondClick = false;

                        // 在UI线程上执行双击操作
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ToggleWebView();
                        });
                    }
                    else
                    {
                        // 这是第一次点击，启动计时器等待可能的第二次点击
                        _isWaitingForSecondClick = true;
                        _clickTimer.Start();
                    }
                }
            };

            InitializeDropWindow();
            InitializeMonitor();
        }

        private static Icon GetEmbeddedIcon(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new ArgumentException("Resource not found: " + resourceName);

                return new Icon(stream);
            }
        }

        private static Bitmap GetEmbeddedBitmap(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new ArgumentException("Resource not found: " + resourceName);

                return new Bitmap(stream);
            }
        }

        // 修改 OnClickTimerElapsed 方法的签名以匹配目标委托的 null 性特性
        private void OnClickTimerElapsed(object? sender, EventArgs e)
        {
            _clickTimer?.Stop();
            _isWaitingForSecondClick = false;

            // 执行单击操作：切换下一张壁纸
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _mainWindow.NextImage();
            });
        }

        // 显示定时器状态并更新托盘图标的提示文本
        private void ShowTimerStateNotification(MainWindow.TimerState state)
        {
            // 更新托盘图标的提示文本
            _notifyIcon.Text = $"当前壁纸切换频率: {GetTimerStateText(state)}";
        }

        // 辅助方法获取状态文本
        private static string GetTimerStateText(MainWindow.TimerState state)
        {
            return state switch
            {
                MainWindow.TimerState.FiveMinutes => "5分钟",
                MainWindow.TimerState.TenMinutes => "10分钟",
                MainWindow.TimerState.TwentyMinutes => "20分钟",
                MainWindow.TimerState.Disabled => "已停用",
                _ => "未知",
            };
        }

        private async void ToggleWebView()
        {
            if (_isWebViewActive)
            {
                // 隐藏
                await _mainWindow.GetWebView().CoreWebView2.ExecuteScriptAsync(@"
                    document.body.style.display='none';
                    document.querySelectorAll('video, audio').forEach(media => media.pause());
                ");

                _mainWindow.GetWebView().CoreWebView2.Navigate("about:blank");
                _needReloadContent = true;
                _isWebViewActive = false;
            }
            else
            {
                // 恢复
                _mainWindow.SetWebViewVisibility(true);

                if (_needReloadContent)
                {
                    _mainWindow.LoadTestHtml();
                    _needReloadContent = false;
                }
                else
                {
                    await _mainWindow.GetWebView().CoreWebView2.ExecuteScriptAsync("document.body.style.display='block'");
                }

                _mainWindow.ApplyFullScreenToWebView();
                _isWebViewActive = true;
            }
        }

        private async System.Threading.Tasks.Task ToggleAudio()
        {
            // 暂停或播放所有音视频
            await _mainWindow.GetWebView().CoreWebView2.ExecuteScriptAsync(@"
                (() => {
                    const mediaElems = document.querySelectorAll('video, audio');
                    for (const m of mediaElems) {
                        if (m.paused) {
                            m.play();
                        } else {
                            m.pause();
                        }
                    }
                })();
            ");
        }

        public void Show()
        {
            _notifyIcon.Visible = true;
        }

        public void Hide()
        {
            _notifyIcon.Visible = false;
        }

        public void Dispose()
        {
            if (_clickTimer != null)
            {
                _clickTimer.Stop();
                _clickTimer.Dispose();
                _clickTimer = null;
            }

            _notifyIcon.Dispose();
        }

        private void InitializeDropWindow()
        {
            _dropWindow = new TrayDropWindow();
            _dropWindow.FileDropped += OnFileDropped;
        }

        private void InitializeMonitor()
        {
            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            _monitorTimer.Tick += MonitorTimer_Tick;
            _monitorTimer.Start();
        }

        private void MonitorTimer_Tick(object? sender, EventArgs e)
        {
            // 检查是否有拖放操作（鼠标左键按下）
            if ((GetKeyState(VK_LBUTTON) & 0x8000) != 0)
            {
                // 获取鼠标位置
                GetCursorPos(out POINT cursorPos);

                // 获取托盘区域
                var trayRect = GetTrayIconRect();

                // 如果鼠标在托盘区域附近
                if (IsPointNearRect(new System.Windows.Point(cursorPos.X, cursorPos.Y), trayRect))
                {
                    ShowDropWindow(trayRect);
                }
                else
                {
                    _dropWindow?.Hide();
                }
            }
            else
            {
                _dropWindow?.Hide();
            }
        }

        private bool IsPointNearRect(System.Windows.Point point, System.Windows.Rect rect)
        {
            // 扩大检测区域
            var expandedRect = new System.Windows.Rect(
                rect.X - 20,
                rect.Y - 20,
                rect.Width + 40,
                rect.Height + 40
            );

            return expandedRect.Contains(point);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        private System.Windows.Rect GetTrayIconRect()
        {
            try
            {
                // 找到任务栏
                IntPtr taskBar = FindWindow("Shell_TrayWnd", null);
                if (taskBar != IntPtr.Zero)
                {
                    // 找到通知区域
                    IntPtr trayNotify = FindWindowEx(taskBar, IntPtr.Zero, "TrayNotifyWnd", null);
                    if (trayNotify != IntPtr.Zero)
                    {
                        // 获取通知区域位置
                        if (GetWindowRect(trayNotify, out RECT trayRect))
                        {
                            return new System.Windows.Rect(trayRect.Left, trayRect.Top, trayRect.Right - trayRect.Left, trayRect.Bottom - trayRect.Top);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 处理获取托盘位置错误
            }

            // 如果无法获取准确位置，使用默认位置
            var screen = SystemParameters.WorkArea;
            return new System.Windows.Rect(
                screen.Right - 200,
                screen.Bottom - 40,
                32,
                32
            );
        }

        private void ShowDropWindow(System.Windows.Rect trayRect)
        {
            if (_dropWindow != null && !_dropWindow.IsVisible)
            {
                // 根据任务栏高度调整窗口大小
                var taskbarHeight = SystemParameters.WorkArea.Bottom - SystemParameters.PrimaryScreenHeight;

                _dropWindow.Left = trayRect.X;
                _dropWindow.Top = trayRect.Y;
                _dropWindow.Width = 200; // 设置一个合适的宽度，足够覆盖图标区域
                _dropWindow.Height = Math.Abs(taskbarHeight); // 使用任务栏的实际高度

                _dropWindow.Show();
                _dropWindow.Activate();
            }
        }

        private void OnFileDropped(string[] files)
        {
            var supportedExtensions = new[]{
    ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff",  // 图片
    ".mp4", ".mov", ".avi", ".mkv", ".webm",           // 视频
    ".html", ".htm"                                    // HTML
        };
            var targetFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "LuckyStarsWallpaper");

            int successCount = 0;
            int failCount = 0;

            foreach (string file in files)
            {
                try
                {
                    string extension = Path.GetExtension(file).ToLowerInvariant();

                    // 检查文件类型是否支持
                    if (!supportedExtensions.Contains(extension))
                    {
                        failCount++;
                        continue;
                    }

                    // 确保目标文件夹存在
                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    // 复制文件到目标文件夹
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(targetFolder, fileName);
                    File.Copy(file, destFile, true);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;

                }
            }

            // 显示通知
            string message = successCount > 0
                ? $"成功接收 {successCount} 个文件" + (failCount > 0 ? $"，失败 {failCount} 个" : "")
                : "没有成功接收任何文件";

            _notifyIcon?.ShowBalloonTip(
                3000, // 显示时间（毫秒）
                "文件处理结果",
                message,
                successCount > 0 ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        private void ExitApplication()
        {
            // 先清理通知图标
            Dispose();

            // 退出程序
            System.Windows.Application.Current.Shutdown();
        }
    }
}

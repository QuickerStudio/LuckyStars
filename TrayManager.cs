using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Point = System.Windows.Point;
using DragEventArgs = System.Windows.DragEventArgs;
using Control = System.Windows.Forms.Control;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;

namespace LuckyStars
{
    public class TrayManager : IDisposable
    {
        // 删除原有的 TaskbarIcon，使用 NotifyIcon 替代
        private readonly NotifyIcon _notifyIcon;
        private readonly MainWindow _mainWindow;
        private TrayDropWindow? _dropWindow;
        private DetectionZoneWindow? _detectionZoneWindow;
        private DispatcherTimer _monitorTimer;
        private Point _lastCursorPos;

        // 添加 TrayIconHostWindow 中的字段
        private bool _isWebViewActive = true;
        private bool _needReloadContent = false;
        private bool _isWaitingForSecondClick = false;
        private System.Windows.Forms.Timer? _clickTimer;
        private const int DoubleClickTime = 300;
        private MouseButtons _lastMouseButton = MouseButtons.None;

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

        private DateTime _lastClickTime;
        private System.Windows.Point _lastClickPos;
        private bool _isClickHandling;
        private const double DoubleClickTimeThreshold = 500; // 双击时间阈值（毫秒）
        private const double ClickThreshold = 5; // 点击判定阈值

        private bool _isMonitoringActive;
        private Rect _monitoringArea;
        private const double MONITOR_INTERVAL_INACTIVE = 500; // 非活动状态检查间隔(ms)
        private const double MONITOR_INTERVAL_ACTIVE = 8;    // 活动状态检查间隔(ms)，提高采样率
        private const double DETECTION_ZONE_HEIGHT = 10;     // 检测区高度
        private const double DETECTION_ZONE_OFFSET = 5;     // 检测区与托盘区的距离
        private bool _isDraggingFromOutside = false;         // 标记是否从外部拖拽
        private System.Windows.Point _lastDetectionZoneEntry;// 记录进入检测区的位置
        private bool _wasInDetectionZone;                    // 记录上一次是否在检测区内

        public TrayManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            
            // 初始化 NotifyIcon
            _notifyIcon = new NotifyIcon
            {
                Icon = GetEmbeddedIcon("LuckyStars.Resource.UI.app.ico"),
                Visible = true,
                Text = "壁纸切换工具"
            };

            // 初始化计时器
            _clickTimer = new System.Windows.Forms.Timer
            {
                Interval = DoubleClickTime
            };
            _clickTimer.Tick += OnClickTimerElapsed;

            // 设置鼠标事件处理
            _notifyIcon.MouseDown += (sender, e) =>
            {
                if ((Control.MouseButtons & MouseButtons.Left) != 0 &&
                    (Control.MouseButtons & MouseButtons.Right) != 0)
                {
                    ExitApplication();
                    return;
                }

                _lastMouseButton = e.Button;

                if (_isWaitingForSecondClick)
                {
                    _clickTimer.Stop();
                    _isWaitingForSecondClick = false;

                    // 执行双击操作
                    if (e.Button == MouseButtons.Left)
                    {
                        // 左键双击：隐藏/显示WebView
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ToggleWebView();
                        });
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        // 右键双击：切换显示时长
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ToggleDisplayDuration();
                        });
                    }
                }
                else
                {
                    _isWaitingForSecondClick = true;
                    _clickTimer.Start();
                }
            };

            InitializeDropWindow();
            InitializeMonitor();
            UpdateDropWindowPosition(); // 启动时更新透明窗口位置

            // 初始化媒体播放器
            _mainWindow.ShowMedia(); // 确保媒体播放器已初始化
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

        // 计时器回调函数，处理单击事件
        private void OnClickTimerElapsed(object? sender, EventArgs e)
        {
            _clickTimer?.Stop();
            _isWaitingForSecondClick = false;

            // 根据最后按下的鼠标按钮执行相应操作
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (_lastMouseButton == MouseButtons.Left)
                {
                    // 左键单击：隐藏视频播放器、切换图片、静音视频播放器
                    _mainWindow.MuteVideoPlayer();
                    _mainWindow.NextImage();
                }
                else if (_lastMouseButton == MouseButtons.Right)
                {
                    // 右键单击：显示视频播放器、切换视频、设置音量20%
                    _mainWindow.UnmuteVideoPlayer();
                    _mainWindow.NextVideo();
                }
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

        private void InitializeDropWindow()
        {
            _dropWindow = new TrayDropWindow();
            _dropWindow.Hide(); // 确保窗口在启动时不可见
            _dropWindow.FileDropped += OnFileDropped;

            // 初始化检测区窗口
            _detectionZoneWindow = new DetectionZoneWindow();
            _detectionZoneWindow.Hide(); // 初始时隐藏
            // 添加拖拽事件处理
            _detectionZoneWindow.DragEnter += DetectionZoneWindow_DragEnter;
            _detectionZoneWindow.DragLeave += DetectionZoneWindow_DragLeave;
            _detectionZoneWindow.FileDropped += OnFileDropped; // 连接文件拖放事件处理器
        }

        private void InitializeMonitor()
        {
            // 初始化监控区域，只在启动时设置一次
            UpdateMonitorArea();
        }

        private void UpdateMonitorArea()
        {
            var trayRect = GetTrayIconRect();
            // 创建一个位于托盘区上方的检测区，确保不覆盖托盘区
            _monitoringArea = new Rect(
                trayRect.X - 50,  // 向左扩展50像素
                trayRect.Y - DETECTION_ZONE_OFFSET - DETECTION_ZONE_HEIGHT,  // 在托盘区上方创建检测区，保持一定距离
                trayRect.Width + 100,  // 向右扩展100像素
                DETECTION_ZONE_HEIGHT  // 检测区高度
            );

            // 更新检测区窗口位置和大小
            _detectionZoneWindow.Left = _monitoringArea.X;
            _detectionZoneWindow.Top = _monitoringArea.Y;
            _detectionZoneWindow.Width = _monitoringArea.Width;
            _detectionZoneWindow.Height = _monitoringArea.Height;

            // 确保窗口显示但不获取焦点
            if (!_detectionZoneWindow.IsVisible)
            {
                _detectionZoneWindow.Show();
            }
        }

        private void DetectionZoneWindow_DragEnter(object sender, DragEventArgs e)
        {
            // 检查是否拖拽的是文件
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 允许拖放操作
                e.Effects = DragDropEffects.Copy;
                _isDraggingFromOutside = true;

                // 更新并显示拖放窗口
                UpdateDropWindowPosition(); // 拖放前更新一次位置
                var trayRect = GetTrayIconRect();
                ShowDropWindow(trayRect);
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void DetectionZoneWindow_DragLeave(object sender, DragEventArgs e)
        {
            // 拖拽离开检测区但不取消拖放窗口，直到文件接收完成
            e.Handled = true;
        }

        private bool HandleTrayIconClick(System.Windows.Point currentPos, bool isLeftButtonDown)
        {
            var trayRect = GetTrayIconRect();
            bool isInTrayArea = IsPointNearRect(currentPos, trayRect);

            if (isLeftButtonDown)
            {
                if (!_isClickHandling)
                {
                    _isClickHandling = true;
                    _lastClickPos = currentPos;
                    _lastClickTime = DateTime.Now;
                    return true;
                }
                else
                {
                    if (isInTrayArea)
                    {
                        // 在托盘区域内移动，不显示窗口
                        return true;
                    }
                    else
                    {
                        // 在托盘区域外移动，允许显示窗口
                        _isClickHandling = false;
                        return false;
                    }
                }
            }
            else
            {
                if (_isClickHandling)
                {
                    if (isInTrayArea)
                    {
                        var timeSinceLastClick = (DateTime.Now - _lastClickTime).TotalMilliseconds;
                        if (timeSinceLastClick <= DoubleClickTimeThreshold)
                        {
                            OnDoubleClick();
                        }
                        else
                        {
                            OnSingleClick();
                        }
                    }
                    _isClickHandling = false;
                }
            }

            return false;
        }

        private void OnSingleClick()
        {
            // 添加单击处理逻辑
        }

        private void OnDoubleClick()
        {
            _mainWindow.Show(); // 显示主窗口
        }

        private bool IsPointNearRect(System.Windows.Point point, Rect rect)
        {
            // 扩大检测区域
            var expandedRect = new Rect(
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

        private Rect GetTrayIconRect()
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
                        RECT trayRect;
                        if (GetWindowRect(trayNotify, out trayRect))
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

        private void ShowDropWindow(Rect trayRect)
        {
            if (_dropWindow != null && !_dropWindow.IsVisible)
            {
                _dropWindow.Left = trayRect.X;
                _dropWindow.Top = trayRect.Y;
                _dropWindow.Width = trayRect.Width;
                _dropWindow.Height = trayRect.Height;

                _dropWindow.Show();
                _dropWindow.Activate();
            }
        }

        public void UpdateDropWindowPosition()
        {
            UpdateMonitorArea(); // 更新监控区域
            var trayRect = GetTrayIconRect();
            // 仅更新位置，不显示窗口
            if (_dropWindow != null)
            {
                _dropWindow.Left = trayRect.X;
                _dropWindow.Top = trayRect.Y;
                _dropWindow.Width = trayRect.Width;
                _dropWindow.Height = trayRect.Height;
            }
        }

        private void OnFileDropped(string[] files)
        {
            var supportedExtensions = new[]{
                ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff",  // 图片
                ".mp4", ".mov", ".avi", ".mkv", ".webm",           // 视频
                ".html", ".htm" ,                                  // HTML            
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
                catch (Exception)
                {
                    failCount++;
                }
            }

            // 如果窗口是隐藏的，显示窗口
            if (!_mainWindow.IsVisible)
            {
                _mainWindow.Show();
            }

            // 显示通知
            string message = successCount > 0
                ? $"成功接收 {successCount} 个文件" + (failCount > 0 ? $"，失败 {failCount} 个" : "")
                : "没有成功接收任何文件";

            _notifyIcon.ShowBalloonTip(3000, "文件处理结果", message, successCount > 0 ? ToolTipIcon.Info : ToolTipIcon.Error);

            // 文件接收完成后，隐藏拖放窗口并重置拖拽状态
            _dropWindow?.Hide();
            _isDraggingFromOutside = false;
        }

        // 添加 ToggleDisplayDuration 方法
        private void ToggleDisplayDuration()
        {
            // 切换图片和视频壁纸的显示时长
            MainWindow.TimerState newState = _mainWindow.CycleTimerState();
            
            // 使用气泡通知代替更新托盘图标提示文本
            _notifyIcon.ShowBalloonTip(
                1000,  // 显示1秒
                "壁纸切换频率",
                $"已切换为: {GetTimerStateText(newState)}",
                ToolTipIcon.Info
            );
        }

        // 添加 ExitApplication 方法
        private void ExitApplication()
        {
            // 先清理通知图标
            Dispose();

            // 退出程序
            System.Windows.Application.Current.Shutdown();
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

            _notifyIcon?.Dispose();
            _dropWindow?.Close();
            _detectionZoneWindow?.Close();
        }
    }

    // 检测区可视化窗口
    public class DetectionZoneWindow : Window
    {
        public DetectionZoneWindow()
        {
            // 设置窗口样式
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;

            // 创建内容
            var grid = new Grid();

            // 添加可视化边框
            var border = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(80, 255, 0, 0)), // 更透明的红色
                BorderBrush = System.Windows.Media.Brushes.Red,
                BorderThickness = new Thickness(1)
            };
            grid.Children.Add(border);

            Content = grid;

            // 确保窗口不获取焦点但可以接收拖放
            Focusable = false;

            // 允许窗口接收拖放操作
            AllowDrop = true;

            // 注册事件处理器以允许处理拖放事件
            DragEnter += OnDragEnter;
            DragLeave += OnDragLeave;
            DragOver += OnDragOver;
            Drop += OnDrop;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            // 确保事件能传递到 TrayManager 中注册的处理程序
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            // 保持拖放效果有效
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            // 转发文件到下方的拖放窗口
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 获取拖放的文件
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // 触发自定义事件
                FileDropped?.Invoke(files);
            }
            e.Handled = true;
        }

        // 声明自定义事件
        public event Action<string[]> FileDropped;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 使用WS_EX_LAYERED来使窗口半透明但可以接收拖放
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var extendedStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);

            // 设置窗口风格 - WS_EX_LAYERED允许透明度，但仍然允许拖放
            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, extendedStyle | Win32.WS_EX_LAYERED);

            // 设置窗口透明度 - 128是半透明
            Win32.SetLayeredWindowAttributes(hwnd, 0, 180, Win32.LWA_ALPHA);
        }
    }

    // Win32 API 辅助类
    public static class Win32
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int LWA_ALPHA = 0x00000002;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    }
}
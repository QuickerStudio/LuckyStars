using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace LuckyStars
{
    public static class SystemTray
    {
        private static NotifyIcon _notifyIcon;
        private static MainWindow _mainWindow;
        private static Window _catcherWindow;

        public static void Initialize(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            // 创建系统托盘图标
            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
                Text = "LuckyStars 壁纸",
                Visible = true
            };

            // 创建托盘菜单
            var contextMenu = new ContextMenuStrip();

            var openMenuItem = new ToolStripMenuItem("打开");
            openMenuItem.Click += (sender, e) => ShowMainWindow();

            var exitMenuItem = new ToolStripMenuItem("退出");
            exitMenuItem.Click += (sender, e) => ExitApplication();

            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            // 注册托盘图标事件，不再使用Click事件
            _notifyIcon.MouseMove += NotifyIcon_MouseMove;

            // 创建Catcher窗口
            CreateCatcherWindow();
        }

        private static void CreateCatcherWindow()
        {
            _catcherWindow = new Window
            {
                Width = 150,
                Height = 60,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0, 0, 0)),
                ShowInTaskbar = false,
                Topmost = true,
                ResizeMode = ResizeMode.NoResize,
                Title = "拖放文件到这里"
            };

            // 添加文件拖放功能
            _catcherWindow.AllowDrop = true;
            _catcherWindow.DragEnter += Catcher_DragEnter;
            _catcherWindow.DragLeave += Catcher_DragLeave;
            _catcherWindow.Drop += Catcher_Drop;

            // 添加文本提示
            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = "拖放文件到这里设置为壁纸",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.White
            };

            var border = new System.Windows.Controls.Border
            {
                BorderBrush = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Child = textBlock
            };

            _catcherWindow.Content = border;

            // 初始时隐藏
            _catcherWindow.Visibility = Visibility.Collapsed;
        }

        private static void NotifyIcon_MouseMove(object sender, MouseEventArgs e)
        {
            // 获取当前拖拽状态
            if (System.Windows.Forms.DragDropEffects.None != System.Windows.Forms.DragDropEffects.Copy)
            {
                // 显示Catcher窗口
                ShowCatcherWindow();
            }
        }

        private static void ShowCatcherWindow()
        {
            if (_catcherWindow == null || _catcherWindow.Visibility == Visibility.Visible)
                return;

            // 获取托盘图标位置
            System.Drawing.Point cursorPos = System.Windows.Forms.Cursor.Position;

            // 设置窗口位置（在托盘图标上方）
            _catcherWindow.Left = cursorPos.X - _catcherWindow.Width / 2;
            _catcherWindow.Top = cursorPos.Y - _catcherWindow.Height - 5;

            // 显示窗口
            _catcherWindow.Visibility = Visibility.Visible;

            // 如果用户没有拖放操作，3秒后自动隐藏
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                // 如果没有拖拽操作，隐藏窗口
                if (!IsDraggingOver)
                {
                    _catcherWindow.Visibility = Visibility.Collapsed;
                }
                timer.Stop();
            };
            timer.Start();
        }

        private static bool IsDraggingOver = false;

        private static void Catcher_DragEnter(object sender, DragEventArgs e)
        {
            IsDraggingOver = true;

            // 改变背景色，提供视觉反馈
            if (_catcherWindow.Content is System.Windows.Controls.Border border)
            {
                border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 120, 215));
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string extension = System.IO.Path.GetExtension(files[0]).ToLower();
                    if (extension == ".html" || extension == ".htm" ||
                        extension == ".png" || extension == ".jpg" ||
                        extension == ".jpeg" || extension == ".gif" ||
                        extension == ".bmp")
                    {
                        e.Effects = DragDropEffects.Copy;
                        return;
                    }
                }
            }
            e.Effects = DragDropEffects.None;
        }

        private static void Catcher_DragLeave(object sender, DragEventArgs e)
        {
            IsDraggingOver = false;

            // 恢复原始背景
            if (_catcherWindow.Content is System.Windows.Controls.Border border)
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private static void Catcher_Drop(object sender, DragEventArgs e)
        {
            IsDraggingOver = false;

            // 恢复原始背景
            if (_catcherWindow.Content is System.Windows.Controls.Border border)
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 获取拖放的文件
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length > 0)
                {
                    // 处理第一个文件
                    _mainWindow.PlayWallpaper(files[0]);
                }
            }

            // 隐藏Catcher窗口
            _catcherWindow.Visibility = Visibility.Collapsed;
        }

        private static void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                // 显示控制面板或其他交互方式
                // 在这里可以添加显示控制面板的代码
            }
        }

        private static void ExitApplication()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        // 添加全局钩子以检测是否有文件被拖动
        public static void SetupDragDropMonitoring()
        {
            // 这里需要实现检测全局拖放操作的功能
            // 可以使用低级键盘/鼠标钩子或者其他系统API
            // 为简化实现，这里使用一个定时器模拟检测
            System.Windows.Threading.DispatcherTimer dragMonitorTimer = new System.Windows.Threading.DispatcherTimer();
            dragMonitorTimer.Interval = TimeSpan.FromMilliseconds(100);
            dragMonitorTimer.Tick += (s, e) => {
                // 检测是否有拖动操作
                if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Left)
                {
                    // 可以在这里添加更复杂的检测逻辑
                    ShowCatcherWindow();
                }
            };
            dragMonitorTimer.Start();
        }
    }
}
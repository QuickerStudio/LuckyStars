using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using LuckyStars.Utils;

namespace LuckyStars.UI
{
    /// <summary>
    /// 托盘拖放窗口，用于接收文件拖放操作
    /// </summary>
    public class TrayDropWindow : Window
    {
        /// <summary>
        /// 文件拖放事件
        /// </summary>
        public event Action<string[]>? FileDropped;

        /// <summary>
        /// 用于霓虹灯效果的计时器
        /// </summary>
        private readonly DispatcherTimer _neonTimer = new();

        /// <summary>
        /// 随机数生成器
        /// </summary>
        private readonly Random _random = new();

        /// <summary>
        /// 初始化托盘拖放窗口
        /// </summary>
        public TrayDropWindow()
        {
            // 基本窗口设置
            AllowDrop = true;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Topmost = true;

            // 设置半透明背景
            Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));

            // 添加霓虹灯边框效果
            BorderBrush = new SolidColorBrush(Colors.Red);
            BorderThickness = new Thickness(2);

            // 设置霓虹灯效果计时器
            _neonTimer.Interval = TimeSpan.FromMilliseconds(100); // 每100毫秒变化一次颜色
            _neonTimer.Tick += NeonTimer_Tick;
            _neonTimer.Start();

            // 注册事件
            DragEnter += OnDragEnter;
            DragOver += OnDragOver;
            Drop += OnDrop;

            // 添加调试信息标签
            var debugLabel = new TextBlock
            {
                Text = "拖放区域",
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Content = debugLabel;
        }

        /// <summary>
        /// 处理拖拽进入事件
        /// </summary>
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                Console.WriteLine("文件拖入");
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 处理拖拽悬停事件
        /// </summary>
        private void OnDragOver(object sender, DragEventArgs e)
        {
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

        /// <summary>
        /// 处理拖拽放下事件
        /// </summary>
        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Console.WriteLine($"收到文件：{string.Join(", ", files)}");
                FileDropped?.Invoke(files);
            }
            Hide();
        }

        /// <summary>
        /// 窗口初始化完成后设置窗口样式
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            var exStyle = Win32Helper.GetWindowLong(hwnd, Win32Helper.GWL_EXSTYLE);
            Win32Helper.SetWindowLong(hwnd, Win32Helper.GWL_EXSTYLE,
                exStyle | Win32Helper.WS_EX_TOOLWINDOW | Win32Helper.WS_EX_NOACTIVATE | Win32Helper.WS_EX_LAYERED);
        }

        /// <summary>
        /// 霓虹灯效果计时器回调
        /// </summary>
        private void NeonTimer_Tick(object? sender, EventArgs e)
        {
            // 生成随机霓虹色
            Color neonColor = GetRandomNeonColor();

            // 创建动画效果
            var colorAnimation = new ColorAnimation
            {
                To = neonColor,
                Duration = TimeSpan.FromMilliseconds(100),
                FillBehavior = FillBehavior.HoldEnd
            };

            // 应用动画到边框
            BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        /// <summary>
        /// 生成随机霓虹色
        /// </summary>
        private Color GetRandomNeonColor()
        {
            // 霓虹色通常是高饱和度、高亮度的颜色
            switch (_random.Next(6))
            {
                case 0: return Color.FromRgb(255, 0, 128);    // 霓虹粉
                case 1: return Color.FromRgb(0, 255, 255);     // 霓虹青
                case 2: return Color.FromRgb(255, 0, 255);     // 霓虹紫
                case 3: return Color.FromRgb(0, 255, 128);     // 霓虹绿
                case 4: return Color.FromRgb(255, 128, 0);     // 霓虹橙
                case 5: return Color.FromRgb(128, 0, 255);     // 霓虹蓝紫
                default: return Color.FromRgb(255, 0, 0);      // 霓虹红
            }
        }



        /// <summary>
        /// 窗口关闭时停止计时器
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _neonTimer.Stop();
        }
    }
}
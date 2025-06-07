using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using LuckyStars.Utils;

namespace LuckyStars.UI
{
    /// <summary>
    /// 检测区可视化窗口，用于检测文件拖放操作
    /// </summary>
    public class DetectionZoneWindow : Window
    {
        /// <summary>
        /// 文件拖放事件
        /// </summary>
        public event Action<string[]>? FileDropped;

        /// <summary>
        /// 初始化检测区窗口
        /// </summary>
        public DetectionZoneWindow()
        {
            // 设置窗口样式
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            // 创建内容
            var grid = new Grid();

            // 添加可视化边框
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 255, 0, 0)), // 更透明的红色
                BorderBrush = Brushes.Red,
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

        /// <summary>
        /// 处理拖拽进入事件
        /// </summary>
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

        /// <summary>
        /// 处理拖拽离开事件
        /// </summary>
        private void OnDragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// 处理拖拽悬停事件
        /// </summary>
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

        /// <summary>
        /// 处理拖拽放下事件
        /// </summary>
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

        /// <summary>
        /// 窗口初始化完成后设置透明度
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 使用WS_EX_LAYERED来使窗口半透明但可以接收拖放
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var extendedStyle = Win32Helper.GetWindowLong(hwnd, Win32Helper.GWL_EXSTYLE);

            // 设置窗口风格 - WS_EX_LAYERED允许透明度，但仍然允许拖放
            Win32Helper.SetWindowLong(hwnd, Win32Helper.GWL_EXSTYLE, extendedStyle | Win32Helper.WS_EX_LAYERED);

            // 设置窗口透明度 - 180是半透明
            Win32Helper.SetLayeredWindowAttributes(hwnd, 0, 180, Win32Helper.LWA_ALPHA);
        }
    }
}

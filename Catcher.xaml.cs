using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;

namespace LuckyStars
{
    /// <summary>
    /// Catcher.xaml 的交互逻辑 - 用于接收文件拖放
    /// </summary>
    public partial class Catcher : Window
    {
        // 拖放处理事件
        public event EventHandler<string[]> FileDropped;
        
        // 托盘图标位置
        private Point _notifyIconPosition;
        
        // 窗口动画
        private Storyboard _fadeInStoryboard;
        private Storyboard _fadeOutStoryboard;
        
        // 窗口句柄
        private IntPtr _windowHandle;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public Catcher()
        {
            InitializeComponent();
            
            // 初始化拖放
            this.AllowDrop = true;
            this.DragEnter += Catcher_DragEnter;
            this.DragLeave += Catcher_DragLeave;
            this.Drop += Catcher_Drop;
            
            // 初始化动画
            InitializeAnimations();
        }
        
        /// <summary>
        /// 窗口加载
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取窗口句柄
                _windowHandle = new WindowInteropHelper(this).Handle;
                
                // 设置窗口扩展样式
                int exStyle = GetWindowLong(_windowHandle, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW; // 添加工具窗口样式
                exStyle |= WS_EX_LAYERED;    // 添加分层窗口样式
                SetWindowLong(_windowHandle, GWL_EXSTYLE, exStyle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Catcher窗口加载时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 显示在指定位置
        /// </summary>
        public void ShowAtPosition(int x, int y)
        {
            _notifyIconPosition = new Point(x, y);
            
            // 调整窗口位置，使其居中于托盘图标位置
            this.Left = x - (this.Width / 2);
            this.Top = y - this.Height - 20; // 在托盘图标上方显示
            
            // 确保窗口在屏幕内
            EnsureWindowVisibleOnScreen();
            
            // 显示窗口
            this.Visibility = Visibility.Visible;
            
            // 播放淡入动画
            _fadeInStoryboard.Begin(this);
        }
        
        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public void HideWindow()
        {
            // 播放淡出动画
            _fadeOutStoryboard.Begin(this);
        }
        
        /// <summary>
        /// 确保窗口在屏幕内
        /// </summary>
        private void EnsureWindowVisibleOnScreen()
        {
            // 获取主屏幕工作区
            System.Drawing.Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            
            // 检查左边界
            if (this.Left < workingArea.Left)
                this.Left = workingArea.Left;
                
            // 检查右边界
            if (this.Left + this.Width > workingArea.Right)
                this.Left = workingArea.Right - this.Width;
                
            // 检查上边界
            if (this.Top < workingArea.Top)
                this.Top = workingArea.Top;
                
            // 检查下边界
            if (this.Top + this.Height > workingArea.Bottom)
                this.Top = workingArea.Bottom - this.Height;
        }
        
        /// <summary>
        /// 初始化动画
        /// </summary>
        private void InitializeAnimations()
        {
            // 创建淡入动画
            _fadeInStoryboard = new Storyboard();
            DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));
            _fadeInStoryboard.Children.Add(fadeInAnimation);
            
            // 创建淡出动画
            _fadeOutStoryboard = new Storyboard();
            DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));
            _fadeOutStoryboard.Children.Add(fadeOutAnimation);
            
            // 淡出动画完成后隐藏窗口
            _fadeOutStoryboard.Completed += (sender, e) => 
            {
                this.Visibility = Visibility.Collapsed;
            };
        }
        
        /// <summary>
        /// 拖放进入
        /// </summary>
        private void Catcher_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            // 检查是否包含文件
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
                
                // 显示高亮边框
                HighlightBorder.Visibility = Visibility.Visible;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            
            e.Handled = true;
        }
        
        /// <summary>
        /// 拖放离开
        /// </summary>
        private void Catcher_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            // 隐藏高亮边框
            HighlightBorder.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }
        
        /// <summary>
        /// 拖放释放
        /// </summary>
        private void Catcher_Drop(object sender, System.Windows.DragEventArgs e)
        {
            // 隐藏高亮边框
            HighlightBorder.Visibility = Visibility.Collapsed;
            
            // 获取文件
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                
                if (files != null && files.Length > 0)
                {
                    // 触发文件拖放事件
                    FileDropped?.Invoke(this, files);
                    
                    // 隐藏窗口
                    HideWindow();
                }
            }
            
            e.Handled = true;
        }
        
        #region Win32 API
        
        // 常量
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_LAYERED = 0x00080000;
        
        // API函数
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        #endregion
    }
}
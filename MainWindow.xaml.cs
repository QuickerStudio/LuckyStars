using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices;

namespace LuckyStars
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 窗口句柄
        private IntPtr _windowHandle;
        
        // 是否已初始化WebView
        private bool _isWebViewInitialized = false;
        
        // 当前壁纸类型
        private enum WallpaperType
        {
            None,
            Image,
            Html
        }
        
        // 当前壁纸类型
        private WallpaperType _currentType = WallpaperType.None;
        
        // 当前壁纸路径
        private string _currentWallpaperPath = null;
        
        public MainWindow()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// 窗口加载完成
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取窗口句柄
                _windowHandle = new WindowInteropHelper(this).Handle;
                
                // 设置窗口扩展样式
                SetWindowExStyle();
                
                // 初始化WebView
                await InitializeWebViewAsync();
                
                // 订阅事件
                App.Instance.WallpaperManager.WallpaperChanged += WallpaperManager_WallpaperChanged;
                App.Instance.WallpaperManager.PauseStateChanged += WallpaperManager_PauseStateChanged;
                
                // 监听DPI变化
                SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
                
                // 设置为桌面壁纸
                SetAsDesktopWallpaper();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"窗口加载时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置为桌面壁纸
        /// </summary>
        private void SetAsDesktopWallpaper()
        {
            try
            {
                // 获取Program Manager窗口
                IntPtr progman = FindWindow("Progman", null);
                
                // 发送设置壁纸消息
                IntPtr result = IntPtr.Zero;
                SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, ref result);
                
                // 查找WorkerW窗口
                IntPtr workerW = IntPtr.Zero;
                EnumWindows(new EnumWindowsProc((topHandle, topParam) =>
                {
                    IntPtr shellView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shellView != IntPtr.Zero)
                    {
                        workerW = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                        return false;
                    }
                    return true;
                }), IntPtr.Zero);
                
                if (workerW != IntPtr.Zero)
                {
                    // 设置窗口为WorkerW的子窗口
                    SetParent(_windowHandle, workerW);
                    
                    // 调整窗口大小和位置
                    SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0,
                        (int)SystemParameters.PrimaryScreenWidth,
                        (int)SystemParameters.PrimaryScreenHeight,
                        SWP_SHOWWINDOW);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置桌面壁纸时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化WebView
        /// </summary>
        private async System.Threading.Tasks.Task InitializeWebViewAsync()
        {
            try
            {
                // 创建WebView2环境
                var webView2Environment = await CoreWebView2Environment.CreateAsync(null, 
                    Path.Combine(App.Instance.AppDataDirectory, "WebView2Cache"));
                
                // 初始化WebView2
                await WallpaperWebView.EnsureCoreWebView2Async(webView2Environment);
                
                // 配置WebView2
                WallpaperWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                WallpaperWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                WallpaperWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                WallpaperWebView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                
                // 处理WebView导航完成事件
                WallpaperWebView.NavigationCompleted += WebView_NavigationCompleted;
                
                _isWebViewInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化WebView时出错: {ex.Message}");
                _isWebViewInitialized = false;
            }
        }
        
        /// <summary>
        /// WebView导航完成
        /// </summary>
        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine("WebView导航完成");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"WebView导航失败: {e.WebErrorStatus}");
            }
        }
        
        /// <summary>
        /// 壁纸变更事件处理
        /// </summary>
        private void WallpaperManager_WallpaperChanged(object sender, string wallpaperPath)
        {
            try
            {
                _currentWallpaperPath = wallpaperPath;
                
                if (string.IsNullOrEmpty(wallpaperPath))
                {
                    ClearWallpaper();
                    return;
                }
                
                // 确定壁纸类型
                string extension = Path.GetExtension(wallpaperPath).ToLowerInvariant();
                
                if (extension == ".html" || extension == ".htm")
                {
                    SetHtmlWallpaper(wallpaperPath);
                }
                else
                {
                    SetImageWallpaper(wallpaperPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"壁纸变更处理出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 暂停状态变更事件处理
        /// </summary>
        private void WallpaperManager_PauseStateChanged(object sender, bool isPaused)
        {
            if (_currentType == WallpaperType.Html && _isWebViewInitialized)
            {
                WallpaperWebView.Visibility = isPaused ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (_currentType == WallpaperType.Image)
            {
                WallpaperImage.Visibility = isPaused ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        
        /// <summary>
        /// 显示设置变更事件处理
        /// </summary>
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            try
            {
                // 调整窗口大小和位置
                SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0,
                    (int)SystemParameters.PrimaryScreenWidth,
                    (int)SystemParameters.PrimaryScreenHeight,
                    SWP_SHOWWINDOW);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示设置变更处理出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置图片壁纸
        /// </summary>
        private void SetImageWallpaper(string imagePath)
        {
            try
            {
                // 加载图片
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();
                bitmap.Freeze(); // 提高性能
                
                // 设置到Image控件
                WallpaperImage.Source = bitmap;
                
                // 显示Image控件，隐藏WebView控件
                WallpaperImage.Visibility = Visibility.Visible;
                WallpaperWebView.Visibility = Visibility.Collapsed;
                
                _currentType = WallpaperType.Image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置图片壁纸时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置HTML壁纸
        /// </summary>
        private void SetHtmlWallpaper(string htmlPath)
        {
            try
            {
                if (!_isWebViewInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("WebView未初始化");
                    return;
                }
                
                // 导航到HTML文件
                WallpaperWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                
                // 显示WebView控件，隐藏Image控件
                WallpaperWebView.Visibility = Visibility.Visible;
                WallpaperImage.Visibility = Visibility.Collapsed;
                
                _currentType = WallpaperType.Html;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置HTML壁纸时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清除壁纸
        /// </summary>
        private void ClearWallpaper()
        {
            WallpaperImage.Source = null;
            WallpaperImage.Visibility = Visibility.Collapsed;
            
            if (_isWebViewInitialized)
            {
                WallpaperWebView.CoreWebView2.Navigate("about:blank");
                WallpaperWebView.Visibility = Visibility.Collapsed;
            }
            
            _currentType = WallpaperType.None;
        }
        
        /// <summary>
        /// 设置窗口扩展样式
        /// </summary>
        private void SetWindowExStyle()
        {
            try
            {
                // 获取当前窗口样式
                int exStyle = GetWindowLong(_windowHandle, GWL_EXSTYLE);
                
                // 添加工具窗口样式
                exStyle |= WS_EX_TOOLWINDOW;
                
                // 应用样式
                SetWindowLong(_windowHandle, GWL_EXSTYLE, exStyle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置窗口扩展样式时出错: {ex.Message}");
            }
        }
        
             /// <summary>
        /// 释放资源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // 取消订阅事件
                if (App.Instance?.WallpaperManager != null)
                {
                    App.Instance.WallpaperManager.WallpaperChanged -= WallpaperManager_WallpaperChanged;
                    App.Instance.WallpaperManager.PauseStateChanged -= WallpaperManager_PauseStateChanged;
                }
                
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
                
                // 清理WebView资源
                if (_isWebViewInitialized)
                {
                    WallpaperWebView.NavigationCompleted -= WebView_NavigationCompleted;
                    WallpaperWebView.Dispose();
                }
                
                // 清理图片资源
                WallpaperImage.Source = null;
                
                // 恢复原始父窗口
                SetParent(_windowHandle, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"窗口关闭时清理资源出错: {ex.Message}");
            }
            
            base.OnClosed(e);
        }
        
        #region Win32 API
        
        // 委托
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        // 常量
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const uint SWP_SHOWWINDOW = 0x0040;
        
        // Win32 API
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, ref IntPtr lpdwResult);
        
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(int uAction, int uParam, IntPtr lpvParam, int fuWinIni);
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 系统事件
        /// </summary>
        private static class SystemEvents
        {
            public static event EventHandler DisplaySettingsChanged;
            
            static SystemEvents()
            {
                // 注册显示设置变更消息
                HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(Application.Current.MainWindow).Handle);
                source?.AddHook(WndProc);
            }
            
            private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                const int WM_DISPLAYCHANGE = 0x007E;
                
                if (msg == WM_DISPLAYCHANGE)
                {
                    DisplaySettingsChanged?.Invoke(null, EventArgs.Empty);
                }
                
                return IntPtr.Zero;
            }
        }
        
        #endregion
    }
}
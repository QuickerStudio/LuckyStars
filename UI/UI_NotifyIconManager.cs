using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using LuckyStars.Core;
using System.Windows.Resources;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace LuckyStars.UI
{
    /// <summary>
    /// 系统托盘图标管理器
    /// </summary>
    public class NotifyIconManager : IDisposable
    {
        // 事件: 退出请求
        public event EventHandler ExitRequested;
        
        // 事件: 切换播放/暂停请求
        public event EventHandler TogglePlayPauseRequested;
        
        // 事件: 文件拖放
        public event EventHandler<string[]> FileDropped;
        
        // 托盘图标
        private NotifyIcon _notifyIcon;
        
        // 上下文菜单
        private ContextMenuStrip _contextMenu;
        
        // 文件捕获窗口
        private Catcher _catcher;
        
        // 是否已初始化
        private bool _isInitialized = false;
        
        // 是否为暂停状态
        private bool _isPaused = false;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public NotifyIconManager()
        {
        }
        
        /// <summary>
        /// 初始化托盘图标
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            try
            {
                // 创建上下文菜单
                _contextMenu = new ContextMenuStrip();
                _contextMenu.Items.Add("退出", null, (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty));
                
                // 创建托盘图标
                _notifyIcon = new NotifyIcon
                {
                    Text = "LuckyStars 壁纸",
                    Visible = true,
                    Icon = LoadTrayIcon(false),
                    ContextMenuStrip = _contextMenu
                };
                
                // 注册单击事件
                _notifyIcon.Click += (s, e) =>
                {
                    if (e is MouseEventArgs mouseArgs && mouseArgs.Button == MouseButtons.Left)
                    {
                        TogglePlayPauseRequested?.Invoke(this, EventArgs.Empty);
                    }
                };
                
                // 注册文件拖放事件
                _notifyIcon.MouseMove += NotifyIcon_MouseMove;
                _notifyIcon.MouseDown += NotifyIcon_MouseDown;
                
                // 创建文件捕获窗口
                _catcher = new Catcher();
                _catcher.FileDropped += (s, files) => FileDropped?.Invoke(this, files);
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化托盘图标时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        private void NotifyIcon_MouseMove(object sender, MouseEventArgs e)
        {
            // 获取托盘图标位置
            try
            {
                // 获取鼠标位置
                var mousePosition = System.Windows.Forms.Control.MousePosition;
                
                // 显示文件捕获窗口在托盘图标附近
                if (IsDraggingFiles())
                {
                    _catcher.Dispatcher.Invoke(() => 
                    {
                        _catcher.ShowAtPosition(mousePosition.X, mousePosition.Y);
                    });
                }
                else if (_catcher.IsVisible)
                {
                    _catcher.Dispatcher.Invoke(() => 
                    {
                        _catcher.HideWindow();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"托盘图标鼠标移动处理时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 鼠标按下事件
        /// </summary>
        private void NotifyIcon_MouseDown(object sender, MouseEventArgs e)
        {
            if (_catcher.IsVisible)
            {
                _catcher.Dispatcher.Invoke(() => 
                {
                    _catcher.HideWindow();
                });
            }
        }
        
        /// <summary>
        /// 检测是否正在拖动文件
        /// </summary>
        /// <returns>是否正在拖动文件</returns>
        private bool IsDraggingFiles()
        {
            // 使用Win32 API检测是否正在拖动文件
            try
            {
                // 简单实现，仅通过GetKeyState检查鼠标左键状态
                return (GetKeyState(0x01) & 0x8000) != 0; // 0x01为VK_LBUTTON (左键)
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 更新暂停状态
        /// </summary>
        /// <param name="isPaused">是否暂停</param>
        public void UpdatePauseState(bool isPaused)
        {
            if (_isPaused != isPaused)
            {
                _isPaused = isPaused;
                
                try
                {
                    // 更新托盘图标
                    _notifyIcon.Icon = LoadTrayIcon(isPaused);
                    _notifyIcon.Text = $"LuckyStars 壁纸 - {(isPaused ? "已暂停" : "正在播放")}";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"更新托盘图标状态时出错: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 加载托盘图标
        /// </summary>
        /// <param name="isPaused">是否为暂停状态</param>
        /// <returns>托盘图标</returns>
        private Icon LoadTrayIcon(bool isPaused)
        {
            try
            {
                // 尝试从资源加载图标
                string iconName = isPaused ? "tray_paused.ico" : "tray.ico";
                
                // 先检查Resources目录
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", iconName);
                
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
                
                // 尝试从嵌入资源加载
                Uri resourceUri = new Uri($"pack://application:,,,/Resources/Images/{iconName}");
                
                try 
                {
                    StreamResourceInfo info = System.Windows.Application.GetResourceStream(resourceUri);
                    if (info != null)
                    {
                        return new Icon(info.Stream);
                    }
                }
                catch 
                {
                    // 忽略资源加载错误
                }
                
                // 使用默认图标
                return System.Drawing.SystemIcons.Application;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载托盘图标时出错: {ex.Message}");
                return System.Drawing.SystemIcons.Application;
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
                
                if (_contextMenu != null)
                {
                    _contextMenu.Dispose();
                    _contextMenu = null;
                }
                
                if (_catcher != null)
                {
                    _catcher.Close();
                    _catcher = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"释放托盘图标资源时出错: {ex.Message}");
            }
        }
        
        #region Win32 API
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
        
        #endregion
    }
}
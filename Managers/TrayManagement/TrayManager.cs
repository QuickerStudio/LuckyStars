using System;
using System.Windows.Forms;
using LuckyStars.Managers.TrayManagement.DropHandling;
using LuckyStars.Managers.TrayManagement.MouseHandlers;
using LuckyStars.Managers.TrayManagement.Utils;
using LuckyStars.UI;

namespace LuckyStars.Managers.TrayManagement
{
    /// <summary>
    /// 托盘管理器，负责管理系统托盘图标和相关功能
    /// </summary>
    public class TrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly MainWindow _mainWindow;
        private TrayDropWindow? _dropWindow;
        private DetectionZoneWindow? _detectionZoneWindow;
        private System.Windows.Forms.Timer? _clickTimer;
        private const int DoubleClickTime = 300;
        private MouseButtons _lastMouseButton = MouseButtons.None;
        private bool _isWaitingForSecondClick = false;

        // 组件
        private readonly LeftClickHandler _leftClickHandler;
        private readonly RightClickHandler _rightClickHandler;
        private readonly DetectionZoneHandler _detectionZoneHandler;
        private readonly TrayDropHandler _trayDropHandler;
        private readonly WebViewToggler _webViewToggler;

        /// <summary>
        /// 初始化托盘管理器
        /// </summary>
        /// <param name="mainWindow">主窗口实例</param>
        public TrayManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            // 初始化WebView切换器
            _webViewToggler = new WebViewToggler(mainWindow);

            // 初始化 NotifyIcon
            _notifyIcon = new NotifyIcon
            {
                Icon = TrayIconUtils.GetEmbeddedIcon("LuckyStars.Resource.UI.app.ico"),
                Visible = true,
                Text = "A little lucky star, lighting up a piece of the night sky for you!"
            };

            // 初始化拖放窗口
            _dropWindow = new TrayDropWindow();
            _dropWindow.Hide(); // 确保窗口在启动时不可见

            // 初始化检测区窗口
            _detectionZoneWindow = new DetectionZoneWindow();
            _detectionZoneWindow.Hide(); // 初始时隐藏

            // 初始化组件
            _leftClickHandler = new LeftClickHandler(mainWindow, _webViewToggler);
            _rightClickHandler = new RightClickHandler(mainWindow, _notifyIcon);
            _trayDropHandler = new TrayDropHandler(mainWindow, _dropWindow);
            _detectionZoneHandler = new DetectionZoneHandler(_detectionZoneWindow, _dropWindow);

            // 连接文件拖放事件
            _detectionZoneWindow.FileDropped += _trayDropHandler.OnFileDropped;

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
                        _leftClickHandler.HandleDoubleClick();
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        _rightClickHandler.HandleDoubleClick();
                    }
                }
                else
                {
                    _isWaitingForSecondClick = true;
                    _clickTimer.Start();
                }
            };

            // 更新拖放窗口位置
            _detectionZoneHandler.UpdateDropWindowPosition();
        }

        /// <summary>
        /// 计时器回调函数，处理单击事件
        /// </summary>
        private void OnClickTimerElapsed(object? sender, EventArgs e)
        {
            _clickTimer?.Stop();
            _isWaitingForSecondClick = false;

            // 根据最后按下的鼠标按钮执行相应操作
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (_lastMouseButton == MouseButtons.Left)
                {
                    _leftClickHandler.HandleSingleClick();
                }
                else if (_lastMouseButton == MouseButtons.Right)
                {
                    _rightClickHandler.HandleSingleClick();
                }
            });
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            // 先清理通知图标
            Dispose();

            // 退出程序
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// 显示托盘图标
        /// </summary>
        public void Show()
        {
            _notifyIcon.Visible = true;
        }

        /// <summary>
        /// 隐藏托盘图标
        /// </summary>
        public void Hide()
        {
            _notifyIcon.Visible = false;
        }

        /// <summary>
        /// 更新拖放窗口位置
        /// </summary>
        public void UpdateDropWindowPosition()
        {
            _detectionZoneHandler.UpdateDropWindowPosition();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
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
            _detectionZoneHandler?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}

using System;
using System.Windows;
using System.Windows.Forms;
using LuckyStars.Managers.TrayManagement.Utils;
using LuckyStars.UI;
using Application = System.Windows.Application;

namespace LuckyStars.Managers.TrayManagement.MouseHandlers
{
    /// <summary>
    /// 左键点击处理器，负责处理托盘图标的左键点击事件
    /// </summary>
    public class LeftClickHandler
    {
        private readonly MainWindow _mainWindow;
        private readonly WebViewToggler _webViewToggler;

        /// <summary>
        /// 初始化左键点击处理器
        /// </summary>
        /// <param name="mainWindow">主窗口实例</param>
        /// <param name="webViewToggler">WebView切换器</param>
        public LeftClickHandler(MainWindow mainWindow, WebViewToggler webViewToggler)
        {
            _mainWindow = mainWindow;
            _webViewToggler = webViewToggler;
        }

        /// <summary>
        /// 处理左键单击事件
        /// </summary>
        public void HandleSingleClick()
        {
            // 在UI线程上执行
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 左键单击：隐藏视频播放器、切换图片、静音视频播放器
                _mainWindow.MuteVideoPlayer();
                _mainWindow.NextImage();
            });
        }

        /// <summary>
        /// 处理左键双击事件
        /// </summary>
        public void HandleDoubleClick()
        {
            // 在UI线程上执行
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 左键双击：切换音乐播放状态并隐藏/显示WebView
                _mainWindow.Webview2_playmusic(); // 切换音乐播放状态
                _webViewToggler.ToggleWebView(); // 保留原有的隐藏/显示WebView功能
            });
        }
    }
}

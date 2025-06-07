using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using LuckyStars.Managers;
using LuckyStars.Players.Settings;

namespace LuckyStars.Players
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public class InteractivePlayer
    {
        private readonly WebView2 webView;

        public InteractivePlayer(WebView2 webView)
        {
            this.webView = webView;
            Settings = new InteractivePlayerSettings();
        }

        private bool isWebViewInitialized = false;

        /// <summary>
        /// 交互式播放器设置
        /// </summary>
        public InteractivePlayerSettings Settings { get; }

        public bool IsWebViewInitialized => isWebViewInitialized;

        /// <summary>
        /// 初始化WebView2控件
        /// </summary>
        /// <returns>初始化任务</returns>
        public async Task InitializeWebView()
        {
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LuckyStarsWebViewData"
            );
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await webView.EnsureCoreWebView2Async(env);

            // 配置WebView2设置
            ConfigureWebViewSettings();
            webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            isWebViewInitialized = true;

            // 加载内容并应用样式
            LoadTestHtml();
            ApplyDpiScale();
            ApplyFullScreenToWebView();

            // 如果启用了自动刷新，则在指定延迟后刷新
            if (Settings.AutoRefreshEnabled)
            {
                await Task.Delay(Settings.RefreshInterval);
                webView.CoreWebView2?.Reload();
            }
        }

        /// <summary>
        /// 配置WebView2的基本设置
        /// </summary>
        private void ConfigureWebViewSettings()
        {
            if (webView.CoreWebView2 == null) return;

            // 启用JS和开发者工具
            webView.CoreWebView2.Settings.IsScriptEnabled = true;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = true;

            // 禁用下载功能
            webView.CoreWebView2.DownloadStarting += (sender, args) => args.Cancel = true;
        }

        /// <summary>
        /// 加载测试HTML内容
        /// </summary>
        public void LoadTestHtml()
        {
            if (!isWebViewInitialized || webView.CoreWebView2 == null) return;

            // 使用星空动画接口加载内容
            var animationProvider = new StarfieldAnimationProvider();
            string html = animationProvider.GetHtml();

            // 加载 HTML 内容
            LoadHtmlContent(html);
        }

        /// <summary>
        /// 加载 HTML 内容
        /// </summary>
        /// <param name="html">要加载的HTML内容</param>
        private void LoadHtmlContent(string html)
        {
            if (webView.CoreWebView2 == null) return;
            webView.CoreWebView2.NavigateToString(html);
        }



        /// <summary>
        /// 应用DPI缩放以适应不同分辨率
        /// </summary>
        public void ApplyDpiScale()
        {
            if (!isWebViewInitialized || webView.CoreWebView2 == null) return;
            var dpiInfo = VisualTreeHelper.GetDpi(webView);
            webView.ZoomFactor = dpiInfo.DpiScaleX;
        }

        /// <summary>
        /// 将WebView设置为全屏模式
        /// </summary>
        public void ApplyFullScreenToWebView()
        {
            if (webView == null) return;

            webView.Width = double.NaN;
            webView.Height = double.NaN;
            webView.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView.VerticalAlignment = VerticalAlignment.Stretch;

            if (webView.ActualWidth > 0 && webView.ActualHeight > 0)
            {
                webView.Width = webView.ActualWidth;
                webView.Height = webView.ActualHeight;
            }
        }

        /// <summary>
        /// 设置WebView的可见性
        /// </summary>
        /// <param name="isVisible">是否可见</param>
        public void SetWebViewVisibility(bool isVisible)
        {
            if (!isWebViewInitialized || webView.CoreWebView2 == null) return;
            webView.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 获取WebView实例
        /// </summary>
        /// <returns>WebView2实例</returns>
        public WebView2 GetWebView() => webView;
    }
}

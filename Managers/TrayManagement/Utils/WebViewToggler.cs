using System.Threading.Tasks;
using LuckyStars.UI;

namespace LuckyStars.Managers.TrayManagement.Utils
{
    /// <summary>
    /// WebView切换器，负责处理WebView的显示和隐藏
    /// </summary>
    public class WebViewToggler
    {
        private readonly MainWindow _mainWindow;
        private bool _isWebViewActive = true;
        private bool _needReloadContent = false;

        /// <summary>
        /// 初始化WebView切换器
        /// </summary>
        /// <param name="mainWindow">主窗口实例</param>
        public WebViewToggler(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        /// <summary>
        /// 切换 WebView 的显示状态
        /// </summary>
        public async void ToggleWebView()
        {
            if (_isWebViewActive)
            {
                // 隐藏
                var webView = _mainWindow.GetWebView();
                if (webView?.CoreWebView2 != null)
                {
                    await webView.CoreWebView2.ExecuteScriptAsync(@"
                        document.body.style.display='none';
                        document.querySelectorAll('video, audio').forEach(media => media.pause());
                    ");

                    // 使用 Navigate("about:blank") 清空内容以降低能耗
                    webView.CoreWebView2.Navigate("about:blank");
                }

                _needReloadContent = true; // 标记需要重新加载内容
                _mainWindow.SetWebViewVisibility(false);
                _isWebViewActive = false;
            }
            else
            {
                // 恢复
                _mainWindow.SetWebViewVisibility(true);

                // 始终重新加载内容，因为我们在隐藏时使用了 Navigate("about:blank")
                var interactivePlayer = _mainWindow.GetInteractivePlayer();
                interactivePlayer?.LoadTestHtml();
                _needReloadContent = false;

                _isWebViewActive = true;
            }
        }

        /// <summary>
        /// 切换音频播放状态
        /// </summary>
        public async Task ToggleAudio()
        {
            // 暂停或播放所有音视频
            var webView = _mainWindow.GetWebView();
            if (webView?.CoreWebView2 != null)
            {
                await webView.CoreWebView2.ExecuteScriptAsync(@"
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
        }

        /// <summary>
        /// 获取WebView活动状态
        /// </summary>
        public bool IsWebViewActive => _isWebViewActive;
    }
}

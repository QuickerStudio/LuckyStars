using LuckyStars;
using System.Windows.Forms;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace LuckyStars
{
    public class TrayIconHostWindow
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly MainWindow _mainWindow;
        private bool _isWebViewActive = true;
        private bool _needReloadContent = false;
        private bool _isWaitingForSecondClick = false;
        private System.Windows.Forms.Timer? _clickTimer;  // 添加 ? 表示可空
        private const int DoubleClickTime = 300;

        public TrayIconHostWindow(MainWindow mainWindow, System.Windows.Forms.Timer mouseClickTimer)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            _notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("Resource/UI/app.ico"),
                Visible = true,
                Text = "壁纸切换工具"
            };

            // 初始化计时器，但不启动
            _clickTimer = mouseClickTimer;
            _clickTimer.Interval = DoubleClickTime;
            _clickTimer.Tick += OnClickTimerElapsed;

            // 鼠标按下事件
            _notifyIcon.MouseDown += (sender, e) =>
            {
                // 同时按下鼠标左右键：调用 ExitApplication() 清理图标并退出程序
                if ((Control.MouseButtons & MouseButtons.Left) != 0 &&
                    (Control.MouseButtons & MouseButtons.Right) != 0)
                {
                    ExitApplication();
                    return;
                }

                // 右键点击：循环切换定时器状态
                if (e.Button == MouseButtons.Right)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow.TimerState newState = _mainWindow.CycleTimerState();
                        ShowTimerStateNotification(newState);
                    });
                }
                // 左键点击处理（延迟处理以区分单击和双击）
                else if (e.Button == MouseButtons.Left)
                {
                    if (_isWaitingForSecondClick)
                    {
                        // 这是双击的第二次点击，停止计时器
                        _clickTimer.Stop();
                        _isWaitingForSecondClick = false;

                        // 在UI线程上执行双击操作
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ToggleWebView();
                        });
                    }
                    else
                    {
                        // 这是第一次点击，启动计时器等待可能的第二次点击
                        _isWaitingForSecondClick = true;
                        _clickTimer.Start();
                    }
                }
            };
        }

        // 修改 OnClickTimerElapsed 方法的签名以匹配目标委托的 null 性特性
        private void OnClickTimerElapsed(object? sender, EventArgs e)
        {
            _clickTimer?.Stop();
            _isWaitingForSecondClick = false;

            // 执行单击操作：切换下一张壁纸
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _mainWindow.NextImage();
            });
        }

        // 显示定时器状态并更新托盘图标的提示文本
        private void ShowTimerStateNotification(MainWindow.TimerState state)
        {
            // 更新托盘图标的提示文本
            _notifyIcon.Text = $"当前壁纸切换频率: {GetTimerStateText(state)}";
        }

        // 辅助方法获取状态文本
        private static string GetTimerStateText(MainWindow.TimerState state)
        {
            return state switch
            {
                MainWindow.TimerState.FiveMinutes => "5分钟",
                MainWindow.TimerState.TenMinutes => "10分钟",
                MainWindow.TimerState.TwentyMinutes => "20分钟",
                MainWindow.TimerState.Disabled => "已停用",
                _ => "未知",
            };
        }

        private async void ToggleWebView()
        {
            if (_isWebViewActive)
            {
                // 隐藏
                await _mainWindow.GetWebView().CoreWebView2.ExecuteScriptAsync(@"
                    document.body.style.display='none';
                    document.querySelectorAll('video, audio').forEach(media => media.pause());
                ");

                _mainWindow.GetWebView().CoreWebView2.Navigate("about:blank");
                _needReloadContent = true;
                _isWebViewActive = false;
            }
            else
            {
                // 恢复
                _mainWindow.SetWebViewVisibility(true);

                if (_needReloadContent)
                {
                    _mainWindow.LoadTestHtml();
                    _needReloadContent = false;
                }
                else
                {
                    await _mainWindow.GetWebView().CoreWebView2.ExecuteScriptAsync("document.body.style.display='block'");
                }

                _mainWindow.ApplyFullScreenToWebView();
                _isWebViewActive = true;
            }
        }

        private async System.Threading.Tasks.Task ToggleAudio()
        {
            // 暂停或播放所有音视频
            await _mainWindow.GetWebView().CoreWebView2.ExecuteScriptAsync(@"
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

        public void Show()
        {
            _notifyIcon.Visible = true;
        }

        public void Hide()
        {
            _notifyIcon.Visible = false;
        }

        public void Dispose()
        {
            if (_clickTimer != null)
            {
                _clickTimer.Stop();
                _clickTimer.Dispose();
                _clickTimer = null;
            }

            _notifyIcon.Dispose();
        }

        private void ExitApplication()
        {
            // 先清理通知图标
            Dispose();

            // 退出程序
            System.Windows.Application.Current.Shutdown();
        }
    }
}
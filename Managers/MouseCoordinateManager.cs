using Microsoft.Web.WebView2.Wpf;
using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using LuckyStars.Players;

namespace LuckyStars.Managers
{
    public class MouseCoordinateManager
    {
        private HwndSource? hwndSource;
        private System.Timers.Timer? mousePositionTimer;
        private IntPtr hwnd = IntPtr.Zero;
        private readonly WebView2 webView;
        private readonly InteractivePlayer interactivePlayer;

        public MouseCoordinateManager(IntPtr hwnd, WebView2 webView, InteractivePlayer interactivePlayer)
        {
            this.hwnd = hwnd;
            this.webView = webView;
            this.interactivePlayer = interactivePlayer;
        }

        /// <summary>
        /// 初始化鼠标坐标定时器和窗口消息钩子
        /// </summary>
        public void StartMouseCoordSender()
        {
            // 获取窗口句柄并添加消息钩子
            hwndSource = HwndSource.FromHwnd(hwnd);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
            }

            // 启动定时器，每 10 毫秒发送一次鼠标坐标
            mousePositionTimer = new System.Timers.Timer(10)
            {
                AutoReset = true,
                Enabled = true
            };
            mousePositionTimer.Elapsed += (s, e) => SendMouseCoordinates();
        }

        /// <summary>
        /// 关闭鼠标坐标发送定时器和移除消息钩子
        /// </summary>
        public void StopMouseCoordSender()
        {
            if (mousePositionTimer != null)
            {
                mousePositionTimer.Stop();
                mousePositionTimer.Dispose();
                mousePositionTimer = null;
            }
            if (hwndSource != null)
            {
                hwndSource.RemoveHook(WndProc);
                hwndSource = null;
            }
        }

        /// <summary>
        /// 从系统获取鼠标坐标，并发送到目标窗口（本例中仍然是主窗口）。
        /// 然后在WndProc里通过ExecuteScriptAsync发送到WebView2。
        /// </summary>
        private void SendMouseCoordinates()
        {
            if (NativeMethods.GetCursorPos(out NativeMethods.POINT pt))
            {
                // 将屏幕坐标转换为窗口客户区坐标
                NativeMethods.ScreenToClient(hwnd, ref pt);

                // 将 X 和 Y 坐标打包到 lParam 中：低 16 位为 X，高 16 位为 Y
                IntPtr lParam = (IntPtr)(((pt.Y & 0xFFFF) << 16) | (pt.X & 0xFFFF));
                // 此处 targetHandle 为本窗口句柄
                var targetHandle = hwnd;
                NativeMethods.SendMessage(targetHandle, (uint)NativeMethods.WM_USER_MOUSE, IntPtr.Zero, lParam);
            }
        }

        /// <summary>
        /// 窗口消息钩子，用于接收自定义鼠标坐标消息，
        /// 再通过 ExecuteScriptAsync 把数据发往 WebView2 JavaScript。
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_USER_MOUSE)
            {
                int xPos = lParam.ToInt32() & 0xFFFF;
                int yPos = (lParam.ToInt32() >> 16) & 0xFFFF;

                // 获取WebView的位置和大小
                System.Windows.Point webViewPos = webView.PointToScreen(new System.Windows.Point(0, 0));

                // 计算鼠标相对于WebView的坐标
                int webViewX = xPos;
                int webViewY = yPos;

                // 调整坐标以匹配鼠标指针头部
                // 标准鼠标指针的热点通常在左上角，但我们需要将跟踪点对准指针头部
                // 根据Windows标准鼠标指针，热点通常在左上角，但视觉上我们看到的是箭头的尖端
                // 这里我们不需要额外调整，因为我们已经转换为客户区坐标

                // [跨层通信] 鼠标坐标

                // 将鼠标坐标发送到 WebView2 的 JS 端
                if (interactivePlayer.IsWebViewInitialized && webView?.CoreWebView2 != null)
                {
                    // 通过JS函数 updateMousePosition(x,y) 更新
                    string jsCode = $"if(typeof updateMousePosition === 'function') {{ updateMousePosition({webViewX}, {webViewY}); }}";
                    Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            await webView.CoreWebView2.ExecuteScriptAsync(jsCode);
                        }
                        catch (Exception ex)
                        {
                            // 执行JS代码失败
                        }
                    });
                }

                handled = true;
            }
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// 跨层通信需要的 Win32 导入
    /// </summary>
    public static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        // 自定义消息ID，用于发送鼠标坐标（WM_USER+100）
        public const int WM_USER_MOUSE = 0x0400 + 100;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}

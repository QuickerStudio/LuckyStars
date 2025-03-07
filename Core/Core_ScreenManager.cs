using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LuckyStars.Core
{
    /// <summary>
    /// 屏幕状态监控，负责监控系统屏幕状态变化
    /// </summary>
    public class ScreenManager : IDisposable
    {
        /// <summary>
        /// 屏幕状态枚举
        /// </summary>
        public enum ScreenState
        {
            /// <summary>
            /// 正常状态
            /// </summary>
            Normal,
            
            /// <summary>
            /// 进入休眠
            /// </summary>
            PowerOff,
            
            /// <summary>
            /// 锁屏
            /// </summary>
            Locked,
            
            /// <summary>
            /// 屏幕保护程序激活
            /// </summary>
            ScreenSaver
        }
        
        // 屏幕状态变化事件
        public event EventHandler<ScreenState> ScreenStateChanged;
        
        // 当前屏幕状态
        private ScreenState _currentState = ScreenState.Normal;
        
        // 用于接收系统消息的窗口句柄
        private IntPtr _messageWindowHandle;
        
        // 消息处理窗口类
        private MessageWindow _messageWindow;
        
        // 是否处理锁屏事件
        private bool _handleLockEvents = true;
        
        // 是否处理电源事件
        private bool _handlePowerEvents = true;
        
        // 是否处理屏幕保护事件
        private bool _handleScreenSaverEvents = true;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="handleLockEvents">是否处理锁屏事件</param>
        /// <param name="handlePowerEvents">是否处理电源事件</param>
        /// <param name="handleScreenSaverEvents">是否处理屏幕保护事件</param>
        public ScreenManager(
            bool handleLockEvents = true, 
            bool handlePowerEvents = true, 
            bool handleScreenSaverEvents = true)
        {
            _handleLockEvents = handleLockEvents;
            _handlePowerEvents = handlePowerEvents;
            _handleScreenSaverEvents = handleScreenSaverEvents;
            
            // 创建消息处理窗口
            _messageWindow = new MessageWindow();
            _messageWindow.MessageReceived += MessageWindow_MessageReceived;
            _messageWindowHandle = _messageWindow.Handle;
            
            // 注册会话状态变更事件
            if (_handleLockEvents)
            {
                SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            }
            
            // 注册电源模式变更事件
            if (_handlePowerEvents)
            {
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            }
            
            Debug.WriteLine("屏幕状态监控已初始化");
        }
        
        /// <summary>
        /// 系统会话状态变更事件处理
        /// </summary>
        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            try
            {
                Debug.WriteLine($"系统会话状态变更: {e.Reason}");
                
                switch (e.Reason)
                {
                    case SessionSwitchReason.SessionLock:
                        // 锁屏
                        SetScreenState(ScreenState.Locked);
                        break;
                        
                    case SessionSwitchReason.SessionUnlock:
                        // 解锁
                        SetScreenState(ScreenState.Normal);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理系统会话状态变更时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 系统电源模式变更事件处理
        /// </summary>
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"系统电源模式变更: {e.Mode}");
                
                switch (e.Mode)
                {
                    case PowerModes.Suspend:
                        // 系统休眠
                        SetScreenState(ScreenState.PowerOff);
                        break;
                        
                    case PowerModes.Resume:
                        // 系统恢复
                        SetScreenState(ScreenState.Normal);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理系统电源模式变更时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 消息窗口收到消息事件处理
        /// </summary>
        private void MessageWindow_MessageReceived(object sender, WindowMessageEventArgs e)
        {
            try
            {
                // 处理系统电源广播消息
                if (e.Message == WM_POWERBROADCAST)
                {
                    // PBT_APMSUSPEND: 系统即将挂起
                    if (e.WParam.ToInt32() == PBT_APMSUSPEND)
                    {
                        Debug.WriteLine("系统即将挂起");
                        SetScreenState(ScreenState.PowerOff);
                    }
                    // PBT_APMRESUMESUSPEND: 系统从挂起恢复
                    else if (e.WParam.ToInt32() == PBT_APMRESUMESUSPEND)
                    {
                        Debug.WriteLine("系统从挂起恢复");
                        SetScreenState(ScreenState.Normal);
                    }
                }
                // 处理屏幕保护消息
                else if (_handleScreenSaverEvents && e.Message == WM_SYSCOMMAND)
                {
                    int wParam = e.WParam.ToInt32() & 0xFFF0;
                    
                    // SC_SCREENSAVE: 屏幕保护程序激活
                    if (wParam == SC_SCREENSAVE)
                    {
                        Debug.WriteLine("屏幕保护程序激活");
                        SetScreenState(ScreenState.ScreenSaver);
                    }
                }
                // 处理显示器电源状态变更
                else if (_handlePowerEvents && e.Message == WM_DISPLAYCHANGE)
                {
                    Debug.WriteLine("显示器状态变更");
                    // 这里可能需要额外判断是否关闭了显示器
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理窗口消息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置屏幕状态
        /// </summary>
        /// <param name="state">屏幕状态</param>
        private void SetScreenState(ScreenState state)
        {
            if (_currentState != state)
            {
                _currentState = state;
                ScreenStateChanged?.Invoke(this, state);
            }
        }
        
        /// <summary>
        /// 获取当前屏幕状态
        /// </summary>
        /// <returns>屏幕状态</returns>
        public ScreenState GetCurrentState()
        {
            return _currentState;
        }
        
        /// <summary>
        /// 检查屏幕保护程序是否处于活动状态
        /// </summary>
        /// <returns>是否处于活动状态</returns>
        public bool IsScreenSaverActive()
        {
            try
            {
                bool result = false;
                SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref result, 0);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查屏幕保护程序状态时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查屏幕是否已锁定
        /// </summary>
        /// <returns>是否已锁定</returns>
        public bool IsScreenLocked()
        {
            return _currentState == ScreenState.Locked;
        }
        
        /// <summary>
        /// 获取当前显示器电源状态
        /// </summary>
        /// <returns>电源状态，-1表示获取失败</returns>
        public int GetMonitorPowerState()
        {
            try
            {
                int powerState = -1;
                bool success = GetDevicePowerState(GetDesktopWindow(), ref powerState);
                return success ? powerState : -1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取显示器电源状态时出错: {ex.Message}");
                return -1;
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 取消注册系统事件
                if (_handleLockEvents)
                {
                    SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                }
                
                if (_handlePowerEvents)
                {
                    SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                }
                
                // 释放消息窗口
                if (_messageWindow != null)
                {
                    _messageWindow.MessageReceived -= MessageWindow_MessageReceived;
                    _messageWindow.Dispose();
                    _messageWindow = null;
                }
                
                Debug.WriteLine("屏幕状态监控已释放资源");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放屏幕状态监控资源时出错: {ex.Message}");
            }
        }
        
        #region Win32 API和常量
        
        // Win32 消息常量
        private const int WM_POWERBROADCAST = 0x0218;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_DISPLAYCHANGE = 0x007E;
        
        // 电源广播常量
        private const int PBT_APMSUSPEND = 0x0004;
        private const int PBT_APMRESUMESUSPEND = 0x0007;
        
        // 系统命令常量
        private const int SC_SCREENSAVE = 0xF140;
        
        // 系统参数常量
        private const int SPI_GETSCREENSAVERRUNNING = 0x0072;
        
        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(int uAction, int uParam, ref bool lpvParam, int fuWinIni);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        
        [DllImport("kernel32.dll")]
        private static extern bool GetDevicePowerState(IntPtr hDevice, ref int pState);
        
        /// <summary>
        /// 接收系统消息的窗口类
        /// </summary>
        private class MessageWindow : NativeWindow, IDisposable
        {
            public event EventHandler<WindowMessageEventArgs> MessageReceived;
            
            public MessageWindow()
            {
                // 创建窗口
                CreateParams cp = new CreateParams();
                cp.Caption = "LuckyStars_ScreenManager_MessageWindow";
                CreateHandle(cp);
            }
            
            protected override void WndProc(ref Message m)
            {
                // 触发消息接收事件
                MessageReceived?.Invoke(this, new WindowMessageEventArgs(m.Msg, m.WParam, m.LParam));
                
                base.WndProc(ref m);
            }
            
            public void Dispose()
            {
                DestroyHandle();
            }
        }
        
        /// <summary>
        /// 窗口消息事件参数
        /// </summary>
        public class WindowMessageEventArgs : EventArgs
        {
            /// <summary>
            /// 消息ID
            /// </summary>
            public int Message { get; }
            
            /// <summary>
            /// 参数1
            /// </summary>
            public IntPtr WParam { get; }
            
            /// <summary>
            /// 参数2
            /// </summary>
            public IntPtr LParam { get; }
            
            public WindowMessageEventArgs(int message, IntPtr wParam, IntPtr lParam)
            {
                Message = message;
                WParam = wParam;
                LParam = lParam;
            }
        }
        
        #endregion
    }
}
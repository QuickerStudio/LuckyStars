using System;
using System.Media;
using System.Windows;
using System.Windows.Forms;
using LuckyStars.Managers.TrayManagement.Utils;
using LuckyStars.UI;
using LuckyStars.Utils;
using Application = System.Windows.Application;

namespace LuckyStars.Managers.TrayManagement.MouseHandlers
{
    /// <summary>
    /// 右键点击处理器，负责处理托盘图标的右键点击事件
    /// </summary>
    public class RightClickHandler
    {
        private readonly MainWindow _mainWindow;
        private readonly NotifyIcon _notifyIcon;

        /// <summary>
        /// 初始化右键点击处理器
        /// </summary>
        /// <param name="mainWindow">主窗口实例</param>
        /// <param name="notifyIcon">托盘图标</param>
        public RightClickHandler(MainWindow mainWindow, NotifyIcon notifyIcon)
        {
            _mainWindow = mainWindow;
            _notifyIcon = notifyIcon;
        }

        /// <summary>
        /// 处理右键单击事件
        /// </summary>
        public void HandleSingleClick()
        {
            // 在UI线程上执行
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 右键单击：显示视频播放器、切换视频、设置音量20%
                _mainWindow.UnmuteVideoPlayer();
                _mainWindow.NextVideo();
            });
        }

        /// <summary>
        /// 处理右键双击事件
        /// </summary>
        public void HandleDoubleClick()
        {
            // 在UI线程上执行
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 右键双击：切换显示时长
                ToggleDisplayDuration();
            });
        }

        /// <summary>
        /// 切换显示时长
        /// </summary>
        private void ToggleDisplayDuration()
        {
            // 切换图片和视频壁纸的显示时长
            RegistryManager.TimerState newState = _mainWindow.CycleTimerState();

            // 播放提示音
            PlayNotificationSound();

            // 显示气泡通知
            _notifyIcon.ShowBalloonTip(
                3000,  // 显示3秒
                "壁纸切换频率",
                $"已切换为: {TrayIconUtils.GetTimerStateText(newState)}",
                ToolTipIcon.Info
            );

            // 更新托盘图标提示文本以便鼠标悬停时显示
            _notifyIcon.Text = $"LuckyStars - 当前壁纸切换频率: {TrayIconUtils.GetTimerStateText(newState)}";
        }

        /// <summary>
        /// 播放通知提示音
        /// </summary>
        private void PlayNotificationSound()
        {
            // 使用系统默认的通知提示音
            SystemSounds.Asterisk.Play();
        }
    }
}

using System;
using System.Windows;
using System.Windows.Forms;
using LuckyStars.Managers.TrayManagement.Utils;
using LuckyStars.UI;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;

namespace LuckyStars.Managers.TrayManagement.DropHandling
{
    /// <summary>
    /// 检测区处理器，负责处理拖放检测区域
    /// </summary>
    public class DetectionZoneHandler
    {
        private readonly DetectionZoneWindow _detectionZoneWindow;
        private readonly TrayDropWindow _dropWindow;
        private Rect _monitoringArea;
        private bool _isDraggingFromOutside = false;
        private System.Windows.Forms.Timer? _autoHideTimer;
        private const int AutoHideDelay = 5000; // 5秒后自动隐藏
        private const double DETECTION_ZONE_HEIGHT = 10;     // 检测区高度
        private const double DETECTION_ZONE_OFFSET = 5;     // 检测区与托盘区的距离

        /// <summary>
        /// 初始化检测区处理器
        /// </summary>
        /// <param name="detectionZoneWindow">检测区窗口</param>
        /// <param name="dropWindow">拖放窗口</param>
        public DetectionZoneHandler(DetectionZoneWindow detectionZoneWindow, TrayDropWindow dropWindow)
        {
            _detectionZoneWindow = detectionZoneWindow;
            _dropWindow = dropWindow;
            
            // 添加拖拽事件处理
            _detectionZoneWindow.DragEnter += DetectionZoneWindow_DragEnter;
            _detectionZoneWindow.DragLeave += DetectionZoneWindow_DragLeave;
            
            // 初始化监控区域
            UpdateMonitorArea();
        }

        /// <summary>
        /// 更新监控区域
        /// </summary>
        public void UpdateMonitorArea()
        {
            var trayRect = TrayIconUtils.GetTrayIconRect();
            // 创建一个位于托盘区上方的检测区，确保不覆盖托盘区
            _monitoringArea = new Rect(
                trayRect.X - 50,  // 向左扩展50像素
                trayRect.Y - DETECTION_ZONE_OFFSET - DETECTION_ZONE_HEIGHT,  // 在托盘区上方创建检测区，保持一定距离
                trayRect.Width + 100,  // 向右扩展100像素
                DETECTION_ZONE_HEIGHT  // 检测区高度
            );

            // 更新检测区窗口位置和大小
            if (_detectionZoneWindow != null)
            {
                _detectionZoneWindow.Left = _monitoringArea.X;
                _detectionZoneWindow.Top = _monitoringArea.Y;
                _detectionZoneWindow.Width = _monitoringArea.Width;
                _detectionZoneWindow.Height = _monitoringArea.Height;

                // 确保窗口显示但不获取焦点
                if (!_detectionZoneWindow.IsVisible)
                {
                    _detectionZoneWindow.Show();
                }
            }
        }

        /// <summary>
        /// 处理拖拽进入检测区事件
        /// </summary>
        private void DetectionZoneWindow_DragEnter(object sender, DragEventArgs e)
        {
            // 检查是否拖拽的是文件
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 允许拖放操作
                e.Effects = DragDropEffects.Copy;
                _isDraggingFromOutside = true;

                // 更新并显示拖放窗口
                UpdateDropWindowPosition(); // 拖放前更新一次位置
                var trayRect = TrayIconUtils.GetTrayIconRect();
                ShowDropWindow(trayRect);
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <summary>
        /// 处理拖拽离开检测区事件
        /// </summary>
        private void DetectionZoneWindow_DragLeave(object sender, DragEventArgs e)
        {
            // 拖拽离开检测区但不立即取消拖放窗口
            e.Handled = true;

            // 重置自动隐藏计时器
            StartAutoHideTimer();
        }

        /// <summary>
        /// 启动自动隐藏计时器
        /// </summary>
        private void StartAutoHideTimer()
        {
            // 创建或重置自动隐藏计时器
            if (_autoHideTimer == null)
            {
                _autoHideTimer = new System.Windows.Forms.Timer
                {
                    Interval = AutoHideDelay
                };
                _autoHideTimer.Tick += AutoHideTimer_Tick;
            }
            else
            {
                _autoHideTimer.Stop(); // 停止之前的计时器
            }

            // 启动计时器，5秒后自动隐藏
            _autoHideTimer.Start();
        }

        private void AutoHideTimer_Tick(object? sender, EventArgs e)
        {
            // 停止计时器
            _autoHideTimer?.Stop();

            // 在UI线程上隐藏拖放窗口
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _dropWindow?.Hide();
                _isDraggingFromOutside = false;
            });
        }

        /// <summary>
        /// 显示拖放窗口
        /// </summary>
        private void ShowDropWindow(Rect trayRect)
        {
            if (_dropWindow != null && !_dropWindow.IsVisible)
            {
                _dropWindow.Left = trayRect.X;
                _dropWindow.Top = trayRect.Y;
                _dropWindow.Width = trayRect.Width;
                _dropWindow.Height = trayRect.Height;

                _dropWindow.Show();
                _dropWindow.Activate();

                // 显示窗口时启动自动隐藏计时器
                StartAutoHideTimer();
            }
        }

        /// <summary>
        /// 更新拖放窗口位置
        /// </summary>
        public void UpdateDropWindowPosition()
        {
            UpdateMonitorArea(); // 更新监控区域
            var trayRect = TrayIconUtils.GetTrayIconRect();
            // 仅更新位置，不显示窗口
            if (_dropWindow != null)
            {
                _dropWindow.Left = trayRect.X;
                _dropWindow.Top = trayRect.Y;
                _dropWindow.Width = trayRect.Width;
                _dropWindow.Height = trayRect.Height;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_autoHideTimer != null)
            {
                _autoHideTimer.Stop();
                _autoHideTimer.Dispose();
                _autoHideTimer = null;
            }
            
            _detectionZoneWindow?.Close();
        }
    }
}

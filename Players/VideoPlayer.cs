using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LuckyStars.Players.Settings;

namespace LuckyStars.Players
{
    public class VideoPlayer : IDisposable
    {
        private readonly MediaElement _mediaElement;
        private string? _currentVideoPath;

        /// <summary>
        /// 视频播放器设置
        /// </summary>
        public VideoPlayerSettings Settings { get; }

        public VideoPlayer(MediaElement mediaElement)
        {
            _mediaElement = mediaElement;
            Settings = new VideoPlayerSettings();

            // 配置MediaElement以获得最佳性能
            _mediaElement.LoadedBehavior = MediaState.Manual;
            _mediaElement.UnloadedBehavior = MediaState.Stop;
            _mediaElement.Stretch = Stretch.Fill;
            _mediaElement.ScrubbingEnabled = true; // 启用快速定位

            // 启用硬件加速
            RenderOptions.SetBitmapScalingMode(mediaElement, BitmapScalingMode.HighQuality);

            // 禁用缓存以减少内存使用
            _mediaElement.CacheMode = null;

            // 添加循环播放功能
            _mediaElement.MediaEnded += OnMediaEnded;
        }

        /// <summary>
        /// 视频播放结束事件处理
        /// </summary>
        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            if (Settings.LoopEnabled)
            {
                // 当视频播放结束时，重新开始播放
                _mediaElement.Position = TimeSpan.Zero;
                _mediaElement.Play();
            }
        }

        public void ShowVideo(string videoPath)
        {
            // 验证视频路径
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath)) return;

            // 如果是同一个视频，不重复加载，但确保它在播放
            if (_currentVideoPath == videoPath)
            {
                // 如果当前视频已经暂停，则重新播放
                if (_mediaElement.Source != null && _mediaElement.CanPause)
                {
                    _mediaElement.Play();
                }
                return;
            }

            _currentVideoPath = videoPath;

            // 直接设置源并播放，不需要预加载
            _mediaElement.Source = new Uri(videoPath);
            _mediaElement.IsMuted = false;
            _mediaElement.Volume = Settings.Volume;
            _mediaElement.Position = TimeSpan.Zero; // 确保从开头播放

            // 立即显示并播放
            _mediaElement.Visibility = Visibility.Visible;
            _mediaElement.Play();
        }

        public void Mute()
        {
            // 使用暂停替代静音
            Pause();
        }

        public void Unmute()
        {
            // 使用恢复播放替代取消静音
            _mediaElement.IsMuted = false;
            _mediaElement.Volume = Settings.Volume;
            Resume();
        }

        // 暂停视频播放（用于节能模式）
        public void Pause()
        {
            if (_mediaElement.Source != null)
            {
                _mediaElement.Pause();
            }
        }

        // 恢复视频播放（用于退出节能模式）
        public void Resume()
        {
            if (_mediaElement.Source != null)
            {
                // 如果当前视频已经播放完毕，则重置到开头
                if (_mediaElement.NaturalDuration.HasTimeSpan &&
                    _mediaElement.Position >= _mediaElement.NaturalDuration.TimeSpan)
                {
                    _mediaElement.Position = TimeSpan.Zero;
                }
                _mediaElement.Play();
            }
        }

        public void Dispose()
        {
            // 清理资源
            if (_mediaElement != null)
            {
                _mediaElement.Source = null;
                _mediaElement.Close();
            }

            _currentVideoPath = null;
            GC.SuppressFinalize(this);
        }
    }
}

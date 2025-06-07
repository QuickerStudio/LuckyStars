using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.Versioning;

using LuckyStars.Players;
using LuckyStars.Utils;

namespace LuckyStars.Managers
{
    [SupportedOSPlatform("windows")]
    public class MediaManager : IDisposable
    {
        private readonly List<string> imagePaths = [];
        private readonly List<string> videoPaths = [];
        private readonly List<string> audioPaths = [];
        private int currentIndex;
        private readonly MusicPlayer musicPlayer;
        private readonly MediaElement mediaPlayer;
        private readonly VideoPlayer videoPlayer;
        private readonly PicturePlayer picturePlayer;

        private readonly string targetFolder;
        private readonly double[] allowedRatios;

        public MediaManager(MediaElement mediaPlayer, ImageBrush backgroundImageBrush, MusicPlayer musicPlayer, string targetFolder, double[] allowedRatios)
        {
            this.mediaPlayer = mediaPlayer;

            this.musicPlayer = musicPlayer;
            this.targetFolder = targetFolder;
            this.allowedRatios = allowedRatios;

            // 初始化播放器
            videoPlayer = new VideoPlayer(mediaPlayer);
            picturePlayer = new PicturePlayer(backgroundImageBrush);
        }

        public List<string> ImagePaths => imagePaths;
        public List<string> VideoPaths => videoPaths;
        public List<string> AudioPaths => audioPaths;

        public void LoadMediaPaths()
        {
            try
            {
                // 检查目标文件夹是否存在，如果不存在则跳过加载
                // 注意：不再在这里创建根目录，而是依赖 FileSystemMonitor
                if (!Directory.Exists(targetFolder))
                {
                    Console.WriteLine($"目标文件夹不存在: {targetFolder}，跳过加载媒体文件");
                    return;
                }

                imagePaths.Clear();
                videoPaths.Clear();
                audioPaths.Clear();
                // 使用SupportedFormats类获取支持的文件格式
                var imageWildcards = SupportedFormats.GetImageWildcards();
                var videoWildcards = SupportedFormats.GetVideoWildcards();
                var audioWildcards = SupportedFormats.GetAudioWildcards();

                // 加载图片文件
                foreach (var wildcard in imageWildcards)
                {
                    var files = Directory.GetFiles(targetFolder, wildcard);
                    foreach (var file in files)
                    {
                        try
                        {
                            // 使用PicturePlayer的静态方法检查图片宽高比
                            double ratio = PicturePlayer.GetImageAspectRatio(file);
                            if (ratio > 0) // 如果成功获取宽高比
                            {
                                double tolerance = 0.01;
                                if (allowedRatios.Any(ar => Math.Abs(ar - ratio) < tolerance))
                                {
                                    imagePaths.Add(file);
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                }

                // 加载视频文件
                foreach (var wildcard in videoWildcards)
                {
                    var files = Directory.GetFiles(targetFolder, wildcard);
                    videoPaths.AddRange(files);
                }

                // 加载音频文件
                foreach (var wildcard in audioWildcards)
                {
                    var files = Directory.GetFiles(targetFolder, wildcard);
                    audioPaths.AddRange(files);
                }

                var sortedImagePaths = imagePaths.OrderBy(path => path).ToList();
                var sortedVideoPaths = videoPaths.OrderBy(path => path).ToList();
                var sortedAudioPaths = audioPaths.OrderBy(path => path).ToList();

                imagePaths.Clear();
                videoPaths.Clear();
                audioPaths.Clear();

                imagePaths.AddRange(sortedImagePaths);
                videoPaths.AddRange(sortedVideoPaths);
                audioPaths.AddRange(sortedAudioPaths);
            }
            catch (Exception) { }
        }


        public bool LoadLastWallpaperState()
        {
            var (path, type) = Utils.RegistryManager.LoadLastWallpaperState();

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                if (type == Utils.RegistryManager.WallpaperType.Image)
                {
                    // 查找图片在列表中的索引
                    int index = imagePaths.FindIndex(p => p.Equals(path, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                    {
                        currentIndex = index;
                        ShowImage(path);
                        MuteVideoPlayer();
                        return true;
                    }
                }
                else if (type == Utils.RegistryManager.WallpaperType.Video)
                {
                    // 查找视频在列表中的索引
                    int index = videoPaths.FindIndex(p => p.Equals(path, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                    {
                        currentIndex = imagePaths.Count + index;
                        // 使用VideoPlayer直接播放视频
                        videoPlayer.ShowVideo(path);
                        UnmuteVideoPlayer();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // 加载上次壁纸状态失败
            }

            return false;
        }


        public void ShowMedia()
        {
            if (imagePaths.Count == 0 && videoPaths.Count == 0 && audioPaths.Count == 0)
            {
                return;
            }

            if (currentIndex < imagePaths.Count)
            {
                ShowImage(imagePaths[currentIndex]);
                MuteVideoPlayer(); // Mute video player when showing images

                // 保存当前壁纸状态到注册表
                Utils.RegistryManager.SaveLastWallpaperState(imagePaths[currentIndex], Utils.RegistryManager.WallpaperType.Image);
            }
            else if (currentIndex < imagePaths.Count + videoPaths.Count)
            {
                ShowVideo();
                UnmuteVideoPlayer(); // Unmute and set volume when showing videos

                // 保存当前壁纸状态到注册表
                int videoIndex = currentIndex - imagePaths.Count;
                if (videoIndex >= 0 && videoIndex < videoPaths.Count)
                {
                    Utils.RegistryManager.SaveLastWallpaperState(videoPaths[videoIndex], Utils.RegistryManager.WallpaperType.Video);
                }
            }
            else
            {
                PlayAudio();
            }

            currentIndex = (currentIndex + 1) % (imagePaths.Count + videoPaths.Count + audioPaths.Count);
        }


        public void ShowImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return;
            }

            // 保存当前壁纸状态到注册表
            Utils.RegistryManager.SaveLastWallpaperState(imagePath, Utils.RegistryManager.WallpaperType.Image);

            try
            {
                mediaPlayer.Stop();
                mediaPlayer.Visibility = Visibility.Collapsed;

                // 使用PicturePlayer显示图片
                bool success = picturePlayer.ShowPicture(imagePath);

                if (!success && imagePaths.Count > 1)
                {
                    currentIndex = (currentIndex + 1) % imagePaths.Count;
                    ShowImage(imagePaths[currentIndex]);
                }
            }
            catch (Exception ex)
            {
                // 显示图片错误
                if (imagePaths.Count > 1)
                {
                    currentIndex = (currentIndex + 1) % imagePaths.Count;
                    ShowImage(imagePaths[currentIndex]);
                }
            }
        }


        public void ShowVideo()
        {
            if (videoPaths.Count == 0)
            {
                return;
            }

            var videoIndex = currentIndex - imagePaths.Count;
            // 确保视频索引在有效范围内
            if (videoIndex >= 0 && videoIndex < videoPaths.Count)
            {
                // 保存当前壁纸状态到注册表
                Utils.RegistryManager.SaveLastWallpaperState(videoPaths[videoIndex], Utils.RegistryManager.WallpaperType.Video);
            }
            try
            {
                picturePlayer.ClearPicture();

                int videoIdx = currentIndex - imagePaths.Count;
                // 确保视频索引在有效范围内
                if (videoIdx >= videoPaths.Count)
                {
                    videoIdx = 0;
                }

                string videoPath = videoPaths[videoIdx];
                string extension = Path.GetExtension(videoPath).ToLowerInvariant();
                string[] supportedVideoExtensions = [".mp4", ".avi", ".mkv", ".webm", ".mov"];

                if (!supportedVideoExtensions.Contains(extension))
                {
                    throw new InvalidOperationException("Unsupported video format.");
                }

                // 使用优化后的VideoPlayer直接播放视频
                videoPlayer.ShowVideo(videoPath);
            }
            catch (Exception ex)
            {
                // 播放视频错误
                if (videoPaths.Count > 1)
                {
                    currentIndex = (currentIndex - imagePaths.Count + 1) % videoPaths.Count + imagePaths.Count;
                    ShowVideo();
                }
                else if (videoPaths.Count == 1)
                {
                    // 只有一个视频时，确保能循环播放
                    currentIndex = imagePaths.Count;
                    ShowVideo();
                }
            }
        }

        // 不再需要MediaEnded方法，由VideoPlayer内部处理循环播放

        [SupportedOSPlatform("windows7.0")]
        public void PlayAudio()
        {
            if (audioPaths.Count == 0)
            {
                return;
            }
            try
            {
                mediaPlayer.Visibility = Visibility.Collapsed;

                var audioIndex = currentIndex - imagePaths.Count - videoPaths.Count;
                // 确保音频索引在有效范围内
                if (audioIndex >= audioPaths.Count)
                {
                    audioIndex = 0;
                }

                var audioPath = audioPaths[audioIndex];
                // 使用新的音乐播放器播放音频
                // 注意：这里不使用await，因为我们希望异步播放
                // 传入true参数，表示检查注册表中的播放状态
                _ = musicPlayer.PlayMusic(audioPath, true);
            }
            catch (Exception ex)
            {
                // 播放音频错误
                if (audioPaths.Count > 1)
                {
                    currentIndex = (currentIndex - imagePaths.Count - videoPaths.Count + 1) % audioPaths.Count + imagePaths.Count + videoPaths.Count;
                    PlayAudio();
                }
                else if (audioPaths.Count == 1)
                {
                    // 只有一个音频时，确保能循环播放
                    currentIndex = imagePaths.Count + videoPaths.Count;
                    PlayAudio();
                }
            }
        }

        public void MuteVideoPlayer()
        {
            videoPlayer.Mute();
        }

        public void UnmuteVideoPlayer()
        {
            videoPlayer.Unmute();
        }

        // 检查当前是否正在播放视频
        public bool IsShowingVideo()
        {
            return currentIndex >= imagePaths.Count && videoPaths.Count > 0;
        }

        // 暂停视频播放（用于节能模式）
        public void PauseVideo()
        {
            if (IsShowingVideo())
            {
                videoPlayer.Pause();
            }
        }

        // 恢复视频播放（用于退出节能模式）
        public void ResumeVideo()
        {
            if (IsShowingVideo())
            {
                videoPlayer.Resume();
            }
        }

        // 显示静态图像（用于节能模式）
        [SupportedOSPlatform("windows")]
        public void ShowImage()
        {
            if (imagePaths.Count == 0)
                return;

            // 切换到图片模式
            int savedIndex = currentIndex;
            currentIndex = new Random().Next(0, imagePaths.Count);

            try
            {
                string imagePath = imagePaths[currentIndex];

                // 使用PicturePlayer直接设置图片（无动画效果）
                picturePlayer.SetPicture(imagePath);

                // 隐藏视频播放器
                videoPlayer.Pause();
                // 节能模式: 显示静态图像
            }
            catch (Exception ex)
            {
                // 显示静态图像错误
            }

            // 恢复原来的索引，以便退出节能模式时可以恢复原来的视频
            currentIndex = savedIndex;
        }


        public void NextImage()
        {
            if (imagePaths.Count == 0)
                return;

            // Increment the current index
            currentIndex++;

            // Wrap around if the index exceeds the number of images
            if (currentIndex >= imagePaths.Count)
                currentIndex = 0;

            // Load and display the image at the current index
            string nextImagePath = imagePaths[currentIndex];
            ShowImage(nextImagePath);
        }


        public void NextVideo()
        {
            if (videoPaths.Count == 0)
                return;

            // Increment the current index to point to the next video
            currentIndex++;

            // Wrap around if the index exceeds the number of videos
            if (currentIndex >= imagePaths.Count + videoPaths.Count)
                currentIndex = imagePaths.Count;

            // Play the video at the current index
            ShowVideo();
        }

        [SupportedOSPlatform("windows7.0")]
        public void TogglePlayPauseMusic()
        {
            musicPlayer.TogglePlayPause();
        }



        [SupportedOSPlatform("windows7.0")]
        public void HandleMusicFileDrop(string musicPath)
        {
            // 检查是否为支持的音乐格式
            if (SupportedFormats.IsAudioFile(musicPath))
            {
                try
                {
                    // 先停止当前正在播放的音乐
                    musicPlayer.StopMusic();

                    // 修改注册表信息为播放状态和非暂停状态
                    RegistryManager.SaveMusicPlayingState(true);
                    RegistryManager.SaveMusicPausedState(false);

                    // 立即开始播放
                    // 注意：这里不使用await，因为我们希望异步播放
                    // 传入false表示不检查注册表状态，因为这是用户主动拖放的音乐
                    _ = musicPlayer.PlayMusic(musicPath, false);

                    // 将音乐文件添加到音频路径列表中（如果不存在）
                    if (!audioPaths.Contains(musicPath))
                    {
                        audioPaths.Add(musicPath);
                    }
                }
                catch (Exception ex)
                {
                    // 处理播放错误
                    Console.WriteLine($"播放音乐文件错误: {ex.Message}");
                }
            }
        }

        public void HandleFolderDrop(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            try
            {
                // 使用SupportedFormats类获取支持的文件格式
                var imageExtensions = SupportedFormats.GetImageExtensions();
                var videoExtensions = SupportedFormats.GetVideoExtensions();
                var audioExtensions = SupportedFormats.GetAudioExtensions();

                // 获取所有文件
                var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

                int successCount = 0;
                int failCount = 0;

                foreach (var file in allFiles)
                {
                    try
                    {
                        string extension = Path.GetExtension(file).ToLowerInvariant();

                        // 检查文件类型是否支持
                        if (imageExtensions.Contains(extension) ||
                            videoExtensions.Contains(extension) ||
                            audioExtensions.Contains(extension))
                        {
                            // 复制到目标文件夹
                            string fileName = Path.GetFileName(file);
                            string destFile = Path.Combine(targetFolder, fileName);
                            File.Copy(file, destFile, true);
                            successCount++;
                        }
                    }
                    catch (Exception)
                    {
                        failCount++;
                    }
                }

                // 如果成功复制了文件，立即更新媒体路径
                if (successCount > 0)
                {
                    LoadMediaPaths();
                }
            }
            catch (Exception ex)
            {
                // 处理文件夹拖放失败
            }
        }

        public void Dispose()
        {
            // 释放 VideoPlayer 资源
            videoPlayer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

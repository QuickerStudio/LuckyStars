using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using LuckyStars.Utils;
using System.Numerics;
using LuckyStars.Players.Settings;

namespace LuckyStars.Players
{
    [SupportedOSPlatform("windows7.0")]
    public class MusicPlayer : IDisposable
    {
        public MusicPlayer()
        {
            // 初始化设置
            Settings = new MusicPlayerSettings();

            // 从注册表加载音量
            _volume = RegistryManager.LoadMusicVolume();
            // 从注册表加载播放状态
            isPlaying = RegistryManager.LoadMusicPlayingState();
            // 如果暂停状态为true，则覆盖播放状态
            if (RegistryManager.LoadMusicPausedState())
            {
                isPlaying = false;
            }
        }

        private WaveOutEvent? waveOut;
        private AudioFileReader? audioFile;
        private bool isPlaying = false;
        private bool isFirstPlay = true;

        // 音频参数常量
        private const float FADE_DURATION = 2.0f;
        private const float MIN_VOLUME = 0.0f;

        // 音量
        private float _volume = 0.2f; // 默认音量20%

        /// <summary>
        /// 音乐播放器设置
        /// </summary>
        public MusicPlayerSettings Settings { get; }

        public bool IsPlaying => isPlaying;

        /// <summary>
        /// 是否启用循环播放
        /// </summary>
        public bool IsLoopEnabled
        {
            get => Settings.LoopEnabled;
            set => Settings.LoopEnabled = value;
        }

        /// <summary>
        /// 是否启用环绕音效果
        /// </summary>
        public bool SurroundEnabled
        {
            get => Settings.SurroundEnabled;
            set => Settings.SurroundEnabled = value;
        }

        /// <summary>
        /// 环绕音效果深度（0.0-1.0）
        /// </summary>
        public float SurroundDepth
        {
            get => Settings.SurroundDepth;
            set => Settings.SurroundDepth = value;
        }

        /// <summary>
        /// 播放指定路径的音乐
        /// </summary>
        /// <param name="musicPath">音乐文件路径</param>
        /// <param name="checkPlayingState">是否检查注册表中的播放状态</param>
        /// <returns>异步任务</returns>
        public async Task PlayMusic(string musicPath, bool checkPlayingState = false)
        {
            try
            {
                if (string.IsNullOrEmpty(musicPath) || !File.Exists(musicPath))
                    return;

                // 暂停当前播放，但不释放播放器资源
                PausePlayback();

                // 如果是拖拽播放（checkPlayingState=false），则强制设置为播放状态
                if (!checkPlayingState)
                {
                    // 拖拽播放时强制设置为播放状态
                    isPlaying = true;

                    // 保存播放状态和暂停状态到注册表
                    RegistryManager.SaveMusicPlayingState(true);
                    RegistryManager.SaveMusicPausedState(false);
                }
                else
                {
                    // 如果需要检查播放状态，则从注册表加载
                    isPlaying = RegistryManager.LoadMusicPlayingState();
                    // 如果暂停状态为true，则覆盖播放状态
                    if (RegistryManager.LoadMusicPausedState())
                    {
                        isPlaying = false;
                    }
                }

                // 初始化音频（重用现有播放器）
                InitializeAudio(musicPath);

                // 设置音量
                if (audioFile != null)
                {
                    audioFile.Volume = _volume;
                }

                // 开始播放（如果应该播放）
                if (isPlaying && waveOut != null)
                {
                    // 使用高优先级线程开始播放，减少延迟
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (waveOut != null)
                            {
                                waveOut.Play();
                                Console.WriteLine("开始播放音乐");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"播放音乐时发生错误: {ex.Message}");
                        }
                    });

                    // 如果是第一次播放，使用淡入效果
                    if (isFirstPlay)
                    {
                        await FadeIn();
                        isFirstPlay = false;
                    }
                }

                // 输出调试信息
                Console.WriteLine($"开始播放音乐: {musicPath}, 播放状态: {isPlaying}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"播放音乐错误: {ex.Message}");
                throw; // 重新抛出异常，以便上层方法可以处理
            }
        }

        /// <summary>
        /// 暂停当前播放，但不释放资源
        /// </summary>
        private void PausePlayback()
        {
            try
            {
                if (waveOut != null && waveOut.PlaybackState != PlaybackState.Stopped)
                {
                    waveOut.Stop();
                }

                // 释放当前的音频文件资源，但保留播放器实例
                if (audioFile != null)
                {
                    audioFile.Dispose();
                    audioFile = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"暂停播放时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化音频播放组件
        /// </summary>
        /// <param name="musicPath">音乐文件路径</param>
        private void InitializeAudio(string musicPath)
        {
            try
            {
                // 创建新的AudioFileReader
                audioFile = new AudioFileReader(musicPath);

                // 设置初始音量
                audioFile.Volume = _volume;

                // 如果播放器实例不存在，则创建新的实例
                if (waveOut == null)
                {
                    // 初始化WaveOutEvent，使用较低的延迟设置
                    waveOut = new WaveOutEvent
                    {
                        DesiredLatency = 100, // 设置较低的延迟，默认是300ms
                        NumberOfBuffers = 2,  // 减少缓冲区数量，默认是3
                    };

                    // 注册播放结束事件
                    waveOut.PlaybackStopped += OnPlaybackStopped;

                    Console.WriteLine("创建新的音频播放器实例");
                }
                else
                {
                    // 确保播放器已停止
                    if (waveOut.PlaybackState != PlaybackState.Stopped)
                    {
                        waveOut.Stop();
                    }

                    Console.WriteLine("重用现有音频播放器实例");
                }

                // 应用环绕音效果（如果启用）
                ApplySurroundEffect();

                Console.WriteLine($"初始化音频成功: {musicPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化音频播放器错误: {ex.Message}");
                throw; // 重新抛出异常，以便上层方法可以处理
            }
        }

        /// <summary>
        /// 应用环绕音效果
        /// </summary>
        private void ApplySurroundEffect()
        {
            if (audioFile == null || waveOut == null)
                return;

            try
            {
                // 停止当前播放，以便重新初始化音频源
                bool wasPlaying = isPlaying;
                waveOut.Stop();

                // 创建基本音频源
                ISampleProvider baseProvider = audioFile;

                if (Settings.SurroundEnabled)
                {
                    // 创建环绕音效果
                    var surroundProvider = new SurroundSoundProvider(baseProvider, Settings.SurroundDepth);

                    // 初始化播放器使用环绕音效果
                    waveOut.Init(surroundProvider);
                    Console.WriteLine($"环绕音效果已应用，深度: {Settings.SurroundDepth}");
                }
                else
                {
                    // 不使用环绕音效果，直接初始化原始音频
                    waveOut.Init(audioFile);
                    Console.WriteLine("环绕音效果已禁用");
                }

                // 如果之前正在播放，则恢复播放
                if (wasPlaying)
                {
                    waveOut.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用环绕音效果时发生错误: {ex.Message}");
                // 出错时尝试使用原始音频
                try
                {
                    if (waveOut != null && audioFile != null)
                    {
                        waveOut.Init(audioFile);
                        if (isPlaying)
                        {
                            waveOut.Play();
                        }
                    }
                }
                catch
                {
                    // 忽略嵌套异常
                }
            }
        }

        /// <summary>
        /// 播放结束事件处理
        /// </summary>
        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            Console.WriteLine($"播放结束事件触发: 循环状态={Settings.LoopEnabled}");

            // 如果启用了循环播放
            if (Settings.LoopEnabled && isPlaying && audioFile != null && waveOut != null)
            {
                try
                {
                    // 使用Task.Run在后台线程上快速重新开始播放
                    Task.Run(() =>
                    {
                        if (audioFile != null && waveOut != null)
                        {
                            // 重置文件位置到开头
                            audioFile.Position = 0;
                            // 立即开始播放
                            waveOut.Play();
                            Console.WriteLine("循环播放已重新开始");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"循环播放错误: {ex.Message}");
                    isPlaying = false;
                }
            }
            else
            {
                isPlaying = false;
                Console.WriteLine("播放结束，不再循环");
            }
        }

        /// <summary>
        /// 执行音频淡入效果
        /// </summary>
        private async Task FadeIn()
        {
            if (audioFile == null || !isPlaying)
                return;

            try
            {
                // 设置初始音量为0
                if (audioFile != null)
                {
                    audioFile.Volume = MIN_VOLUME;

                    const int steps = 20;
                    const float stepDuration = FADE_DURATION / steps;
                    float volumeStep = _volume / steps;

                    // 逐步增加音量
                    for (int i = 1; i <= steps; i++)
                    {
                        if (!isPlaying || audioFile == null)
                            break;

                        float newVolume = volumeStep * i;
                        audioFile.Volume = newVolume;
                        await Task.Delay((int)(stepDuration * 1000));
                    }

                    // 确保最终音量正确
                    if (audioFile != null && isPlaying)
                    {
                        audioFile.Volume = _volume;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"音频淡入效果错误: {ex.Message}");
                // 出错时直接设置为目标音量
                if (audioFile != null && isPlaying)
                {
                    audioFile.Volume = _volume;
                }
            }
        }

        /// <summary>
        /// 切换播放/暂停状态
        /// </summary>
        public void TogglePlayPause()
        {
            if (waveOut == null || audioFile == null)
                return;

            if (isPlaying)
            {
                // 暂停播放
                waveOut?.Pause();
                isPlaying = false;
            }
            else
            {
                // 恢复播放
                waveOut?.Play();
                isPlaying = true;
            }

            // 保存播放状态和暂停状态到注册表
            RegistryManager.SaveMusicPlayingState(isPlaying);
            RegistryManager.SaveMusicPausedState(!isPlaying);
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="volume">音量值（0.0-1.0）</param>
        public void SetVolume(float volume)
        {
            // 确保音量在有效范围内
            _volume = Math.Clamp(volume, 0.0f, 1.0f);

            // 如果正在播放，则应用新的音量
            if (isPlaying && audioFile != null)
            {
                audioFile.Volume = _volume;
            }

            // 保存音量到注册表
            RegistryManager.SaveMusicVolume(_volume);
        }

        /// <summary>
        /// 获取当前音量
        /// </summary>
        /// <returns>当前音量值（0.0-1.0）</returns>
        public float GetVolume()
        {
            return _volume;
        }

        // IsPlaying 属性已在类的开头定义



        /// <summary>
        /// 停止音乐播放并释放资源
        /// </summary>
        /// <param name="fullDispose">是否完全释放资源，默认为false</param>
        public void StopMusic(bool fullDispose = false)
        {
            try
            {
                // 使用Task.Run在后台线程上异步停止播放，避免阻塞主线程
                Task.Run(() =>
                {
                    try
                    {
                        if (waveOut != null)
                        {
                            // 停止播放
                            if (waveOut.PlaybackState != PlaybackState.Stopped)
                            {
                                waveOut.Stop();
                            }

                            // 只有在需要完全释放资源时才释放播放器
                            if (fullDispose)
                            {
                                // 先移除事件处理器，避免在释放资源时触发事件
                                waveOut.PlaybackStopped -= OnPlaybackStopped;
                                waveOut.Dispose();
                                waveOut = null;
                                Console.WriteLine("播放器资源已完全释放");
                            }
                        }

                        // 释放音频文件资源
                        if (audioFile != null)
                        {
                            audioFile.Dispose();
                            audioFile = null;
                        }

                        Console.WriteLine("音频文件资源已释放");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"释放音频资源时发生错误: {ex.Message}");
                    }
                });

                isPlaying = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止音乐时发生错误: {ex.Message}");
                isPlaying = false;
            }
        }

        /// <summary>
        /// 释放所有资源
        /// </summary>
        public void Dispose()
        {
            // 在应用程序关闭时完全释放资源
            StopMusic(true);
            GC.SuppressFinalize(this);
        }
    }
}
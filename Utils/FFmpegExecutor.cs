using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;

namespace LuckyStars.Utils
{
    /// <summary>
    /// FFmpeg命令执行器，负责执行FFmpeg命令并处理输出
    /// </summary>
    public class FFmpegExecutor : IDisposable
    {
        /// <summary>
        /// FFmpeg进程
        /// </summary>
        private Process _ffmpegProcess;
        
        /// <summary>
        /// FFmpeg可执行文件路径
        /// </summary>
        private string _ffmpegPath;
        
        /// <summary>
        /// 转换任务的取消令牌源
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;
        
        /// <summary>
        /// 命令执行超时（毫秒）
        /// </summary>
        private int _executionTimeout = 300000; // 默认5分钟
        
        /// <summary>
        /// 命令输出事件
        /// </summary>
        public event EventHandler<string> OutputReceived;
        
        /// <summary>
        /// 错误输出事件
        /// </summary>
        public event EventHandler<string> ErrorReceived;
        
        /// <summary>
        /// 任务完成事件
        /// </summary>
        public event EventHandler<FFmpegTaskCompletedEventArgs> TaskCompleted;
        
        /// <summary>
        /// 进度更新事件
        /// </summary>
        public event EventHandler<FFmpegProgressEventArgs> ProgressUpdated;
        
        /// <summary>
        /// FFmpeg任务结果类
        /// </summary>
        public class FFmpegResult
        {
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Success { get; set; }
            
            /// <summary>
            /// 退出码
            /// </summary>
            public int ExitCode { get; set; }
            
            /// <summary>
            /// 标准输出
            /// </summary>
            public string StandardOutput { get; set; }
            
            /// <summary>
            /// 错误输出
            /// </summary>
            public string ErrorOutput { get; set; }
            
            /// <summary>
            /// 输出文件路径
            /// </summary>
            public string OutputPath { get; set; }
            
            /// <summary>
            /// 执行时长（毫秒）
            /// </summary>
            public long ExecutionTimeMs { get; set; }
        }
        
        /// <summary>
        /// FFmpeg任务完成事件参数
        /// </summary>
        public class FFmpegTaskCompletedEventArgs : EventArgs
        {
            /// <summary>
            /// 任务结果
            /// </summary>
            public FFmpegResult Result { get; }
            
            /// <summary>
            /// 任务标识
            /// </summary>
            public string TaskId { get; }
            
            /// <summary>
            /// 是否被取消
            /// </summary>
            public bool Cancelled { get; }
            
            public FFmpegTaskCompletedEventArgs(FFmpegResult result, string taskId, bool cancelled = false)
            {
                Result = result;
                TaskId = taskId;
                Cancelled = cancelled;
            }
        }
        
        /// <summary>
        /// FFmpeg进度事件参数
        /// </summary>
        public class FFmpegProgressEventArgs : EventArgs
        {
            /// <summary>
            /// 处理进度（0-100）
            /// </summary>
            public double Progress { get; }
            
            /// <summary>
            /// 当前处理时间
            /// </summary>
            public TimeSpan CurrentTime { get; }
            
            /// <summary>
            /// 总时长
            /// </summary>
            public TimeSpan TotalDuration { get; }
            
            /// <summary>
            /// 当前帧
            /// </summary>
            public int Frame { get; }
            
            /// <summary>
            /// 帧率
            /// </summary>
            public double Fps { get; }
            
            /// <summary>
            /// 处理速度
            /// </summary>
            public double Speed { get; }
            
            /// <summary>
            /// 任务标识
            /// </summary>
            public string TaskId { get; }
            
            /// <summary>
            /// 构造函数
            /// </summary>
            public FFmpegProgressEventArgs(double progress, TimeSpan currentTime, TimeSpan totalDuration, 
                int frame, double fps, double speed, string taskId)
            {
                Progress = progress;
                CurrentTime = currentTime;
                TotalDuration = totalDuration;
                Frame = frame;
                Fps = fps;
                Speed = speed;
                TaskId = taskId;
            }
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ffmpegPath">FFmpeg可执行文件路径</param>
        /// <param name="timeout">执行超时（毫秒）</param>
        public FFmpegExecutor(string ffmpegPath, int timeout = 300000)
        {
            _ffmpegPath = ffmpegPath;
            _executionTimeout = timeout;
            
            if (!File.Exists(_ffmpegPath))
            {
                throw new FileNotFoundException("找不到FFmpeg可执行文件", _ffmpegPath);
            }
        }
        
        /// <summary>
        /// 异步执行FFmpeg命令
        /// </summary>
        /// <param name="arguments">命令行参数</param>
        /// <param name="taskId">任务标识</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        public async Task<FFmpegResult> ExecuteAsync(string arguments, string taskId = null, CancellationToken? cancellationToken = null)
        {
            taskId = taskId ?? Guid.NewGuid().ToString().Substring(0, 8);
            cancellationToken = cancellationToken ?? CancellationToken.None;
            
            // 准备结果对象
            var result = new FFmpegResult
            {
                Success = false,
                OutputPath = GetOutputPathFromArguments(arguments)
            };
            
            // 创建输出缓冲区
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            // 准备进程启动信息
            var startInfo = new ProcessStartInfo(_ffmpegPath, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            
            try
            {
                using (_ffmpegProcess = new Process())
                {
                    _ffmpegProcess.StartInfo = startInfo;
                    
                    // 设置输出和错误处理事件
                    _ffmpegProcess.OutputDataReceived += (sender, e) => {
                        if (e.Data != null)
                        {
                            outputBuilder.AppendLine(e.Data);
                            OutputReceived?.Invoke(this, e.Data);
                        }
                    };
                    
                    _ffmpegProcess.ErrorDataReceived += (sender, e) => {
                        if (e.Data != null)
                        {
                            errorBuilder.AppendLine(e.Data);
                            ErrorReceived?.Invoke(this, e.Data);
                            
                            // 尝试解析进度信息
                            ParseProgress(e.Data, taskId);
                        }
                    };
                    
                    // 记录开始时间
                    var startTime = DateTime.Now;
                    
                    // 启动进程
                    _ffmpegProcess.Start();
                    _ffmpegProcess.BeginOutputReadLine();
                    _ffmpegProcess.BeginErrorReadLine();
                    
                    // 等待进程完成，支持取消
                    await Task.Run(() => {
                        try
                        {
                            if (!_ffmpegProcess.WaitForExit(_executionTimeout) || 
                                cancellationToken.Value.IsCancellationRequested)
                            {
                                try
                                {
                                    if (!_ffmpegProcess.HasExited)
                                        _ffmpegProcess.Kill();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"结束FFmpeg进程失败: {ex.Message}");
                                }
                                
                                if (cancellationToken.Value.IsCancellationRequested)
                                {
                                    TaskCompleted?.Invoke(this, new FFmpegTaskCompletedEventArgs(result, taskId, true));
                                    throw new TaskCanceledException();
                                }
                                else
                                {
                                    throw new TimeoutException("FFmpeg执行超时");
                                }
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            throw; // 重新抛出取消异常
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"等待FFmpeg进程时出错: {ex.Message}");
                            throw;
                        }
                    }, cancellationToken.Value);
                    
                    // 计算执行时间
                    var endTime = DateTime.Now;
                    result.ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
                    
                    // 获取退出码
                    result.ExitCode = _ffmpegProcess.ExitCode;
                    result.Success = result.ExitCode == 0;
                    
                    // 保存输出
                    result.StandardOutput = outputBuilder.ToString();
                    result.ErrorOutput = errorBuilder.ToString();
                    
                    // 触发任务完成事件
                    TaskCompleted?.Invoke(this, new FFmpegTaskCompletedEventArgs(result, taskId));
                    
                    return result;
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("FFmpeg任务已取消");
                result.ErrorOutput = errorBuilder.ToString();
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行FFmpeg命令时出错: {ex.Message}");
                result.ErrorOutput = $"{errorBuilder}\n执行出错: {ex.Message}";
                return result;
            }
            finally
            {
                _ffmpegProcess = null;
            }
        }
        
        /// <summary>
        /// 根据文件类型创建转换命令
        /// </summary>
        /// <param name="inputPath">输入文件路径</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="mediaType">媒体类型</param>
        /// <param name="quality">质量（1-100）</param>
        /// <returns>FFmpeg命令参数</returns>
        public string CreateConversionCommand(string inputPath, string outputPath, MediaTypeDetector.MediaType mediaType, int quality = 80)
        {
            if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("输入和输出路径不能为空");
            }
            
            StringBuilder command = new StringBuilder();
            command.Append($"-y -i \"{inputPath}\" "); // -y 表示覆盖输出文件
            
            string outputExt = Path.GetExtension(outputPath).ToLowerInvariant();
            
            switch (mediaType)
            {
                case MediaTypeDetector.MediaType.Image:
                case MediaTypeDetector.MediaType.AnimatedGif:
                    // 图像转换
                    if (outputExt == ".png")
                    {
                        int compressionLevel = Math.Min(9, Math.Max(0, (100 - quality) / 10));
                        command.Append($"-compression_level {compressionLevel} ");
                    }
                    else if (outputExt == ".jpg" || outputExt == ".jpeg")
                    {
                        command.Append($"-q:v {Math.Max(1, Math.Min(31, (100 - quality) / 3))} ");
                    }
                    break;
                    
                case MediaTypeDetector.MediaType.Video:
                    // 视频转换到静态图像
                    if (outputExt == ".png" || outputExt == ".jpg" || outputExt == ".jpeg")
                    {
                        command.Append("-ss 0 -vframes 1 "); // 截取第一帧
                        if (outputExt == ".jpg" || outputExt == ".jpeg")
                        {
                            command.Append($"-q:v {Math.Max(1, Math.Min(31, (100 - quality) / 3))} ");
                        }
                    }
                    // 视频到视频转换
                    else
                    {
                        int crf = Math.Max(18, Math.Min(28, 28 - quality / 5));
                        command.Append($"-c:v libx264 -preset medium -crf {crf} -c:a aac -b:a 128k ");
                    }
                    break;
                    
                case MediaTypeDetector.MediaType.Web:
                    // HTML文件处理（可能需要特殊处理，如截图等）
                    break;
                    
                default:
                    // 其他类型默认转换
                    command.Append("-c copy ");
                    break;
            }
            
            command.Append($"\"{outputPath}\"");
            
            Debug.WriteLine($"创建的FFmpeg命令: {command}");
            return command.ToString();
        }
        
        /// <summary>
        /// 提取输出路径
        /// </summary>
        /// <param name="arguments">命令行参数</param>
        /// <returns>输出文件路径</returns>
        private string GetOutputPathFromArguments(string arguments)
        {
            try
            {
                // 尝试找到最后一个引号包裹的参数，通常是输出路径
                var matches = Regex.Matches(arguments, "\"([^\"]*)\"");
                if (matches.Count > 0)
                {
                    return matches[matches.Count - 1].Groups[1].Value;
                }
                
                // 如果没有找到引号包裹的参数，尝试找到最后一个参数
                var parts = arguments.Split(' ');
                if (parts.Length > 0)
                {
                    return parts[parts.Length - 1].Trim('"');
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"解析输出路径失败: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 解析进度信息
        /// </summary>
        /// <param name="line">输出行</param>
        /// <param name="taskId">任务标识</param>
        private void ParseProgress(string line, string taskId)
        {
            if (string.IsNullOrEmpty(line))
                return;
                
            try
            {
                // FFmpeg进度行通常包含 time=HH:MM:SS.MS
                TimeSpan currentTime = TimeSpan.Zero;
                TimeSpan totalDuration = TimeSpan.Zero;
                int frame = 0;
                double fps = 0;
                double speed = 0;
                double progress = 0;
                
                // 解析当前时间
                var timeMatch = Regex.Match(line, @"time=(\d+):(\d+):(\d+\.\d+)");
                if (timeMatch.Success)
                {
                    int hours = int.Parse(timeMatch.Groups[1].Value);
                    int minutes = int.Parse(timeMatch.Groups[2].Value);
                    double seconds = double.Parse(timeMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                    currentTime = TimeSpan.FromSeconds(hours * 3600 + minutes * 60 + seconds);
                }
                
                // 解析总时长（通常在开始处理前输出）
                var durationMatch = Regex.Match(line, @"Duration: (\d+):(\d+):(\d+\.\d+)");
                if (durationMatch.Success)
                {
                    int hours = int.Parse(durationMatch.Groups[1].Value);
                    int minutes = int.Parse(durationMatch.Groups[2].Value);
                    double seconds = double.Parse(durationMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                    totalDuration = TimeSpan.FromSeconds(hours * 3600 + minutes * 60 + seconds);
                }
                
                // 解析帧信息
                var frameMatch = Regex.Match(line, @"frame=\s*(\d+)");
                if (frameMatch.Success)
                {
                    frame = int.Parse(frameMatch.Groups[1].Value);
                }
                
                // 解析FPS
                var fpsMatch = Regex.Match(line, @"fps=\s*([\d.]+)");
                if (fpsMatch.Success)
                {
                    fps = double.Parse(fpsMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }
                
                // 解析速度
                var speedMatch = Regex.Match(line, @"speed=\s*([\d.]+)x");
                if (speedMatch.Success)
                {
                    speed = double.Parse(speedMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }
                
                // 计算进度百分比
                if (totalDuration != TimeSpan.Zero && currentTime != TimeSpan.Zero)
                {
                    progress = (currentTime.TotalMilliseconds / totalDuration.TotalMilliseconds) * 100;
                    progress = Math.Min(100, Math.Max(0, progress)); // 确保在0-100范围内
                    
                    // 触发进度事件
                    ProgressUpdated?.Invoke(this, new FFmpegProgressEventArgs(
                        progress, currentTime, totalDuration, frame, fps, speed, taskId));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"解析FFmpeg进度信息失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取媒体文件信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>媒体信息字典</returns>
        public async Task<Dictionary<string, string>> GetMediaInfoAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);
                
            var mediaInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            // 使用FFprobe获取媒体信息
            string arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"";
            
            var result = await ExecuteAsync(arguments.Replace("ffmpeg", "ffprobe"));
            
            if (!result.Success)
            {
                Debug.WriteLine($"获取媒体信息失败: {result.ErrorOutput}");
                return mediaInfo;
            }
            
            // 解析输出信息
            try
            {
                // 简单解析主要信息
                var durationMatch = Regex.Match(result.StandardOutput, @"""duration""\s*:\s*""([\d.]+)""");
                if (durationMatch.Success)
                {
                    mediaInfo["Duration"] = durationMatch.Groups[1].Value;
                }
                
                var bitrateMatch = Regex.Match(result.StandardOutput, @"""bit_rate""\s*:\s*""([\d.]+)""");
                if (bitrateMatch.Success)
                {
                    mediaInfo["BitRate"] = bitrateMatch.Groups[1].Value;
                }
                
                // 视频流信息
                var widthMatch = Regex.Match(result.StandardOutput, @"""width""\s*:\s*(\d+)");
                var heightMatch = Regex.Match(result.StandardOutput, @"""height""\s*:\s*(\d+)");
                if (widthMatch.Success && heightMatch.Success)
                {
                    mediaInfo["Width"] = widthMatch.Groups[1].Value;
                    mediaInfo["Height"] = heightMatch.Groups[1].Value;
                    mediaInfo["Resolution"] = $"{widthMatch.Groups[1].Value}x{heightMatch.Groups[1].Value}";
                }
                
                var codecMatch = Regex.Match(result.StandardOutput, @"""codec_name""\s*:\s*""([^""]+)""");
                if (codecMatch.Success)
                {
                    mediaInfo["Codec"] = codecMatch.Groups[1].Value;
                }
                
                var fpsMatch = Regex.Match(result.StandardOutput, @"""r_frame_rate""\s*:\s*""(\d+)\/(\d+)""");
                if (fpsMatch.Success)
                {
                    int num = int.Parse(fpsMatch.Groups[1].Value);
                    int den = int.Parse(fpsMatch.Groups[2].Value);
                    float fps = (float)num / den;
                    mediaInfo["FPS"] = fps.ToString("0.##");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"解析媒体信息失败: {ex.Message}");
            }
            
            return mediaInfo;
        }
        
        /// <summary>
        /// 取消当前任务
        /// </summary>
        public void CancelCurrentTask()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                
                if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                {
                    _ffmpegProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"取消FFmpeg任务失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 验证文件格式并在必要时转换
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="targetFormat">目标格式（如.png, .mp4）</param>
        /// <param name="outputFolder">输出目录</param>
        /// <returns>转换后的文件路径（如无需转换则为原路径）</returns>
        public async Task<string> ValidateAndConvertIfNeededAsync(string filePath, string targetFormat, string outputFolder)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("文件不存在", filePath);
            }
            
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // 如果扩展名已经匹配目标格式，无需转换
            if (extension.Equals(targetFormat, StringComparison.OrdinalIgnoreCase))
            {
                return filePath;
            }
            
            // 检测媒体类型
            var mediaType = MediaTypeDetector.GetMediaType(filePath);
            
            // 确定输出路径
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string safeFileName = FileUtils.MakeSafeFileName(fileName);
            string outputPath = Path.Combine(outputFolder, $"{safeFileName}_{DateTime.Now.Ticks.ToString().Substring(10)}{targetFormat}");
            
            // 创建转换命令
            string command = CreateConversionCommand(filePath, outputPath, mediaType);
            
            // 执行转换
            var result = await ExecuteAsync(command);
            
            if (result.Success && File.Exists(outputPath))
            {
                Debug.WriteLine($"文件转换成功: {filePath} -> {outputPath}");
                return outputPath;
            }
            else
            {
                Debug.WriteLine($"文件转换失败: {result.ErrorOutput}");
                return filePath; // 转换失败时返回原路径
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                CancelCurrentTask();
                _cancellationTokenSource?.Dispose();
                _ffmpegProcess?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放FFmpegExecutor资源失败: {ex.Message}");
            }
        }
    }
}
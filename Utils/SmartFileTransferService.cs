using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 智能文件传输服务 - 专注于线程生命周期管理和资源释放
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class SmartFileTransferService
    {
        /// <summary>
        /// 当根目录不存在时触发的事件
        /// </summary>
        public event EventHandler<string>? RootDirectoryMissing;

        private readonly string _targetFolder;
        private readonly ConcurrentDictionary<int, Process> _activeProcesses = new();
        private readonly object _lockObject = new();
        private int _nextProcessId = 0;

        /// <summary>
        /// 初始化智能文件传输服务
        /// </summary>
        /// <param name="targetFolder">目标文件夹路径</param>
        public SmartFileTransferService(string targetFolder)
        {
            _targetFolder = targetFolder;

            // 检查目标文件夹是否存在，如果不存在则触发事件
            CheckTargetFolder();
        }

        /// <summary>
        /// 检查目标文件夹是否存在，如果不存在则触发事件
        /// </summary>
        /// <returns>目标文件夹是否存在</returns>
        private bool CheckTargetFolder()
        {
            if (!Directory.Exists(_targetFolder))
            {
                // 触发事件，通知 FileTransferManager 创建根目录
                RootDirectoryMissing?.Invoke(this, _targetFolder);

                // 再次检查目录是否已创建
                return Directory.Exists(_targetFolder);
            }

            return true;
        }

        /// <summary>
        /// 处理文件夹传输
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>处理结果</returns>
        public async Task<(int total, int success, string? firstAudioFile)> TransferFolderAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return (0, 0, null);

            // 检查目标文件夹是否存在，如果不存在则触发事件
            if (!CheckTargetFolder())
                return (0, 0, null);

            try
            {
                // 取消之前的传输任务
                CancelTransfer();

                // 获取文件夹属性
                var folderInfo = GetFolderInfo(folderPath);
                Console.WriteLine($"文件夹 {folderPath} 包含 {folderInfo.fileCount} 个文件和 {folderInfo.folderCount} 个子文件夹");

                // 查找视频文件
                var videoFiles = FindVideoFiles(folderPath);
                Console.WriteLine($"找到 {videoFiles.Count} 个视频文件");

                // 查找音乐文件
                var audioFiles = FindAudioFiles(folderPath);
                Console.WriteLine($"找到 {audioFiles.Count} 个音乐文件");

                // 创建取消令牌
                using var cts = new CancellationTokenSource();

                // 处理结果变量
                int successCount = 0;
                string? firstMediaFile = null;

                // 根据文件类型选择不同的处理策略
                if (videoFiles.Count > 0 && audioFiles.Count > 0)
                {
                    // 同时有视频和音乐文件，分别处理
                    Console.WriteLine("同时处理视频和音乐文件");

                    // 处理视频文件
                    var videoTasks = ProcessVideoFiles(videoFiles);

                    // 处理音乐文件
                    var audioTask = ProcessAudioFilesBatch(audioFiles, cts.Token);

                    // 处理其他文件（排除视频和音乐）
                    var otherFilesTask = Task.Run(() =>
                    {
                        return RunRobocopy(folderPath, _targetFolder, cts.Token, excludeVideosAndAudios: true);
                    });

                    // 等待所有任务完成
                    await Task.WhenAll(videoTasks);
                    var audioResult = await audioTask;
                    var otherResult = await otherFilesTask;

                    // 汇总结果
                    successCount = videoFiles.Count + audioResult + otherResult.successCount;

                    // 优先返回音频文件，其次是视频文件
                    if (audioFiles.Count > 0)
                    {
                        string audioFileName = Path.GetFileName(audioFiles[0]);
                        string targetAudioPath = Path.Combine(_targetFolder, audioFileName);
                        if (File.Exists(targetAudioPath))
                        {
                            firstMediaFile = targetAudioPath;
                        }
                    }

                    if (firstMediaFile == null && videoFiles.Count > 0)
                    {
                        string videoFileName = Path.GetFileName(videoFiles[0]);
                        string targetVideoPath = Path.Combine(_targetFolder, videoFileName);
                        if (File.Exists(targetVideoPath))
                        {
                            firstMediaFile = targetVideoPath;
                        }
                    }

                    if (firstMediaFile == null)
                    {
                        firstMediaFile = otherResult.firstAudioFile;
                    }
                }
                else if (videoFiles.Count > 0)
                {
                    // 只有视频文件
                    Console.WriteLine("只处理视频文件");

                    // 处理视频文件
                    var videoTasks = ProcessVideoFiles(videoFiles);

                    // 处理非视频文件
                    var otherFilesTask = Task.Run(() =>
                    {
                        return RunRobocopy(folderPath, _targetFolder, cts.Token, excludeVideos: true);
                    });

                    // 等待所有任务完成
                    await Task.WhenAll(videoTasks);
                    var otherResult = await otherFilesTask;

                    // 汇总结果
                    successCount = videoFiles.Count + otherResult.successCount;

                    // 返回第一个视频文件路径作为结果
                    if (videoFiles.Count > 0)
                    {
                        string videoFileName = Path.GetFileName(videoFiles[0]);
                        string targetVideoPath = Path.Combine(_targetFolder, videoFileName);
                        if (File.Exists(targetVideoPath))
                        {
                            firstMediaFile = targetVideoPath;
                        }
                    }

                    if (firstMediaFile == null)
                    {
                        firstMediaFile = otherResult.firstAudioFile;
                    }
                }
                else if (audioFiles.Count > 0)
                {
                    // 只有音乐文件
                    Console.WriteLine("只处理音乐文件");

                    // 处理音乐文件
                    var audioResult = await ProcessAudioFilesBatch(audioFiles, cts.Token);

                    // 处理非音乐文件
                    var otherFilesTask = Task.Run(() =>
                    {
                        return RunRobocopy(folderPath, _targetFolder, cts.Token, excludeAudios: true);
                    });

                    var otherResult = await otherFilesTask;

                    // 汇总结果
                    successCount = audioResult + otherResult.successCount;

                    // 返回第一个音乐文件路径作为结果
                    if (audioFiles.Count > 0)
                    {
                        string audioFileName = Path.GetFileName(audioFiles[0]);
                        string targetAudioPath = Path.Combine(_targetFolder, audioFileName);
                        if (File.Exists(targetAudioPath))
                        {
                            firstMediaFile = targetAudioPath;
                        }
                    }
                }
                else
                {
                    // 没有视频和音乐文件，使用常规方法处理
                    Console.WriteLine("使用常规方法处理文件");

                    var result = await Task.Run(() =>
                    {
                        return RunRobocopy(folderPath, _targetFolder, cts.Token);
                    });

                    successCount = result.successCount;
                    firstMediaFile = result.firstAudioFile;
                }

                return (folderInfo.fileCount, successCount, firstMediaFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"传输文件夹时出错: {ex.Message}");
                return (0, 0, null);
            }
        }

        /// <summary>
        /// 处理单个文件传输
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>处理结果</returns>
        public (bool success, string? audioPath) TransferSingleFile(string filePath, bool playAudio = true)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return (false, null);

            // 检查目标文件夹是否存在，如果不存在则触发事件
            if (!CheckTargetFolder())
                return (false, null);

            try
            {
                // 检查文件是否支持
                if (!SupportedFormats.IsSupportedFile(filePath))
                    return (false, null);

                string fileName = Path.GetFileName(filePath);
                string? sourceDir = Path.GetDirectoryName(filePath);

                // 确保源目录不为空
                if (string.IsNullOrEmpty(sourceDir))
                {
                    return (false, null);
                }

                // 检查是否是视频文件
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };

                if (videoExtensions.Contains(extension))
                {
                    // 使用专门的视频处理方法
                    Console.WriteLine($"使用专门的视频处理方法处理文件: {fileName}");

                    // 创建任务并等待完成
                    var task = Task.Run(() => ProcessSingleVideoFile(filePath));
                    task.Wait(); // 等待任务完成

                    // 检查文件是否成功复制
                    string targetPath = Path.Combine(_targetFolder, fileName);
                    bool success = File.Exists(targetPath);

                    // 返回视频文件路径
                    return (success, success ? targetPath : null);
                }
                else
                {
                    // 检查目标文件是否已存在
                    string targetPath = Path.Combine(_targetFolder, fileName);
                    if (File.Exists(targetPath))
                    {
                        Console.WriteLine($"文件已存在，跳过: {fileName}");
                        bool isAudioFile = SupportedFormats.IsAudioFile(filePath);
                        return (true, isAudioFile && playAudio ? targetPath : null);
                    }

                    // 使用 Robocopy 处理非视频文件
                    using var cts = new CancellationTokenSource();
                    var (successCount, firstAudioFile) = RunRobocopy(sourceDir, _targetFolder, cts.Token, fileName);

                    bool success = successCount > 0;
                    bool isAudio = SupportedFormats.IsAudioFile(filePath);

                    return (success, isAudio && playAudio && success ? firstAudioFile : null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"传输单个文件时出错: {ex.Message}");
                return (false, null);
            }
        }

        /// <summary>
        /// 取消当前传输任务
        /// </summary>
        public void CancelTransfer()
        {
            try
            {
                // 终止所有活动进程
                foreach (var process in _activeProcesses.Values)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch
                    {
                        // 忽略终止进程时的错误
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                // 清空进程字典
                _activeProcesses.Clear();
            }
            catch
            {
                // 忽略取消传输时的错误
            }
        }

        /// <summary>
        /// 获取文件夹信息（文件数量和文件夹数量）
        /// </summary>
        private (int fileCount, int folderCount) GetFolderInfo(string folderPath)
        {
            try
            {
                // 获取支持的文件格式
                var supportedExtensions = SupportedFormats.GetAllSupportedExtensions();

                // 计算文件数量（只计算支持的文件格式）
                int fileCount = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Count(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

                // 计算子文件夹数量
                int folderCount = Directory.GetDirectories(folderPath).Length;

                return (fileCount, folderCount);
            }
            catch
            {
                return (0, 0);
            }
        }

        /// <summary>
        /// 查找指定文件夹中的所有视频文件
        /// </summary>
        private List<string> FindVideoFiles(string folderPath)
        {
            var result = new List<string>();
            var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };

            try
            {
                // 获取顶级目录中的视频文件
                foreach (var ext in videoExtensions)
                {
                    var files = Directory.GetFiles(folderPath, $"*{ext}", SearchOption.TopDirectoryOnly);
                    result.AddRange(files);
                }

                // 获取子目录中的视频文件
                foreach (var dir in Directory.GetDirectories(folderPath))
                {
                    foreach (var ext in videoExtensions)
                    {
                        var files = Directory.GetFiles(dir, $"*{ext}", SearchOption.AllDirectories);
                        result.AddRange(files);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找视频文件时出错: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 查找指定文件夹中的所有音乐文件
        /// </summary>
        private List<string> FindAudioFiles(string folderPath)
        {
            var result = new List<string>();
            var audioExtensions = new[] { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".wma", ".m4a" };

            try
            {
                // 获取顶级目录中的音乐文件
                foreach (var ext in audioExtensions)
                {
                    var files = Directory.GetFiles(folderPath, $"*{ext}", SearchOption.TopDirectoryOnly);
                    result.AddRange(files);
                }

                // 获取子目录中的音乐文件
                foreach (var dir in Directory.GetDirectories(folderPath))
                {
                    foreach (var ext in audioExtensions)
                    {
                        var files = Directory.GetFiles(dir, $"*{ext}", SearchOption.AllDirectories);
                        result.AddRange(files);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找音乐文件时出错: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 处理视频文件列表，为每个视频文件创建单独的线程
        /// </summary>
        private Task[] ProcessVideoFiles(List<string> videoFiles)
        {
            var tasks = new List<Task>();

            foreach (var videoFile in videoFiles)
            {
                // 为每个视频文件创建单独的任务
                var task = Task.Run(() => ProcessSingleVideoFile(videoFile));
                tasks.Add(task);
            }

            return tasks.ToArray();
        }

        /// <summary>
        /// 处理单个视频文件
        /// </summary>
        private void ProcessSingleVideoFile(string videoFilePath)
        {
            try
            {
                string fileName = Path.GetFileName(videoFilePath);
                string targetPath = Path.Combine(_targetFolder, fileName);

                // 如果目标文件已存在，则跳过
                if (File.Exists(targetPath))
                {
                    Console.WriteLine($"视频文件已存在，跳过: {fileName}");
                    return;
                }

                Console.WriteLine($"开始处理视频文件: {fileName}");

                // 使用文件复制而不是 Robocopy 来处理单个视频文件
                // 这样可以更好地控制进度和错误处理
                using (var sourceStream = new FileStream(videoFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                using (var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                {
                    // 缓冲区大小设置为 1MB，适合大文件传输
                    byte[] buffer = new byte[1024 * 1024];
                    int bytesRead;
                    long totalBytesRead = 0;
                    long fileSize = new FileInfo(videoFilePath).Length;

                    // 读取并写入数据
                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        targetStream.Write(buffer, 0, bytesRead);

                        // 更新进度
                        totalBytesRead += bytesRead;
                        int progressPercentage = (int)((double)totalBytesRead / fileSize * 100);

                        // 每 10% 报告一次进度
                        if (progressPercentage % 10 == 0)
                        {
                            Console.WriteLine($"视频文件 {fileName} 传输进度: {progressPercentage}%");
                        }
                    }
                }

                Console.WriteLine($"视频文件处理完成: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理视频文件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量处理音乐文件，使用 TPL Dataflow 实现高效并行处理
        /// </summary>
        private async Task<int> ProcessAudioFilesBatch(List<string> audioFiles, CancellationToken cancellationToken)
        {
            if (audioFiles.Count == 0)
                return 0;

            Console.WriteLine($"开始批量处理 {audioFiles.Count} 个音乐文件");

            // 创建处理管道
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount, // 使用处理器核心数作为并行度
                CancellationToken = cancellationToken
            };

            // 创建转换块，用于处理单个音乐文件
            var processBlock = new TransformBlock<string, bool>(async audioFile =>
            {
                try
                {
                    string fileName = Path.GetFileName(audioFile);
                    string targetPath = Path.Combine(_targetFolder, fileName);

                    // 如果目标文件已存在，则跳过
                    if (File.Exists(targetPath))
                    {
                        Console.WriteLine($"音乐文件已存在，跳过: {fileName}");
                        return true;
                    }

                    Console.WriteLine($"处理音乐文件: {fileName}");

                    // 使用文件复制来处理音乐文件
                    using (var sourceStream = new FileStream(audioFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                    using (var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    {
                        // 缓冲区大小设置为 256KB，适合音乐文件传输
                        byte[] buffer = new byte[256 * 1024];
                        int bytesRead;

                        // 读取并写入数据
                        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            await targetStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        }
                    }

                    Console.WriteLine($"音乐文件处理完成: {fileName}");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理音乐文件时出错: {ex.Message}");
                    return false;
                }
            }, options);

            // 创建动作块，用于计数成功处理的文件
            var countBlock = new ActionBlock<bool>(success =>
            {
                // 这里不需要做任何事情，我们只关心成功处理的文件数量
            }, options);

            // 连接块
            processBlock.LinkTo(countBlock, new DataflowLinkOptions { PropagateCompletion = true });

            // 发送所有音乐文件到处理管道
            foreach (var audioFile in audioFiles)
            {
                await processBlock.SendAsync(audioFile);
            }

            // 标记处理块已完成
            processBlock.Complete();

            // 等待所有处理完成
            await countBlock.Completion;

            // 计算成功处理的文件数
            int successCount = audioFiles.Count - processBlock.OutputCount;

            Console.WriteLine($"音乐文件批量处理完成，成功处理 {successCount} 个文件");

            return successCount;
        }

        /// <summary>
        /// 运行 Robocopy 进程
        /// </summary>
        private (int successCount, string? firstAudioFile) RunRobocopy(
            string source,
            string destination,
            CancellationToken cancellationToken,
            string filePattern = "*.*",
            bool excludeVideos = false,
            bool excludeAudios = false,
            bool excludeVideosAndAudios = false)
        {
            // 获取支持的文件格式
            var supportedExtensions = SupportedFormats.GetAllSupportedExtensions();

            // 如果需要排除视频文件
            if (excludeVideos || excludeVideosAndAudios)
            {
                var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };
                supportedExtensions = supportedExtensions.Except(videoExtensions, StringComparer.OrdinalIgnoreCase).ToArray();
            }

            // 如果需要排除音频文件
            if (excludeAudios || excludeVideosAndAudios)
            {
                var audioExtensions = new[] { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".wma", ".m4a" };
                supportedExtensions = supportedExtensions.Except(audioExtensions, StringComparer.OrdinalIgnoreCase).ToArray();
            }

            // 构建文件过滤参数
            string fileFilters = filePattern;

            // 如果是通配符模式，则使用支持的扩展名构建过滤器
            if (filePattern == "*.*")
            {
                fileFilters = string.Join(" ", supportedExtensions.Select(ext => $"*{ext}"));
            }

            // 创建进程启动信息
            var startInfo = new ProcessStartInfo
            {
                FileName = "robocopy",
                // 关键参数：/MT:8 多线程复制，/Z 断点续传，/S 包含子目录但不包含空目录
                // /NP 不显示进度，/NFL 不显示文件列表，/NDL 不显示目录列表
                Arguments = $"\"{source}\" \"{destination}\" {fileFilters} /S /MT:8 /Z /R:1 /W:1 /NP /NFL /NDL /NC /NS /COPY:DAT",
                UseShellExecute = false,
                CreateNoWindow = true, // 不显示控制台窗口
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            string? firstAudioFile = null;
            int successCount = 0;
            int processId = Interlocked.Increment(ref _nextProcessId);

            // 启动进程
            using var process = new Process { StartInfo = startInfo };

            try
            {
                _activeProcesses.TryAdd(processId, process);
                process.Start();

                // 设置取消令牌的回调，以便在取消时终止进程
                using var registration = cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch
                    {
                        // 忽略终止进程时的错误
                    }
                });

                // 读取输出和错误
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // 等待进程完成，但设置超时以防止无限等待
                bool completed = process.WaitForExit(60000); // 60秒超时

                if (!completed)
                {
                    // 如果超时，强制终止进程
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch
                    {
                        // 忽略终止进程时的错误
                    }
                }
                else
                {
                    // 检查退出代码
                    int exitCode = process.ExitCode;

                    // Robocopy 退出代码:
                    // 0 - 没有文件被复制，没有失败
                    // 1 - 文件被成功复制
                    // 2 - 有额外的文件或目录，没有文件被复制
                    // 4 - 有不匹配的文件或目录，没有文件被复制
                    // 8 - 有文件复制失败
                    // 16 - 有严重错误

                    // 如果有严重错误，记录到日志
                    if (exitCode >= 16)
                    {
                        string logPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "LuckyStars",
                            "Logs"
                        );

                        Directory.CreateDirectory(logPath);

                        string logFile = Path.Combine(logPath, $"RobocopyError_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                        File.WriteAllText(logFile, $"命令: robocopy {startInfo.Arguments}\n\n输出:\n{output}\n\n错误:\n{error}");
                    }

                    // 解析输出，获取成功传输的文件数
                    var match = Regex.Match(output, @"Files\s+:\s+(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int count))
                    {
                        successCount = count;
                    }

                    // 查找第一个音频文件
                    if (successCount > 0)
                    {
                        var audioExtensions = new[] { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".wma", ".m4a" };

                        foreach (var ext in audioExtensions)
                        {
                            string[] files = Directory.GetFiles(destination, $"*{ext}", SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                firstAudioFile = files[0];
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // 忽略执行 Robocopy 时的错误
            }
            finally
            {
                // 从活动进程列表中移除
                _activeProcesses.TryRemove(processId, out _);
            }

            return (successCount, firstAudioFile);
        }
    }
}

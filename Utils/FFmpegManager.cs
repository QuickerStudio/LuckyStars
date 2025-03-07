using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Text;
using System.ComponentModel;

namespace LuckyStars.Utils
{
    /// <summary>
    /// FFmpeg 管理器，负责 FFmpeg 的版本管理和执行
    /// </summary>
    public class FFmpegManager : IDisposable
    {
        // 应用程序目录
        private readonly string _appDirectory;
        
        // FFmpeg 目录
        private readonly string _ffmpegDirectory;
        
        // 备份目录
        private readonly string _backupDirectory;
        
        // 临时目录
        private readonly string _tempDirectory;
        
        // 日志目录
        private readonly string _logDirectory;
        
        // FFmpeg 日志记录器
        private FFmpegLogger _logger;
        
        // FFmpeg GitHub 发布页面
        private const string FFMPEG_VERSION_API = "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest";
        
        // 当前使用的版本信息
        private string _currentVersion = "";
        
        // 是否正在检查更新
        private bool _isCheckingUpdate = false;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="appDirectory">应用程序数据目录</param>
        public FFmpegManager(string appDirectory)
        {
            _appDirectory = appDirectory;
            _ffmpegDirectory = Path.Combine(appDirectory, "FFmpeg");
            _backupDirectory = Path.Combine(_ffmpegDirectory, "Backup");
            _tempDirectory = Path.Combine(appDirectory, "Temp");
            _logDirectory = Path.Combine(appDirectory, "Logs", "FFmpeg");
            
            // 创建目录
            Directory.CreateDirectory(_ffmpegDirectory);
            Directory.CreateDirectory(_backupDirectory);
            Directory.CreateDirectory(_tempDirectory);
            Directory.CreateDirectory(_logDirectory);
            
            // 初始化日志记录器
            _logger = new FFmpegLogger(appDirectory);
            
            // 加载当前版本信息
            LoadCurrentVersionInfo();
            
            // 检查FFmpeg可用性
            EnsureFFmpegAvailable();
        }
        
        /// <summary>
        /// 确保FFmpeg可用
        /// </summary>
        private void EnsureFFmpegAvailable()
        {
            string ffmpegPath = GetFFmpegPath();
            
            if (!File.Exists(ffmpegPath))
            {
                Debug.WriteLine("FFmpeg不存在，尝试从内置资源复制");
                
                // 尝试从内置资源复制
                string embeddedFFmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "FFmpeg", "bin");
                
                if (Directory.Exists(embeddedFFmpegPath))
                {
                    // 复制内置FFmpeg
                    CopyDirectory(embeddedFFmpegPath, Path.Combine(_ffmpegDirectory, "bin"));
                    Debug.WriteLine("已从内置资源复制FFmpeg");
                }
                else
                {
                    Debug.WriteLine("内置FFmpeg不存在，将在后台下载");
                }
            }
        }
        
        /// <summary>
        /// 获取FFmpeg路径
        /// </summary>
        /// <returns>FFmpeg可执行文件路径</returns>
        public string GetFFmpegPath()
        {
            string ffmpegPath = Path.Combine(_ffmpegDirectory, "bin", "ffmpeg.exe");
            
            // 如果当前版本不存在，尝试从备份恢复
            if (!File.Exists(ffmpegPath))
            {
                RollbackToLastWorkingVersion();
            }
            
            // 如果仍然不存在，尝试在系统路径中找到ffmpeg
            if (!File.Exists(ffmpegPath))
            {
                string systemPath = FindExecutableInSystemPath("ffmpeg.exe");
                if (!string.IsNullOrEmpty(systemPath))
                {
                    return systemPath;
                }
            }
            
            return ffmpegPath;
        }
        
        /// <summary>
        /// 在系统路径中查找可执行文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件完整路径，未找到则返回null</returns>
        private string FindExecutableInSystemPath(string fileName)
        {
            try
            {
                // 获取系统PATH环境变量
                string path = Environment.GetEnvironmentVariable("PATH");
                
                if (string.IsNullOrEmpty(path))
                    return null;
                    
                // 分割PATH
                string[] directories = path.Split(';');
                
                // 查找可执行文件
                foreach (string directory in directories)
                {
                    if (string.IsNullOrEmpty(directory))
                        continue;
                        
                    string fullPath = Path.Combine(directory, fileName);
                    
                    if (File.Exists(fullPath))
                        return fullPath;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"在系统路径中查找可执行文件时出错: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 启动时调用此方法，异步检查更新
        /// </summary>
        public void CheckForUpdatesAsync()
        {
            if (_isCheckingUpdate)
                return;
                
            _isCheckingUpdate = true;
            
            // 在后台线程执行，不阻塞主程序
            Task.Run(async () => {
                try
                {
                    // 确保有网络连接
                    if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    {
                        _isCheckingUpdate = false;
                        return;
                    }
                        
                    // 检查是否有新版本
                    var latestVersion = await GetLatestVersionAsync();
                    if (string.IsNullOrEmpty(latestVersion) || latestVersion == _currentVersion)
                    {
                        _isCheckingUpdate = false;
                        return;
                    }
                        
                    // 下载新版本
                    bool success = await DownloadAndUpdateFFmpegAsync(latestVersion);
                    
                    if (success)
                    {
                        // 更新版本信息
                        _currentVersion = latestVersion;
                        SaveCurrentVersionInfo();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"检查FFmpeg更新失败: {ex.Message}");
                }
                finally
                {
                    _isCheckingUpdate = false;
                }
            });
        }
        
        /// <summary>
        /// 获取最新版本
        /// </summary>
        /// <returns>版本号</returns>
        private async Task<string> GetLatestVersionAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // 设置User-Agent避免GitHub API限制
                    client.DefaultRequestHeaders.Add("User-Agent", "LuckyStars-Wallpaper/1.0");
                    
                    // 获取最新版本信息
                    string response = await client.GetStringAsync(FFMPEG_VERSION_API);
                    
                    // 解析JSON
                    dynamic releaseInfo = JsonConvert.DeserializeObject(response);
                    
                    // 获取版本号
                    string tagName = releaseInfo.tag_name;
                    
                    return tagName;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取FFmpeg最新版本失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 下载并更新FFmpeg
        /// </summary>
        /// <param name="version">版本号</param>
        /// <returns>是否成功</returns>
        private async Task<bool> DownloadAndUpdateFFmpegAsync(string version)
        {
            try
            {
                // FFmpeg下载URL
                string downloadUrl = GetFFmpegDownloadUrl(version);
                
                // 临时压缩文件路径
                string zipFilePath = Path.Combine(_tempDirectory, $"ffmpeg_{version}.zip");
                
                // 临时解压目录
                string extractPath = Path.Combine(_tempDirectory, $"ffmpeg_{version}");
                
                // 备份路径
                string backupPath = Path.Combine(_backupDirectory, _currentVersion);
                
                // 确保临时目录存在
                Directory.CreateDirectory(extractPath);
                
                // 下载FFmpeg
                bool downloadSuccess = await DownloadFileAsync(downloadUrl, zipFilePath);
                if (!downloadSuccess)
                    return false;
                    
                // 解压FFmpeg
                try
                {
                    ZipFile.ExtractToDirectory(zipFilePath, extractPath, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"解压FFmpeg失败: {ex.Message}");
                    return false;
                }
                
                // 验证解压后的文件
                string extractedFFmpegPath = FindFFmpegInExtractedFolder(extractPath);
                if (string.IsNullOrEmpty(extractedFFmpegPath) || !Directory.Exists(extractedFFmpegPath))
                {
                    Debug.WriteLine("解压后无法找到FFmpeg目录");
                    return false;
                }
                
                // 如果当前版本存在，先备份
                string currentFFmpegPath = Path.Combine(_ffmpegDirectory, "bin");
                if (!string.IsNullOrEmpty(_currentVersion) && Directory.Exists(currentFFmpegPath))
                {
                    // 创建备份目录
                    Directory.CreateDirectory(backupPath);
                    
                    // 复制当前版本到备份目录
                    try
                    {
                        string backupBinPath = Path.Combine(backupPath, "bin");
                        CopyDirectory(currentFFmpegPath, backupBinPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"备份当前FFmpeg版本失败: {ex.Message}");
                    }
                }
                
                // 清理当前版本
                try
                {
                    if (Directory.Exists(currentFFmpegPath))
                    {
                        Directory.Delete(currentFFmpegPath, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"清理当前FFmpeg版本失败: {ex.Message}");
                }
                
                // 移动新版本到FFmpeg目录
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(currentFFmpegPath));
                    Directory.Move(extractedFFmpegPath, currentFFmpegPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"更新FFmpeg版本失败: {ex.Message}");
                    
                    // 如果更新失败，恢复备份
                    RollbackToLastWorkingVersion();
                    return false;
                }
                
                // 清理临时文件
                try
                {
                    if (File.Exists(zipFilePath))
                        File.Delete(zipFilePath);
                        
                    if (Directory.Exists(extractPath))
                        Directory.Delete(extractPath, true);
                }
                catch
                {
                    // 忽略清理错误
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"下载并更新FFmpeg失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取FFmpeg下载URL
        /// </summary>
        /// <param name="version">版本号</param>
        /// <returns>下载URL</returns>
        private string GetFFmpegDownloadUrl(string version)
        {
            // 这里使用BtbN的FFmpeg构建
            // 需要根据实际情况调整下载URL构造方式
            return $"https://github.com/BtbN/FFmpeg-Builds/releases/download/{version}/ffmpeg-n4.4-latest-win64-gpl-4.4.zip";
        }
        
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url">下载URL</param>
        /// <param name="destinationPath">目标路径</param>
        /// <returns>是否成功</returns>
        private async Task<bool> DownloadFileAsync(string url, string destinationPath)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // 设置超时
                    client.Timeout = TimeSpan.FromMinutes(5);
                    
                    // 设置User-Agent
                    client.DefaultRequestHeaders.Add("User-Agent", "LuckyStars-Wallpaper/1.0");
                    
                    // 下载文件
                    byte[] fileData = await client.GetByteArrayAsync(url);
                    
                    // 写入文件
                    await File.WriteAllBytesAsync(destinationPath, fileData);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"下载文件失败: {ex.Message}");
                return false;
            }
        }
        
                /// <summary>
        /// 在解压文件夹中查找FFmpeg可执行文件目录
        /// </summary>
        /// <param name="extractPath">解压路径</param>
        /// <returns>FFmpeg目录，未找到则返回null</returns>
        private string FindFFmpegInExtractedFolder(string extractPath)
        {
            try
            {
                // 常见的解压后目录结构
                string[] possiblePaths = {
                    Path.Combine(extractPath, "bin"),
                    Path.Combine(extractPath, "ffmpeg-*", "bin"),
                    Path.Combine(extractPath, "*", "bin")
                };
                
                // 首先检查直接路径
                if (Directory.Exists(Path.Combine(extractPath, "bin")) && 
                    File.Exists(Path.Combine(extractPath, "bin", "ffmpeg.exe")))
                {
                    return Path.Combine(extractPath, "bin");
                }
                
                // 搜索子目录
                foreach (string directory in Directory.GetDirectories(extractPath, "*", SearchOption.AllDirectories))
                {
                    string binDir = Path.Combine(directory, "bin");
                    string ffmpegPath = Path.Combine(binDir, "ffmpeg.exe");
                    
                    if (Directory.Exists(binDir) && File.Exists(ffmpegPath))
                    {
                        return binDir;
                    }
                }
                
                // 直接搜索ffmpeg.exe
                string[] ffmpegFiles = Directory.GetFiles(extractPath, "ffmpeg.exe", SearchOption.AllDirectories);
                if (ffmpegFiles.Length > 0)
                {
                    return Path.GetDirectoryName(ffmpegFiles[0]);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"查找解压后的FFmpeg失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 检查是否需要回退
        /// </summary>
        public void CheckForRollback()
        {
            string flagFile = Path.Combine(_appDirectory, "crash_detected.flag");
            
            if (File.Exists(flagFile))
            {
                string reason = "未知";
                try
                {
                    reason = File.ReadAllText(flagFile);
                }
                catch { }
                
                Debug.WriteLine($"检测到FFmpeg异常标记，原因: {reason}");
                
                // 执行回退
                RollbackToLastWorkingVersion();
                
                // 删除标记文件
                try { File.Delete(flagFile); } catch { }
            }
        }
        
        /// <summary>
        /// 回退到上一个工作版本
        /// </summary>
        private bool RollbackToLastWorkingVersion()
        {
            try
            {
                // 获取最新备份
                var backups = Directory.GetDirectories(_backupDirectory)
                                .Select(d => new DirectoryInfo(d))
                                .OrderByDescending(d => d.CreationTime)
                                .ToList();
                
                if (backups.Count > 0)
                {
                    // 记录回退日志
                    string logFile = Path.Combine(_logDirectory, $"rollback_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    File.WriteAllText(logFile, $"回退到版本: {backups[0].Name}\n时间: {DateTime.Now}");
                    
                    // 当前bin目录
                    string currentBinDir = Path.Combine(_ffmpegDirectory, "bin");
                    
                    // 备份目录
                    string backupBinDir = Path.Combine(backups[0].FullName, "bin");
                    
                    // 如果当前bin目录存在，将其重命名为临时目录
                    if (Directory.Exists(currentBinDir))
                    {
                        string tempDir = Path.Combine(_ffmpegDirectory, $"bin_broken_{DateTime.Now:yyyyMMdd_HHmmss}");
                        Directory.Move(currentBinDir, tempDir);
                    }
                    
                    // 复制备份到当前目录
                    CopyDirectory(backupBinDir, currentBinDir);
                    
                    Debug.WriteLine($"成功回退到备份版本: {backups[0].Name}");
                    return true;
                }
                else
                {
                    Debug.WriteLine("没有可用的备份版本");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"回退到上一版本失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 复制目录及其内容
        /// </summary>
        /// <param name="sourceDir">源目录</param>
        /// <param name="destDir">目标目录</param>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            // 创建目标目录
            Directory.CreateDirectory(destDir);
            
            // 复制文件
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            
            // 复制子目录
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
        
        /// <summary>
        /// 加载当前版本信息
        /// </summary>
        private void LoadCurrentVersionInfo()
        {
            try
            {
                string versionFile = Path.Combine(_ffmpegDirectory, "version.json");
                
                if (File.Exists(versionFile))
                {
                    string json = File.ReadAllText(versionFile);
                    dynamic versionInfo = JsonConvert.DeserializeObject(json);
                    
                    _currentVersion = versionInfo.Version;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载版本信息失败: {ex.Message}");
                _currentVersion = "";
            }
        }
        
        /// <summary>
        /// 保存当前版本信息
        /// </summary>
        private void SaveCurrentVersionInfo()
        {
            try
            {
                string versionFile = Path.Combine(_ffmpegDirectory, "version.json");
                
                var versionInfo = new
                {
                    Version = _currentVersion,
                    UpdateTime = DateTime.Now
                };
                
                string json = JsonConvert.SerializeObject(versionInfo, Formatting.Indented);
                File.WriteAllText(versionFile, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存版本信息失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行FFmpeg命令
        /// </summary>
        /// <param name="arguments">命令行参数</param>
        /// <returns>执行结果</returns>
        public async Task<FFmpegResult> ExecuteCommandAsync(string arguments)
        {
            return await ExecuteCommandAsync("FFmpeg操作", arguments);
        }
        
        /// <summary>
        /// 执行FFmpeg命令
        /// </summary>
        /// <param name="operation">操作描述</param>
        /// <param name="arguments">命令行参数</param>
        /// <returns>执行结果</returns>
        public async Task<FFmpegResult> ExecuteCommandAsync(string operation, string arguments)
        {
            string ffmpegPath = GetFFmpegPath();
            
            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                return new FFmpegResult
                {
                    Success = false,
                    ExitCode = -1,
                    StandardOutput = "",
                    ErrorOutput = "FFmpeg可执行文件不存在"
                };
            }
            
            StringBuilder outputBuilder = new StringBuilder();
            StringBuilder errorBuilder = new StringBuilder();
            int exitCode = -1;
            
            try
            {
                using (Process process = new Process())
                {
                    // 配置进程启动信息
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    
                    // 输出处理器
                    process.OutputDataReceived += (sender, e) => 
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            outputBuilder.AppendLine(e.Data);
                    };
                    
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            errorBuilder.AppendLine(e.Data);
                    };
                    
                    // 启动进程
                    process.Start();
                    
                    // 开始异步读取输出
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    
                    // 等待进程结束
                    await process.WaitForExitAsync();
                    exitCode = process.ExitCode;
                }
                
                // 构造结果
                var result = new FFmpegResult
                {
                    Success = exitCode == 0,
                    ExitCode = exitCode,
                    StandardOutput = outputBuilder.ToString(),
                    ErrorOutput = errorBuilder.ToString()
                };
                
                // 记录结果
                _logger.LogFFmpegResult(operation, result.Success, exitCode, 
                    result.StandardOutput, result.ErrorOutput);
                
                return result;
            }
            catch (Exception ex)
            {
                var result = new FFmpegResult
                {
                    Success = false,
                    ExitCode = -1,
                    StandardOutput = "",
                    ErrorOutput = $"执行异常: {ex.Message}"
                };
                
                // 记录结果
                _logger.LogFFmpegResult(operation, false, -1, "", result.ErrorOutput);
                
                return result;
            }
        }
        
        /// <summary>
        /// 测试FFmpeg是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        public async Task<bool> TestFFmpegAsync()
        {
            try
            {
                // 执行简单的FFmpeg命令（-version）
                var result = await ExecuteCommandAsync("测试", "-version");
                return result.Success && !string.IsNullOrEmpty(result.StandardOutput);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"测试FFmpeg失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// FFmpeg执行结果
        /// </summary>
        public class FFmpegResult
        {
            /// <summary>
            /// 是否成功（ExitCode == 0）
            /// </summary>
            public bool Success { get; set; }
            
            /// <summary>
            /// 退出代码
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
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _logger = null;
        }
    }
    
    /// <summary>
    /// FFmpeg日志记录器
    /// </summary>
    public class FFmpegLogger
    {
        private readonly string _logDirectory;
        private readonly string _statisticsFile;
        private int _successCount = 0;
        private int _failureCount = 0;
        private readonly object _lockObj = new object();
        
        // 连续失败阈值
        private const int CONSECUTIVE_FAILURE_THRESHOLD = 3;
        private int _consecutiveFailures = 0;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="appDataPath">应用数据目录</param>
        public FFmpegLogger(string appDataPath)
        {
            _logDirectory = Path.Combine(appDataPath, "Logs", "FFmpeg");
            _statisticsFile = Path.Combine(_logDirectory, "statistics.json");
            
            Directory.CreateDirectory(_logDirectory);
            LoadStatistics();
        }
        
        /// <summary>
        /// 记录FFmpeg执行结果
        /// </summary>
        /// <param name="operation">操作描述</param>
        /// <param name="success">是否成功</param>
        /// <param name="exitCode">退出代码</param>
        /// <param name="standardOutput">标准输出</param>
        /// <param name="errorOutput">错误输出</param>
        public void LogFFmpegResult(string operation, bool success, int exitCode, string standardOutput, string errorOutput)
        {
            // 生成唯一的日志文件名
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logFileName = $"ffmpeg_{operation}_{timestamp}.log";
            string logFilePath = Path.Combine(_logDirectory, logFileName);
            
            try
            {
                // 写入详细日志
                using (StreamWriter writer = new StreamWriter(logFilePath))
                {
                    writer.WriteLine($"[FFmpeg {operation}] {DateTime.Now}");
                    writer.WriteLine($"成功: {success}");
                    writer.WriteLine($"退出代码: {exitCode}");
                    writer.WriteLine("--- 标准输出 ---");
                    writer.WriteLine(standardOutput);
                    writer.WriteLine("--- 错误输出 ---");
                    writer.WriteLine(errorOutput);
                    
                    // 提取错误关键词
                    string[] errorKeywords = {"Invalid", "Error", "Failed", "Cannot", "Unknown"};
                    foreach (var keyword in errorKeywords)
                    {
                        if (errorOutput.Contains(keyword))
                        {
                            writer.WriteLine($"检测到关键错误词: {keyword}");
                        }
                    }
                }
                
                // 更新统计数据
                UpdateStatistics(success);
                
                // 检查是否需要标记回退
                if (ShouldTriggerRollback(success, errorOutput))
                {
                    CreateRollbackFlag("连续失败或严重错误");
                }
            }
            catch (Exception ex)
            {
                // 即使日志记录失败，也不应影响主程序
                Debug.WriteLine($"记录FFmpeg日志失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 判断是否应该触发回退
        /// </summary>
        private bool ShouldTriggerRollback(bool success, string errorOutput)
        {
            if (success)
            {
                _consecutiveFailures = 0;
                return false;
            }
            
            _consecutiveFailures++;
            
            // 条件1: 连续失败次数超过阈值
            if (_consecutiveFailures >= CONSECUTIVE_FAILURE_THRESHOLD)
                return true;
                
            // 条件2: 检测到严重错误关键词
            string[] criticalErrors = {
                "不兼容的版本",
                "不支持的格式",
                "缺少关键组件",
                "版本冲突",
                "命令行选项无效"
            };
            
            foreach (var error in criticalErrors)
            {
                if (errorOutput.Contains(error))
                    return true;
            }
            
            // 条件3: 整体成功率过低(低于60%)
            if (_successCount + _failureCount > 10)
            {
                double successRate = (double)_successCount / (_successCount + _failureCount);
                if (successRate < 0.6)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 创建回退标记
        /// </summary>
        private void CreateRollbackFlag(string reason)
        {
            try
            {
                string flagFile = Path.Combine(Path.GetDirectoryName(_logDirectory), "..", "crash_detected.flag");
                File.WriteAllText(flagFile, $"触发回退原因: {reason}\n时间: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建回退标记失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新统计数据
        /// </summary>
        private void UpdateStatistics(bool success)
        {
            lock (_lockObj)
            {
                if (success)
                    _successCount++;
                else
                    _failureCount++;
                    
                try
                {
                    var statistics = new
                    {
                        SuccessCount = _successCount,
                        FailureCount = _failureCount,
                        LastUpdate = DateTime.Now
                    };
                    
                    string json = JsonConvert.SerializeObject(statistics, Formatting.Indented);
                    File.WriteAllText(_statisticsFile, json);
                }
                catch
                {
                    // 统计信息不重要，忽略错误
                }
            }
        }
        
        /// <summary>
        /// 加载统计数据
        /// </summary>
        private void LoadStatistics()
        {
            try
            {
                if (File.Exists(_statisticsFile))
                {
                    string json = File.ReadAllText(_statisticsFile);
                    dynamic statistics = JsonConvert.DeserializeObject(json);
                    
                    _successCount = statistics.SuccessCount;
                    _failureCount = statistics.FailureCount;
                }
            }
            catch
            {
                // 如果加载失败，使用默认值
                _successCount = 0;
                _failureCount = 0;
            }
        }
    }
}
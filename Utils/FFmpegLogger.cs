using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace LuckyStars.Utils
{
    /// <summary>
    /// FFmpeg日志记录器，用于记录FFmpeg操作的日志信息
    /// </summary>
    public class FFmpegLogger
    {
        // 日志文件目录
        private readonly string _logDirectory;
        
        // 当前日志文件路径
        private string _currentLogFile;
        
        // 线程同步锁
        private readonly object _logLock = new object();
        
        // 日志保留天数
        private const int LogRetentionDays = 7;

        /// <summary>
        /// 初始化FFmpeg日志记录器
        /// </summary>
        /// <param name="logDirectory">日志目录路径</param>
        public FFmpegLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            
            // 确保日志目录存在
            Directory.CreateDirectory(_logDirectory);
            
            // 创建新的日志文件
            CreateNewLogFile();
        }

        /// <summary>
        /// 创建新的日志文件
        /// </summary>
        private void CreateNewLogFile()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _currentLogFile = Path.Combine(_logDirectory, $"ffmpeg_{timestamp}.log");
            
            // 写入日志文件头
            try
            {
                using (StreamWriter writer = File.CreateText(_currentLogFile))
                {
                    writer.WriteLine("==================================================");
                    writer.WriteLine($"FFmpeg Log - Started at {DateTime.Now}");
                    writer.WriteLine($"System: {Environment.OSVersion}, .NET: {Environment.Version}");
                    writer.WriteLine("==================================================");
                    writer.WriteLine();
                }
            }
            catch (Exception ex)
            {
                // 如果无法创建日志文件，输出到控制台
                Console.WriteLine($"无法创建FFmpeg日志文件: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
                
            try
            {
                lock (_logLock)
                {
                    // 检查日志文件是否存在，如果不存在则重新创建
                    if (!File.Exists(_currentLogFile))
                    {
                        CreateNewLogFile();
                    }
                    
                    // 追加日志
                    using (StreamWriter writer = File.AppendText(_currentLogFile))
                    {
                        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
                    }
                    
                    // 同时输出到控制台
                    Console.WriteLine($"FFmpeg: {message}");
                }
            }
            catch (Exception ex)
            {
                // 如果无法写入日志，输出到控制台
                Console.WriteLine($"无法写入FFmpeg日志: {ex.Message}");
                Console.WriteLine($"原始消息: {message}");
            }
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="context">异常上下文</param>
        public void LogException(Exception ex, string context = null)
        {
            if (ex == null)
                return;
                
            string message = string.IsNullOrEmpty(context) 
                ? $"异常: {ex.Message}" 
                : $"异常({context}): {ex.Message}";
                
            Log(message);
            Log($"堆栈跟踪: {ex.StackTrace}");
            
            // 记录内部异常
            if (ex.InnerException != null)
            {
                Log($"内部异常: {ex.InnerException.Message}");
                Log($"内部堆栈跟踪: {ex.InnerException.StackTrace}");
            }
        }

        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        public void CleanupOldLogs()
        {
            try
            {
                // 查找所有日志文件
                string[] logFiles = Directory.GetFiles(_logDirectory, "ffmpeg_*.log");
                
                // 获取截止日期
                DateTime cutoffDate = DateTime.Now.AddDays(-LogRetentionDays);
                
                foreach (string logFile in logFiles)
                {
                    try
                    {
                        // 获取文件创建时间
                        DateTime fileDate = File.GetCreationTime(logFile);
                        
                        // 如果文件早于截止日期，则删除
                        if (fileDate < cutoffDate)
                        {
                            File.Delete(logFile);
                            Log($"已删除旧日志文件: {Path.GetFileName(logFile)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 忽略单个文件的删除错误
                        Console.WriteLine($"无法删除日志文件 {logFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"清理旧日志文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前日志文件内容
        /// </summary>
        /// <returns>日志文件内容</returns>
        public string GetCurrentLogContent()
        {
            try
            {
                lock (_logLock)
                {
                    if (File.Exists(_currentLogFile))
                    {
                        return File.ReadAllText(_currentLogFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法读取日志文件: {ex.Message}");
            }
            
            return string.Empty;
        }

        /// <summary>
        /// 获取最近的日志文件列表
        /// </summary>
        /// <param name="count">返回的文件数量</param>
        /// <returns>日志文件路径列表</returns>
        public List<string> GetRecentLogFiles(int count = 5)
        {
            try
            {
                // 查找所有日志文件
                string[] logFiles = Directory.GetFiles(_logDirectory, "ffmpeg_*.log");
                
                // 按创建时间排序并返回指定数量
                return logFiles
                    .OrderByDescending(File.GetCreationTime)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法获取最近的日志文件: {ex.Message}");
                return new List<string>();
            }
        }
    }
}
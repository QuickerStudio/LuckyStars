using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 文件操作工具类，提供常用的文件操作方法
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// 安全地创建目录，如果目录已存在则不做任何操作
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>是否成功创建目录</returns>
        public static bool CreateDirectorySafe(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建目录失败: {path}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 安全地删除文件，如果文件不存在则不做任何操作
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否成功删除文件</returns>
        public static bool DeleteFileSafe(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除文件失败: {path}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 安全地复制文件，如果目标文件已存在则覆盖
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在的文件</param>
        /// <returns>是否成功复制文件</returns>
        public static bool CopyFileSafe(string sourcePath, string destPath, bool overwrite = true)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    // 确保目标目录存在
                    string destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        CreateDirectorySafe(destDir);
                    }

                    File.Copy(sourcePath, destPath, overwrite);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"复制文件失败: {sourcePath} -> {destPath}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 异步复制文件
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在的文件</param>
        /// <returns>是否成功复制文件</returns>
        public static async Task<bool> CopyFileAsync(string sourcePath, string destPath, bool overwrite = true)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    // 确保目标目录存在
                    string destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        CreateDirectorySafe(destDir);
                    }

                    using (FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                    using (FileStream destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    {
                        await sourceStream.CopyToAsync(destStream);
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"异步复制文件失败: {sourcePath} -> {destPath}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 计算文件MD5哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>MD5哈希字符串</returns>
        public static string CalculateFileMD5(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return string.Empty;
                }

                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                    {
                        sb.Append(hash[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"计算文件MD5失败: {filePath}, 错误: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 异步计算文件MD5哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>MD5哈希字符串</returns>
        public static async Task<string> CalculateFileMD5Async(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return string.Empty;
                }

                using (var md5 = MD5.Create())
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                {
                    byte[] hash = await md5.ComputeHashAsync(stream);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                    {
                        sb.Append(hash[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"异步计算文件MD5失败: {filePath}, 错误: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取文件大小的可读字符串表示
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>可读的文件大小字符串</returns>
        public static string GetReadableFileSize(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return "0 B";
                }

                long fileSize = new FileInfo(filePath).Length;
                return GetReadableSize(fileSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取文件大小失败: {filePath}, 错误: {ex.Message}");
                return "未知大小";
            }
        }

        /// <summary>
        /// 将字节大小转换为可读字符串
        /// </summary>
        /// <param name="size">字节大小</param>
        /// <returns>可读的文件大小字符串</returns>
        public static string GetReadableSize(long size)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
            int unit = 0;
            double fileSize = size;

            while (fileSize >= 1024 && unit < units.Length - 1)
            {
                fileSize /= 1024;
                unit++;
            }

            return $"{fileSize:0.##} {units[unit]}";
        }

        /// <summary>
        /// 获取文件扩展名（不含点号）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>扩展名</returns>
        public static string GetExtensionWithoutDot(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            string ext = Path.GetExtension(filePath);
            return ext.StartsWith(".") ? ext.Substring(1) : ext;
        }

        /// <summary>
        /// 将文件扩展名转换为小写
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>带小写扩展名的文件路径</returns>
        public static string GetPathWithLowerExtension(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            string ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
                return filePath;

            return filePath.Substring(0, filePath.Length - ext.Length) + ext.ToLowerInvariant();
        }

        /// <summary>
        /// 获取不重复的文件路径，如果文件已存在则添加数字后缀
        /// </summary>
        /// <param name="filePath">原始文件路径</param>
        /// <returns>不重复的文件路径</returns>
        public static string GetUniqueFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return filePath;

            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 1;
            string newPath = filePath;

            while (File.Exists(newPath))
            {
                newPath = Path.Combine(directory, $"{fileName}_{counter++}{extension}");
            }

            return newPath;
        }

        /// <summary>
        /// 确保文件路径可用于文件名（删除或替换非法字符）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>处理后的文件名</returns>
        public static string MakeSafeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            // 非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string result = fileName;

            foreach (char c in invalidChars)
            {
                result = result.Replace(c, '_');
            }

            // 替换其它可能导致问题的字符
            result = result.Replace(':', '_');
            result = result.Replace('*', '_');
            result = result.Replace('?', '_');
            result = result.Replace('<', '_');
            result = result.Replace('>', '_');
            result = result.Replace('|', '_');
            result = result.Replace('"', '_');

            return result;
        }

        /// <summary>
        /// 获取文件的MIME类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>MIME类型字符串</returns>
        public static string GetMimeType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "application/octet-stream";

            string ext = GetExtensionWithoutDot(filePath).ToLowerInvariant();
            
            // 常见文件类型映射
            Dictionary<string, string> mimeTypes = new Dictionary<string, string>
            {
                { "jpg", "image/jpeg" },
                { "jpeg", "image/jpeg" },
                { "png", "image/png" },
                { "gif", "image/gif" },
                { "bmp", "image/bmp" },
                { "webp", "image/webp" },
                { "ico", "image/x-icon" },
                { "svg", "image/svg+xml" },
                
                { "mp4", "video/mp4" },
                { "avi", "video/x-msvideo" },
                { "wmv", "video/x-ms-wmv" },
                { "mkv", "video/x-matroska" },
                { "mov", "video/quicktime" },
                { "webm", "video/webm" },
                
                { "mp3", "audio/mpeg" },
                { "wav", "audio/wav" },
                { "ogg", "audio/ogg" },
                { "flac", "audio/flac" },
                
                { "html", "text/html" },
                { "htm", "text/html" },
                { "css", "text/css" },
                { "js", "text/javascript" },
                { "txt", "text/plain" },
                { "json", "application/json" },
                { "xml", "application/xml" },
                
                { "pdf", "application/pdf" },
                { "zip", "application/zip" },
                { "7z", "application/x-7z-compressed" },
                { "rar", "application/x-rar-compressed" },
                
                { "exe", "application/x-msdownload" },
                { "dll", "application/x-msdownload" }
            };

            if (mimeTypes.TryGetValue(ext, out string mimeType))
            {
                return mimeType;
            }

            // 尝试从注册表获取
            try
            {
                string registryKey = $".{ext}";
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        object contentType = key.GetValue("Content Type");
                        if (contentType != null)
                        {
                            return contentType.ToString();
                        }
                    }
                }
            }
            catch
            {
                // 如果注册表查询失败，忽略错误，使用默认值
            }

            return "application/octet-stream";
        }

        /// <summary>
        /// 获取临时文件路径
        /// </summary>
        /// <param name="extension">文件扩展名（包含点号）</param>
        /// <returns>临时文件路径</returns>
        public static string GetTempFilePath(string extension = null)
        {
            extension = string.IsNullOrEmpty(extension) ? ".tmp" : extension;
            if (!extension.StartsWith("."))
                extension = "." + extension;

            return Path.Combine(
                Path.GetTempPath(), 
                "LuckyStars", 
                $"temp_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}"
            );
        }

        /// <summary>
        /// 递归复制目录
        /// </summary>
        /// <param name="sourceDir">源目录</param>
        /// <param name="destDir">目标目录</param>
        /// <param name="overwrite">是否覆盖已存在的文件</param>
        public static void CopyDirectory(string sourceDir, string destDir, bool overwrite = true)
        {
            try
            {
                // 如果源目录不存在，则直接返回
                if (!Directory.Exists(sourceDir))
                {
                    return;
                }

                // 如果目标目录不存在，则创建
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // 复制所有文件
                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destDir, fileName);
                    File.Copy(file, destFile, overwrite);
                }

                // 递归复制子目录
                foreach (string dir in Directory.GetDirectories(sourceDir))
                {
                    string dirName = Path.GetFileName(dir);
                    string destSubDir = Path.Combine(destDir, dirName);
                    CopyDirectory(dir, destSubDir, overwrite);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"复制目录失败: {sourceDir} -> {destDir}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查文件是否被锁定或被其他进程占用
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件是否被锁定</returns>
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    stream.Close();
                }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (Exception)
            {
                // 任何其他异常可能意味着文件不存在或没有权限
                return true;
            }
        }

        /// <summary>
        /// 创建文件的副本
        /// </summary>
        /// <param name="filePath">原始文件路径</param>
        /// <param name="backupExtension">备份文件扩展名</param>
        /// <returns>备份文件路径</returns>
        public static string CreateBackup(string filePath, string backupExtension = ".bak")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                string backupPath = filePath + backupExtension;
                File.Copy(filePath, backupPath, true);
                return backupPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建文件备份失败: {filePath}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 重命名文件或目录
        /// </summary>
        /// <param name="oldPath">原始路径</param>
        /// <param name="newName">新名称</param>
        /// <returns>重命名后的路径</returns>
        public static string Rename(string oldPath, string newName)
        {
            try
            {
                if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newName))
                    return oldPath;

                string directory = Path.GetDirectoryName(oldPath);
                string newPath = Path.Combine(directory, newName);

                if (File.Exists(oldPath))
                {
                    // 确保新路径具有同样的扩展名
                    if (!Path.HasExtension(newPath) && Path.HasExtension(oldPath))
                    {
                        newPath += Path.GetExtension(oldPath);
                    }

                    if (File.Exists(newPath))
                    {
                        newPath = GetUniqueFilePath(newPath);
                    }

                    File.Move(oldPath, newPath);
                }
                else if (Directory.Exists(oldPath))
                {
                    if (Directory.Exists(newPath))
                    {
                        // 生成唯一目录名
                        int counter = 1;
                        string originalNewPath = newPath;
                        while (Directory.Exists(newPath))
                        {
                            newPath = originalNewPath + $"_{counter++}";
                        }
                    }

                    Directory.Move(oldPath, newPath);
                }
                else
                {
                    return oldPath; // 原路径不存在
                }

                return newPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"重命名失败: {oldPath} -> {newName}, 错误: {ex.Message}");
                return oldPath;
            }
        }

        /// <summary>
        /// 清空目录但不删除目录本身
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>是否成功清空目录</returns>
        public static bool ClearDirectory(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    return false;
                }

                // 删除所有文件
                foreach (string file in Directory.GetFiles(directoryPath))
                {
                    File.Delete(file);
                }

                // 删除所有子目录
                foreach (string dir in Directory.GetDirectories(directoryPath))
                {
                    Directory.Delete(dir, true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清空目录失败: {directoryPath}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从URL或路径中获取文件名
        /// </summary>
        /// <param name="urlOrPath">URL或文件路径</param>
        /// <returns>文件名</returns>
        public static string GetFileNameFromUrlOrPath(string urlOrPath)
        {
            if (string.IsNullOrEmpty(urlOrPath))
                return string.Empty;

            try
            {
                // 尝试创建Uri来处理URL
                bool isUrl = Uri.TryCreate(urlOrPath, UriKind.Absolute, out Uri uri) && 
                            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

                string fileName;
                if (isUrl)
                {
                    // 从URL中获取文件名
                    fileName = Path.GetFileName(uri.LocalPath);

                    // 如果没有文件名或URL不包含文件名
                    if (string.IsNullOrEmpty(fileName))
                    {
                        // 使用主机名作为基础，添加后缀
                        fileName = uri.Host.Replace("www.", "").Replace(".", "_") + "_resource";
                    }
                }
                else
                {
                    // 普通文件路径
                    fileName = Path.GetFileName(urlOrPath);
                }

                // 规范化文件名
                fileName = MakeSafeFileName(fileName);

                return fileName;
            }
            catch
            {
                // 如果解析失败，返回唯一标识符作为文件名
                return $"file_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }
        }

        /// <summary>
        /// 获取图像文件尺寸
        /// </summary>
        /// <param name="filePath">图像文件路径</param>
        /// <returns>图像尺寸，如果读取失败则返回Empty</returns>
        public static Size GetImageSize(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return Size.Empty;

                using (Image img = Image.FromFile(filePath))
                {
                    return new Size(img.Width, img.Height);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取图像尺寸失败: {filePath}, 错误: {ex.Message}");
                return Size.Empty;
            }
        }

        /// <summary>
        /// 检查是否为文本文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否为文本文件</returns>
        public static bool IsTextFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                string ext = GetExtensionWithoutDot(filePath).ToLowerInvariant();

                // 常见文本文件扩展名
                string[] textExtensions = { "txt", "log", "ini", "cfg", "config", "xml", "json", "html", "htm", "css", "js", "md", "csv" };
                if (textExtensions.Contains(ext))
                    return true;

                // 读取文件开头检测是否为文本
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[Math.Min(4096, fs.Length)];
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);

                    // 统计控制字符和二进制字符的数量
                    int controlChars = 0;
                    for (int i = 0; i < bytesRead; i++)
                    {
                        // ASCII控制字符范围，排除常见的换行符等
                        if ((buffer[i] < 32 || buffer[i] > 127) && 
                            buffer[i] != 9 && // Tab
                            buffer[i] != 10 && // LF
                            buffer[i] != 13) // CR
                        {
                            controlChars++;
                        }
                    }

                    // 如果控制字符占比超过10%，认为是二进制文件
                    return (controlChars / (float)bytesRead) < 0.1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查文本文件失败: {filePath}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 尝试读取文本文件内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="maxLength">最大读取长度，默认为10MB</param>
        /// <returns>文件内容，读取失败则返回空字符串</returns>
        public static string ReadTextFileSafe(string filePath, int maxLength = 10 * 1024 * 1024)
        {
            try
            {
                if (!File.Exists(filePath) || !IsTextFile(filePath))
                    return string.Empty;

                // 检查文件大小，避免读取过大的文件
                FileInfo fi = new FileInfo(filePath);
                if (fi.Length > maxLength)
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        char[] buffer = new char[maxLength];
                        reader.Read(buffer, 0, maxLength);
                        return new string(buffer) + "\n[文件过大，已截断显示]";
                    }
                }
                else
                {
                    return File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"读取文本文件失败: {filePath}, 错误: {ex.Message}");
                return $"[读取文件失败: {ex.Message}]";
            }
        }
    }
}
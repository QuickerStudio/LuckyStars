using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 文件类型检测工具
    /// </summary>
    public static class FileTypeDetector
    {
        /// <summary>
        /// 文件类型枚举
        /// </summary>
        public enum FileType
        {
            Unknown,
            Image,
            Video,
            HTML,
            Document
        }
        
        // 图片文件扩展名
        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg", ".heic", ".heif"
        };
        
        // 视频文件扩展名
        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpeg", ".mpg"
        };
        
        // HTML文件扩展名
        private static readonly HashSet<string> HtmlExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".html", ".htm"
        };
        
        // 文档文件扩展名
        private static readonly HashSet<string> DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt", ".rtf"
        };
        
        /// <summary>
        /// 异步检测文件类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件类型</returns>
        public static async Task<FileType> DetectFileTypeAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return FileType.Unknown;
                
            return await Task.Run(() => DetectFileType(filePath));
        }
        
        /// <summary>
        /// 检测文件类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件类型</returns>
        public static FileType DetectFileType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return FileType.Unknown;
                
            try
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                // 根据扩展名判断文件类型
                if (ImageExtensions.Contains(extension))
                    return FileType.Image;
                    
                if (VideoExtensions.Contains(extension))
                    return FileType.Video;
                    
                if (HtmlExtensions.Contains(extension))
                    return FileType.HTML;
                    
                if (DocumentExtensions.Contains(extension))
                    return FileType.Document;
                
                // 尝试通过文件头识别
                return DetectFileTypeByHeader(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检测文件类型时出错: {ex.Message}");
                return FileType.Unknown;
            }
        }
        
        /// <summary>
        /// 通过文件头识别文件类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件类型</returns>
        private static FileType DetectFileTypeByHeader(string filePath)
        {
            try
            {
                // 读取文件头部数据
                byte[] buffer = new byte[16];
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length < 16)
                        buffer = new byte[fs.Length];
                        
                    fs.Read(buffer, 0, buffer.Length);
                }
                
                // JPEG文件头: FF D8 FF
                if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                    return FileType.Image;
                    
                // PNG文件头: 89 50 4E 47 0D 0A 1A 0A
                                // PNG文件头: 89 50 4E 47 0D 0A 1A 0A
                if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47
                    && buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
                    return FileType.Image;
                
                // GIF文件头: 47 49 46 38
                if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38)
                    return FileType.Image;
                    
                // BMP文件头: 42 4D
                if (buffer[0] == 0x42 && buffer[1] == 0x4D)
                    return FileType.Image;
                
                // WebP文件头: 52 49 46 46 xx xx xx xx 57 45 42 50
                if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46
                    && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                    return FileType.Image;
                
                // TIFF文件头: 49 49 2A 00 或 4D 4D 00 2A
                if ((buffer[0] == 0x49 && buffer[1] == 0x49 && buffer[2] == 0x2A && buffer[3] == 0x00) ||
                    (buffer[0] == 0x4D && buffer[1] == 0x4D && buffer[2] == 0x00 && buffer[3] == 0x2A))
                    return FileType.Image;
                
                // PDF文件头: 25 50 44 46
                if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
                    return FileType.Document;
                
                // MP4文件头: 66 74 79 70
                if (buffer[4] == 0x66 && buffer[5] == 0x74 && buffer[6] == 0x79 && buffer[7] == 0x70)
                    return FileType.Video;
                
                // HTML检测
                string fileContent = string.Empty;
                try
                {
                    // 读取文件前1024个字符
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        char[] charBuffer = new char[1024];
                        reader.Read(charBuffer, 0, 1024);
                        fileContent = new string(charBuffer);
                    }
                    
                    // 简单检查HTML标记
                    fileContent = fileContent.ToLowerInvariant();
                    if (fileContent.Contains("<!doctype html>") || 
                        fileContent.Contains("<html") || 
                        fileContent.Contains("<head") || 
                        fileContent.Contains("<body"))
                        return FileType.HTML;
                }
                catch
                {
                    // 忽略读取失败的情况
                }
                
                // 无法通过文件头判断，返回未知类型
                return FileType.Unknown;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通过文件头检测文件类型时出错: {ex.Message}");
                return FileType.Unknown;
            }
        }
        
        /// <summary>
        /// 获取文件MIME类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>MIME类型字符串</returns>
        public static string GetMimeType(string filePath)
        {
            FileType type = DetectFileType(filePath);
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            switch (type)
            {
                case FileType.Image:
                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            return "image/jpeg";
                        case ".png":
                            return "image/png";
                        case ".gif":
                            return "image/gif";
                        case ".bmp":
                            return "image/bmp";
                        case ".webp":
                            return "image/webp";
                        case ".tiff":
                        case ".tif":
                            return "image/tiff";
                        case ".svg":
                            return "image/svg+xml";
                        case ".heic":
                        case ".heif":
                            return "image/heif";
                        default:
                            return "image/unknown";
                    }
                
                case FileType.Video:
                    switch (extension)
                    {
                        case ".mp4":
                            return "video/mp4";
                        case ".avi":
                            return "video/x-msvideo";
                        case ".mkv":
                            return "video/x-matroska";
                        case ".mov":
                            return "video/quicktime";
                        case ".wmv":
                            return "video/x-ms-wmv";
                        case ".flv":
                            return "video/x-flv";
                        case ".webm":
                            return "video/webm";
                        default:
                            return "video/unknown";
                    }
                
                case FileType.HTML:
                    return "text/html";
                
                case FileType.Document:
                    switch (extension)
                    {
                        case ".pdf":
                            return "application/pdf";
                        case ".doc":
                            return "application/msword";
                        case ".docx":
                            return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        case ".ppt":
                            return "application/vnd.ms-powerpoint";
                        case ".pptx":
                            return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                        case ".xls":
                            return "application/vnd.ms-excel";
                        case ".xlsx":
                            return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        case ".txt":
                            return "text/plain";
                        case ".rtf":
                            return "application/rtf";
                        default:
                            return "application/octet-stream";
                    }
                
                default:
                    return "application/octet-stream";
            }
        }
    }
}
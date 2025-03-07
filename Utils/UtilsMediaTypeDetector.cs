using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 媒体类型检测器，用于识别和分类多种类型的媒体文件
    /// </summary>
    public static class MediaTypeDetector
    {
        /// <summary>
        /// 媒体类型枚举
        /// </summary>
        public enum MediaType
        {
            /// <summary>
            /// 未知类型
            /// </summary>
            Unknown,
            
            /// <summary>
            /// 图像文件
            /// </summary>
            Image,
            
            /// <summary>
            /// 视频文件
            /// </summary>
            Video,
            
            /// <summary>
            /// 音频文件
            /// </summary>
            Audio,
            
            /// <summary>
            /// HTML/Web文件
            /// </summary>
            Web,
            
            /// <summary>
            /// 文本文件
            /// </summary>
            Text,
            
            /// <summary>
            /// GIF动画
            /// </summary>
            AnimatedGif,
            
            /// <summary>
            /// 压缩文件
            /// </summary>
            Archive
        }
        
        // 图片文件扩展名
        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".webp", ".ico", ".svg", ".heic", ".heif"
        };
        
        // 视频文件扩展名
        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp", ".ts"
        };
        
        // 音频文件扩展名
        private static readonly HashSet<string> AudioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".ogg", ".aac", ".wma", ".m4a", ".opus", ".ape", ".mid", ".midi"
        };
        
        // Web相关文件扩展名
        private static readonly HashSet<string> WebExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".html", ".htm", ".xhtml", ".svg", ".mhtml"
        };
        
        // 文本文件扩展名
        private static readonly HashSet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".log", ".xml", ".json", ".csv", ".md", ".css", ".js", ".ini", ".conf", ".cfg"
        };
        
        // 压缩文件扩展名
        private static readonly HashSet<string> ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".tgz"
        };
        
        // 文件魔数签名映射
        private static readonly Dictionary<MediaType, List<(byte[] Signature, int Offset)>> FileSignatures = 
            new Dictionary<MediaType, List<(byte[] Signature, int Offset)>>
        {
            {
                MediaType.Image, new List<(byte[] Signature, int Offset)>
                {
                    (new byte[] { 0xFF, 0xD8, 0xFF }, 0), // JPEG
                    (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0), // PNG
                    (new byte[] { 0x47, 0x49, 0x46, 0x38 }, 0), // GIF
                    (new byte[] { 0x42, 0x4D }, 0), // BMP
                    (new byte[] { 0x49, 0x49, 0x2A, 0x00 }, 0), // TIFF (little endian)
                    (new byte[] { 0x4D, 0x4D, 0x00, 0x2A }, 0), // TIFF (big endian)
                    (new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0), // WEBP (RIFF)
                }
            },
            {
                MediaType.Video, new List<(byte[] Signature, int Offset)>
                {
                    (new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }, 4), // MP4
                    (new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0), // AVI (RIFF)
                    (new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, 0), // MKV/WEBM
                    (new byte[] { 0x00, 0x00, 0x01, 0xBA }, 0), // MPEG
                    (new byte[] { 0x00, 0x00, 0x01, 0xB3 }, 0), // MPEG
                    (new byte[] { 0x46, 0x4C, 0x56 }, 0), // FLV
                }
            },
            {
                MediaType.Audio, new List<(byte[] Signature, int Offset)>
                {
                    (new byte[] { 0x49, 0x44, 0x33 }, 0), // MP3 (ID3)
                    (new byte[] { 0xFF, 0xFB }, 0), // MP3 (without ID3)
                    (new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0), // WAV (RIFF)
                    (new byte[] { 0x66, 0x4C, 0x61, 0x43 }, 0), // FLAC
                    (new byte[] { 0x4F, 0x67, 0x67, 0x53 }, 0), // OGG
                }
            },
            {
                MediaType.Web, new List<(byte[] Signature, int Offset)>
                {
                    (Encoding.ASCII.GetBytes("<!DOCTYPE HTML"), 0), // HTML
                    (Encoding.ASCII.GetBytes("<html"), 0), // HTML
                    (Encoding.ASCII.GetBytes("<?xml"), 0), // XML
                }
            },
            {
                MediaType.Archive, new List<(byte[] Signature, int Offset)>
                {
                    (new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 0), // ZIP
                    (new byte[] { 0x52, 0x61, 0x72, 0x21 }, 0), // RAR
                    (new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, 0), // 7Z
                }
            }
        };
        
        /// <summary>
        /// 获取文件的媒体类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>媒体类型</returns>
        public static MediaType GetMediaType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return MediaType.Unknown;
            
            try
            {
 // 检查是否是GIF动画
                if (Path.GetExtension(filePath).Equals(".gif", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsAnimatedGif(filePath))
                    {
                        return MediaType.AnimatedGif;
                    }
                    return MediaType.Image;
                }
                
                // 首先根据扩展名进行快速检测
                string extension = Path.GetExtension(filePath);
                
                if (ImageExtensions.Contains(extension))
                    return MediaType.Image;
                    
                if (VideoExtensions.Contains(extension))
                    return MediaType.Video;
                    
                if (AudioExtensions.Contains(extension))
                    return MediaType.Audio;
                    
                if (WebExtensions.Contains(extension))
                    return MediaType.Web;
                    
                if (TextExtensions.Contains(extension))
                    return MediaType.Text;
                    
                if (ArchiveExtensions.Contains(extension))
                    return MediaType.Archive;
                
                // 如果扩展名无法确定，则检查文件魔数
                return DetectByFileSignature(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取媒体类型失败: {ex.Message}");
                return MediaType.Unknown;
            }
        }
        
        /// <summary>
        /// 通过文件魔数检测媒体类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>媒体类型</returns>
        private static MediaType DetectByFileSignature(string filePath)
        {
            try
            {
                // 读取文件头部数据
                byte[] fileHeader = new byte[16];
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length < 16)
                        fileHeader = new byte[fs.Length];
                        
                    fs.Read(fileHeader, 0, fileHeader.Length);
                }
                
                // 检查每种媒体类型的文件签名
                foreach (var kvp in FileSignatures)
                {
                    MediaType mediaType = kvp.Key;
                    var signatures = kvp.Value;
                    
                    foreach (var (signature, offset) in signatures)
                    {
                        if (IsSignatureMatch(fileHeader, signature, offset))
                        {
                            return mediaType;
                        }
                    }
                }
                
                // 检查文本文件
                if (IsTextFile(filePath))
                    return MediaType.Text;
                    
                return MediaType.Unknown;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测文件签名失败: {ex.Message}");
                return MediaType.Unknown;
            }
        }
        
        /// <summary>
        /// 检查文件签名是否匹配
        /// </summary>
        /// <param name="fileHeader">文件头部数据</param>
        /// <param name="signature">要匹配的签名</param>
        /// <param name="offset">签名在文件头部的偏移量</param>
        /// <returns>是否匹配</returns>
        private static bool IsSignatureMatch(byte[] fileHeader, byte[] signature, int offset)
        {
            if (offset + signature.Length > fileHeader.Length)
                return false;
                
            for (int i = 0; i < signature.Length; i++)
            {
                if (fileHeader[offset + i] != signature[i])
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查是否为动画GIF文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否为动画GIF</returns>
        public static bool IsAnimatedGif(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // 确认文件是GIF格式
                    byte[] header = new byte[6];
                    if (fs.Read(header, 0, 6) != 6)
                        return false;
                        
                    // 检查GIF标头
                    if (!IsSignatureMatch(header, new byte[] { 0x47, 0x49, 0x46, 0x38 }, 0)) // "GIF8"
                        return false;
                        
                    // 跳过GIF文件头 (10字节) 和 全局颜色表 (如果存在)
                    fs.Position = 6; // 跳过GIF文件头的前6字节
                    
                    // 读取屏幕宽度、高度等基本信息和全局颜色表标志
                    byte[] logicalScreenDescriptor = new byte[7];
                    if (fs.Read(logicalScreenDescriptor, 0, 7) != 7)
                        return false;
                        
                    // 获取全局颜色表标志和大小
                    bool hasGlobalColorTable = (logicalScreenDescriptor[4] & 0x80) != 0;
                    int globalColorTableSize = 0;
                    
                    if (hasGlobalColorTable)
                    {
                        globalColorTableSize = 2 << (logicalScreenDescriptor[4] & 0x07);
                        fs.Seek(3 * globalColorTableSize, SeekOrigin.Current); // 跳过全局颜色表
                    }
                    
                    // 查找图像块并计算帧数
                    int frameCount = 0;
                    byte nextByte;
                    
                    while ((nextByte = (byte)fs.ReadByte()) != -1)
                    {
                        if (nextByte == 0x21) // 扩展块
                        {
                            byte extensionType = (byte)fs.ReadByte();
                            
                            if (extensionType == 0xF9) // 图形控制扩展块
                            {
                                frameCount++;
                                
                                // 跳过扩展块数据
                                byte blockSize = (byte)fs.ReadByte();
                                fs.Seek(blockSize, SeekOrigin.Current);
                                
                                // 跳过块终结器
                                if (fs.ReadByte() != 0)
                                {
                                    // 格式异常，不是标准GIF
                                    break;
                                }
                            }
                            else
                            {
                                // 跳过其他扩展块
                                SkipExtensionBlock(fs);
                            }
                        }
                        else if (nextByte == 0x2C) // 图像描述符
                        {
                            // 如果没有图形控制扩展块，则增加帧计数
                            if (frameCount == 0)
                                frameCount++;
                                
                            // 跳过图像描述符的其余部分
                            fs.Seek(8, SeekOrigin.Current);
                            
                            // 检查是否有局部颜色表
                            byte packed = (byte)fs.ReadByte();
                            bool hasLocalColorTable = (packed & 0x80) != 0;
                            
                            if (hasLocalColorTable)
                            {
                                int localColorTableSize = 2 << (packed & 0x07);
                                fs.Seek(3 * localColorTableSize, SeekOrigin.Current); // 跳过局部颜色表
                            }
                            
                            // 跳过LZW最小码长度
                            fs.ReadByte();
                            
                            // 跳过图像数据块
                            SkipDataSubBlocks(fs);
                        }
                        else if (nextByte == 0x3B) // GIF文件结束标记
                        {
                            break;
                        }
                    }
                    
                    // 如果有多个帧，则认为是动画GIF
                    return frameCount > 1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测动画GIF失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 跳过扩展块
        /// </summary>
        /// <param name="fs">文件流</param>
        private static void SkipExtensionBlock(FileStream fs)
        {
            int blockSize;
            
            // 跳过所有子数据块，直到遇到块终结器（0字节）
            while ((blockSize = fs.ReadByte()) > 0)
            {
                fs.Seek(blockSize, SeekOrigin.Current);
            }
        }
        
        /// <summary>
        /// 跳过数据子块
        /// </summary>
        /// <param name="fs">文件流</param>
        private static void SkipDataSubBlocks(FileStream fs)
        {
            int blockSize;
            
            // 跳过所有子数据块，直到遇到块终结器（0字节）
            while ((blockSize = fs.ReadByte()) > 0)
            {
                fs.Seek(blockSize, SeekOrigin.Current);
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
                // 最多检查前4KB
                const int maxBytesToCheck = 4096;
                
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[Math.Min(maxBytesToCheck, fs.Length)];
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);
                    
                    // 检查是否含有过多控制字符或空字节
                    int controlCharCount = 0;
                    int nullByteCount = 0;
                    
                    for (int i = 0; i < bytesRead; i++)
                    {
                        byte b = buffer[i];
                        
                        if (b == 0)
                        {
                            nullByteCount++;
                        }
                        else if (b < 32 && b != 9 && b != 10 && b != 13) // 不包括Tab, LF, CR
                        {
                            controlCharCount++;
                        }
                    }
                    
                    // 如果包含空字节，几乎可以确定是二进制文件
                    if (nullByteCount > 0)
                        return false;
                        
                    // 如果控制字符过多，则可能是二进制文件
                    double controlCharPercentage = (double)controlCharCount / bytesRead;
                    return controlCharPercentage < 0.1; // 控制字符占比小于10%视为文本文件
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测文本文件失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取媒体类型的描述
        /// </summary>
        /// <param name="mediaType">媒体类型</param>
        /// <returns>描述字符串</returns>
        public static string GetMediaTypeDescription(MediaType mediaType)
        {
            switch (mediaType)
            {
                case MediaType.Image:
                    return "图片文件";
                case MediaType.AnimatedGif:
                    return "GIF动画";
                case MediaType.Video:
                    return "视频文件";
                case MediaType.Audio:
                    return "音频文件";
                case MediaType.Web:
                    return "网页文件";
                case MediaType.Text:
                    return "文本文件";
                case MediaType.Archive:
                    return "压缩文件";
                case MediaType.Unknown:
                default:
                    return "未知类型";
            }
        }
        
        /// <summary>
        /// 检测URL的媒体类型
        /// </summary>
        /// <param name="url">URL地址</param>
        /// <returns>媒体类型</returns>
        public static MediaType GetMediaTypeFromUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return MediaType.Unknown;
                    
                // 尝试从URL中提取文件名和扩展名
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    return MediaType.Unknown;
                    
                string path = uri.AbsolutePath;
                string extension = Path.GetExtension(path);
                
                if (string.IsNullOrEmpty(extension))
                {
                    // 如果URL没有扩展名，检查一些常见的视频流URL模式
                    if (url.Contains("youtube.com/watch") || url.Contains("youtu.be/") || 
                        url.Contains("vimeo.com") || url.Contains("dailymotion.com") ||
                        url.Contains("bilibili.com/video"))
                    {
                        return MediaType.Video;
                    }
                    
                    // 一般网址默认为Web类型
                    return MediaType.Web;
                }
                
                // 根据扩展名判断
                if (ImageExtensions.Contains(extension))
                    return MediaType.Image;
                    
                if (VideoExtensions.Contains(extension))
                    return MediaType.Video;
                    
                if (AudioExtensions.Contains(extension))
                    return MediaType.Audio;
                    
                if (WebExtensions.Contains(extension))
                    return MediaType.Web;
                    
                if (TextExtensions.Contains(extension))
                    return MediaType.Text;
                    
                if (ArchiveExtensions.Contains(extension))
                    return MediaType.Archive;
                    
                return MediaType.Unknown;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"从URL获取媒体类型失败: {ex.Message}");
                return MediaType.Unknown;
            }
        }
        
        /// <summary>
        /// 获取媒体类型对应的MIME类型
        /// </summary>
        /// <param name="mediaType">媒体类型</param>
        /// <param name="extension">文件扩展名（可选，用于细化MIME类型）</param>
        /// <returns>MIME类型字符串</returns>
        public static string GetMimeType(MediaType mediaType, string extension = null)
        {
            switch (mediaType)
            {
                case MediaType.Image:
                    if (!string.IsNullOrEmpty(extension))
                    {
                        extension = extension.ToLowerInvariant().TrimStart('.');
                        switch (extension)
                        {
                            case "jpg":
                            case "jpeg":
                                return "image/jpeg";
                            case "png":
                                return "image/png";
                            case "gif":
                                return "image/gif";
                            case "bmp":
                                return "image/bmp";
                            case "webp":
                                return "image/webp";
                            case "svg":
                                return "image/svg+xml";
                            case "ico":
                                return "image/x-icon";
                        }
                    }
                    return "image/*";
                    
                case MediaType.AnimatedGif:
                    return "image/gif";
                    
                case MediaType.Video:
                    if (!string.IsNullOrEmpty(extension))
                    {
                        extension = extension.ToLowerInvariant().TrimStart('.');
                        switch (extension)
                        {
                            case "mp4":
                                return "video/mp4";
                            case "webm":
                                return "video/webm";
                            case "avi":
                                return "video/x-msvideo";
                            case "mkv":
                                return "video/x-matroska";
                            case "mov":
                                return "video/quicktime";
                            case "wmv":
                                return "video/x-ms-wmv";
                        }
                    }
                    return "video/*";
                    
                case MediaType.Audio:
                    if (!string.IsNullOrEmpty(extension))
                    {
                        extension = extension.ToLowerInvariant().TrimStart('.');
                        switch (extension)
                        {
                            case "mp3":
                                return "audio/mpeg";
                            case "wav":
                                return "audio/wav";
                            case "ogg":
                                return "audio/ogg";
                            case "flac":
                                return "audio/flac";
                            case "aac":
                                return "audio/aac";
                        }
                    }
                    return "audio/*";
                    
                case MediaType.Web:
                    return "text/html";
                    
                case MediaType.Text:
                    if (!string.IsNullOrEmpty(extension))
                    {
                        extension = extension.ToLowerInvariant().TrimStart('.');
                        switch (extension)
                        {
                            case "txt":
                                return "text/plain";
                            case "xml":
                                return "text/xml";
                            case "json":
                                return "application/json";
                            case "csv":
                                return "text/csv";
                            case "css":
                                return "text/css";
                            case "js":
                                return "text/javascript";
                        }
                    }
                    return "text/plain";
                    
                case MediaType.Archive:
                    if (!string.IsNullOrEmpty(extension))
                    {
                        extension = extension.ToLowerInvariant().TrimStart('.');
                        switch (extension)
                        {
                            case "zip":
                                return "application/zip";
                            case "rar":
                                return "application/x-rar-compressed";
                            case "7z":
                                return "application/x-7z-compressed";
                            case "tar":
                                return "application/x-tar";
                            case "gz":
                                return "application/gzip";
                        }
                    }
                    return "application/octet-stream";
                    
                case MediaType.Unknown:
                default:
                    return "application/octet-stream";
            }
        }
        
        /// <summary>
        /// 获取媒体类型对应的图标
        /// </summary>
        /// <param name="mediaType">媒体类型</param>
        /// <returns>图标资源路径</returns>
        public static string GetMediaTypeIconPath(MediaType mediaType)
        {
            // 返回各种媒体类型对应的图标资源路径
            switch (mediaType)
            {
                case MediaType.Image:
                    return "/Resources/Icons/image.png";
                case MediaType.AnimatedGif:
                    return "/Resources/Icons/animation.png";
                case MediaType.Video:
                    return "/Resources/Icons/video.png";
                case MediaType.Audio:
                    return "/Resources/Icons/audio.png";
                case MediaType.Web:
                    return "/Resources/Icons/web.png";
                case MediaType.Text:
                    return "/Resources/Icons/text.png";
                case MediaType.Archive:
                    return "/Resources/Icons/archive.png";
                case MediaType.Unknown:
                default:
                    return "/Resources/Icons/unknown.png";
            }
        }
    }
}
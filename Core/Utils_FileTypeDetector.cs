using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LuckyStars.Models;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 文件类型检测工具，用于识别文件的实际类型
    /// </summary>
    public class FileTypeDetector
    {
        // 文件魔数字典，用于按二进制特征识别文件
        private static readonly Dictionary<string, byte[]> FileMagicNumbers = new Dictionary<string, byte[]>
        {
            // 图片格式
            { "jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { "png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            { "gif", new byte[] { 0x47, 0x49, 0x46, 0x38 } },
            { "bmp", new byte[] { 0x42, 0x4D } },
            { "webp", new byte[] { 0x52, 0x49, 0x46, 0x46 } }, // WEBP需要进一步检查
            
            // 视频格式
            { "mp4", new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 } }, // 检查ftyp
            { "mkv", new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } },
            { "avi", new byte[] { 0x52, 0x49, 0x46, 0x46 } }, // AVI也是RIFF格式，需要进一步检查
            { "webm", new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } }, // 与MKV使用相同的容器格式
            
            // 其他格式
            { "html", new byte[] { 0x3C, 0x21, 0x44, 0x4F, 0x43, 0x54, 0x59, 0x50, 0x45 } }, // <!DOCTYPE
            { "htm", new byte[] { 0x3C, 0x68, 0x74, 0x6D, 0x6C } }, // <html
            { "zip", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            { "exe", new byte[] { 0x4D, 0x5A } } // MZ
        };

        /// <summary>
        /// 检测文件的实际类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>识别到的文件类型</returns>
        public static FileType DetectFileType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("文件不存在", filePath);
            }

            try
            {
                // 首先尝试通过文件头识别
                var magicType = DetectByMagicNumbers(filePath);
                if (magicType != FileType.Unknown)
                {
                    return magicType;
                }

                // 如果魔数检测失败，使用扩展名
                string extension = Path.GetExtension(filePath).TrimStart('.').ToLower();
                
                // 注册的图片格式
                if (new[] { "jpg", "jpeg", "png", "bmp", "gif", "webp", "tiff", "tif" }.Contains(extension))
                {
                    return FileType.Image;
                }
                
                // 注册的视频格式
                if (new[] { "mp4", "avi", "mkv", "webm", "mov", "wmv", "flv", "m4v" }.Contains(extension))
                {
                    return FileType.Video;
                }
                
                // HTML格式
                if (new[] { "html", "htm" }.Contains(extension))
                {
                    return FileType.Html;
                }
                
                // 应用程序
                if (new[] { "exe", "lnk", "url" }.Contains(extension))
                {
                    return FileType.Application;
                }
                
                // GIF单独判断，因为它既是图片也是动画
                if (extension == "gif")
                {
                    return IsAnimatedGif(filePath) ? FileType.Gif : FileType.Image;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"文件类型检测失败: {ex.Message}");
            }

            // 无法识别或发生错误
            return FileType.Unknown;
        }

        /// <summary>
        /// 根据文件魔数检测文件类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>检测到的文件类型</returns>
        private static FileType DetectByMagicNumbers(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length < 8) // 至少需要8字节来检测大多数文件类型
                    {
                        return FileType.Unknown;
                    }

                    byte[] buffer = new byte[16]; // 读取文件前16字节
                    fs.Read(buffer, 0, buffer.Length);

                    // 检查每种已知格式
                    foreach (var magicNumber in FileMagicNumbers)
                    {
                        if (buffer.Length >= magicNumber.Value.Length && 
                            buffer.Take(magicNumber.Value.Length).SequenceEqual(magicNumber.Value))
                        {
                            // 特殊处理：RIFF格式需要进一步判断
                            if (magicNumber.Key == "webp" || magicNumber.Key == "avi")
                            {
                                // 对于RIFF格式，需要检查第8字节开始的4个字节
                                fs.Position = 8;
                                byte[] formatBuffer = new byte[4];
                                fs.Read(formatBuffer, 0, 4);
                                
                                if (formatBuffer.SequenceEqual(new byte[] { 0x57, 0x45, 0x42, 0x50 })) // "WEBP"
                                {
                                    return FileType.Image;
                                }
                                else if (formatBuffer.SequenceEqual(new byte[] { 0x41, 0x56, 0x49, 0x20 })) // "AVI "
                                {
                                    return FileType.Video;
                                }
                            }
                            else
                            {
                                // 其他格式直接映射
                                switch (magicNumber.Key)
                                {
                                    case "jpg":
                                    case "png":
                                    case "bmp":
                                    case "webp":
                                        return FileType.Image;
                                    case "gif":
                                        return IsAnimatedGif(filePath) ? FileType.Gif : FileType.Image;
                                    case "mp4":
                                    case "mkv":
                                    case "webm":
                                        return FileType.Video;
                                    case "html":
                                    case "htm":
                                        return FileType.Html;
                                    case "exe":
                                        return FileType.Application;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"魔数检测失败: {ex.Message}");
            }

            return FileType.Unknown;
        }

        /// <summary>
        /// 检测GIF是否为动画GIF
        /// </summary>
        /// <param name="filePath">GIF文件路径</param>
        /// <returns>如果是动画GIF返回true</returns>
        public static bool IsAnimatedGif(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // GIF文件至少需要13字节的头部信息
                    if (fs.Length < 13)
                    {
                        return false;
                    }

                    // 跳过GIF头部6字节 (GIF89a 或 GIF87a)
                    fs.Position = 6;

                    // 跳过逻辑屏幕描述符 (宽度2字节 + 高度2字节 + 1字节包：全局颜色表标志, 颜色分辨率, 排序标
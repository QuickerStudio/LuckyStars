using System.Collections.Generic;
using System.Linq;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 支持的文件格式管理类，集中管理所有支持的文件格式
    /// </summary>
    public static class SupportedFormats
    {
        // 支持的图片格式
        private static readonly string[] _imageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };
        
        // 支持的视频格式
        private static readonly string[] _videoExtensions = { ".mp4", ".avi", ".mkv", ".webm", ".mov" };
        
        // 支持的音频格式
        private static readonly string[] _audioExtensions = { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".wma", ".m4a" };

        /// <summary>
        /// 获取所有支持的图片格式扩展名
        /// </summary>
        /// <returns>图片格式扩展名数组</returns>
        public static string[] GetImageExtensions() => _imageExtensions;

        /// <summary>
        /// 获取所有支持的视频格式扩展名
        /// </summary>
        /// <returns>视频格式扩展名数组</returns>
        public static string[] GetVideoExtensions() => _videoExtensions;

        /// <summary>
        /// 获取所有支持的音频格式扩展名
        /// </summary>
        /// <returns>音频格式扩展名数组</returns>
        public static string[] GetAudioExtensions() => _audioExtensions;

        /// <summary>
        /// 获取所有支持的媒体格式扩展名
        /// </summary>
        /// <returns>所有媒体格式扩展名数组</returns>
        public static string[] GetAllSupportedExtensions()
        {
            return _imageExtensions.Concat(_videoExtensions).Concat(_audioExtensions).ToArray();
        }

        /// <summary>
        /// 获取用于文件搜索的图片格式通配符
        /// </summary>
        /// <returns>图片格式通配符数组</returns>
        public static string[] GetImageWildcards()
        {
            return _imageExtensions.Select(ext => $"*{ext}").ToArray();
        }

        /// <summary>
        /// 获取用于文件搜索的视频格式通配符
        /// </summary>
        /// <returns>视频格式通配符数组</returns>
        public static string[] GetVideoWildcards()
        {
            return _videoExtensions.Select(ext => $"*{ext}").ToArray();
        }

        /// <summary>
        /// 获取用于文件搜索的音频格式通配符
        /// </summary>
        /// <returns>音频格式通配符数组</returns>
        public static string[] GetAudioWildcards()
        {
            return _audioExtensions.Select(ext => $"*{ext}").ToArray();
        }

        /// <summary>
        /// 检查文件是否为支持的图片格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否为支持的图片格式</returns>
        public static bool IsImageFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return _imageExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件是否为支持的视频格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否为支持的视频格式</returns>
        public static bool IsVideoFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return _videoExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件是否为支持的音频格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否为支持的音频格式</returns>
        public static bool IsAudioFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return _audioExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件是否为支持的任何媒体格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否为支持的媒体格式</returns>
        public static bool IsSupportedFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return _imageExtensions.Contains(extension) || 
                   _videoExtensions.Contains(extension) || 
                   _audioExtensions.Contains(extension);
        }
    }
}

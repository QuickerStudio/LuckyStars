using System;
using System.IO;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using LuckyStars.Utils;

namespace LuckyStars.Models
{
    /// <summary>
    /// 壁纸信息模型
    /// </summary>
    public class WallpaperInfo
    {
        /// <summary>
        /// 壁纸类型
        /// </summary>
        public enum WallpaperType
        {
            /// <summary>
            /// 图片壁纸
            /// </summary>
            Picture,
            
            /// <summary>
            /// HTML壁纸
            /// </summary>
            Web,
            
            /// <summary>
            /// 视频壁纸
            /// </summary>
            Video,
            
            /// <summary>
            /// 未知类型
            /// </summary>
            Unknown
        }
        
        /// <summary>
        /// 壁纸唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 壁纸名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 壁纸文件路径
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// 壁纸类型
        /// </summary>
        public WallpaperType Type { get; set; } = WallpaperType.Unknown;
        
        /// <summary>
        /// 壁纸预览图路径
        /// </summary>
        public string ThumbnailPath { get; set; }
        
        /// <summary>
        /// 壁纸添加时间
        /// </summary>
        public DateTime AddedTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 壁纸上次使用时间
        /// </summary>
        public DateTime LastUsedTime { get; set; } = DateTime.MinValue;
        
        /// <summary>
        /// 壁纸描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 壁纸标签（用逗号分隔）
        /// </summary>
        public string Tags { get; set; }
        
        /// <summary>
        /// 壁纸作者
        /// </summary>
        public string Author { get; set; }
        
        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// 是否为默认壁纸
        /// </summary>
        public bool IsDefault { get; set; }
        
        /// <summary>
        /// 壁纸MD5哈希值（用于判断重复）
        /// </summary>
        public string MD5Hash { get; set; }
        
        /// <summary>
        /// 是否喜欢
        /// </summary>
        public bool IsFavorite { get; set; }
        
        /// <summary>
        /// 壁纸使用计数
        /// </summary>
        public int UsageCount { get; set; }
        
        /// <summary>
        /// 预览图缓存
        /// </summary>
        [JsonIgnore]
        public BitmapImage ThumbnailCache { get; set; }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WallpaperInfo()
        {
        }
        
        /// <summary>
        /// 从文件路径创建壁纸信息
        /// </summary>
        /// <param name="filePath">壁纸文件路径</param>
        public WallpaperInfo(string filePath)
        {
            FilePath = filePath;
            
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;
                
            // 设置文件名为壁纸名称
            Name = Path.GetFileName(filePath);
            
            // 获取文件大小
            FileSize = new FileInfo(filePath).Length;
            
            // 根据文件扩展名确定壁纸类型
            DetermineWallpaperType();
        }
        
        /// <summary>
        /// 确定壁纸类型
        /// </summary>
        private void DetermineWallpaperType()
        {
            if (string.IsNullOrEmpty(FilePath))
                return;
                
            string extension = Path.GetExtension(FilePath).ToLowerInvariant();
            
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                case ".webp":
                    Type = WallpaperType.Picture;
                    break;
                    
                case ".html":
                case ".htm":
                    Type = WallpaperType.Web;
                    break;
                    
                case ".mp4":
                case ".avi":
                case ".mkv":
                case ".mov":
                case ".webm":
                    Type = WallpaperType.Video;
                    break;
                    
                default:
                    Type = WallpaperType.Unknown;
                    break;
            }
        }
        
        /// <summary>
        /// 获取壁纸的MIME类型
        /// </summary>
        /// <returns>MIME类型</returns>
        public string GetMimeType()
        {
            if (string.IsNullOrEmpty(FilePath))
                return "application/octet-stream";
                
            return FileTypeDetector.GetMimeType(FilePath);
        }
        
        /// <summary>
        /// 将壁纸信息标记为已使用
        /// </summary>
        public void MarkAsUsed()
        {
            LastUsedTime = DateTime.Now;
            UsageCount++;
        }
        
        /// <summary>
        /// 设置喜欢状态
        /// </summary>
        /// <param name="favorite">是否喜欢</param>
        public void SetFavorite(bool favorite)
        {
            IsFavorite = favorite;
        }
        
        /// <summary>
        /// 获取标签列表
        /// </summary>
        /// <returns>标签数组</returns>
        public string[] GetTagArray()
        {
            if (string.IsNullOrEmpty(Tags))
                return Array.Empty<string>();
                
            return Tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(t => t.Trim())
                      .ToArray();
        }
        
        /// <summary>
        /// 添加标签
        /// </summary>
        /// <param name="tag">标签</param>
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;
                
            string[] tags = GetTagArray();
            
            // 检查标签是否已存在
            if (!tags.Contains(tag))
            {
                if (string.IsNullOrEmpty(Tags))
                    Tags = tag;
                else
                    Tags += "," + tag;
            }
        }
        
        /// <summary>
        /// 移除标签
        /// </summary>
        /// <param name="tag">标签</param>
        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrEmpty(Tags))
                return;
                
            string[] tags = GetTagArray();
            
            // 过滤掉要删除的标签
            Tags = string.Join(",", tags.Where(t => t != tag));
        }
        
        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <returns>文件是否存在</returns>
        public bool FileExists()
        {
            return !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
        }
        
        /// <summary>
        /// 从JSON反序列化后处理
        /// </summary>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            // 检查文件是否存在，如果不存在，设置特殊标记
            if (!FileExists())
            {
                Description = $"{Description} [文件已丢失]".Trim();
            }
            
            // 如果类型为Unknown，尝试重新确定
            if (Type == WallpaperType.Unknown && FileExists())
            {
                DetermineWallpaperType();
            }
        }
        
        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns>信息字符串</returns>
        public override string ToString()
        {
            return $"{Name} [{Type}]";
        }
    }
}
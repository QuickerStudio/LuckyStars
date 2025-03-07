using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Diagnostics;
using LuckyStars.Utils;

namespace LuckyStars.Core
{
    /// <summary>
    /// 基础格式转换器，提供基本的文件格式转换功能
    /// </summary>
    public class FormatConverter : IDisposable
    {
        // FFmpeg管理器
        protected readonly FFmpegManager _ffmpegManager;
        
        // 临时输出目录
        protected readonly string _tempOutputDir;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ffmpegManager">FFmpeg管理器</param>
        public FormatConverter(FFmpegManager ffmpegManager)
        {
            _ffmpegManager = ffmpegManager;
            
            // 创建临时输出目录
            _tempOutputDir = Path.Combine(Path.GetTempPath(), "LuckyStars", "TempConvert");
            Directory.CreateDirectory(_tempOutputDir);
        }
        
         /// <summary>
        /// 将视频转换为图片
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>生成的图片路径</returns>
        public async Task<string> ConvertVideoToImageAsync(string videoPath, string outputDirectory = null)
        {
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
                return null;
            
            if (string.IsNullOrEmpty(outputDirectory))
                outputDirectory = _tempOutputDir;
            
            try
            {
                // 生成唯一的输出文件路径
                string fileName = Path.GetFileNameWithoutExtension(videoPath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg");
                
                // 构建FFmpeg命令，提取视频中的关键帧作为图片
                string arguments = $"-i \"{videoPath}\" -vframes 1 -q:v 2 \"{outputPath}\"";
                
                // 执行FFmpeg命令
                var result = await _ffmpegManager.ExecuteCommandAsync("视频转图片", arguments);
                
                if (result.Success && File.Exists(outputPath))
                {
                    Debug.WriteLine($"视频转图片成功: {outputPath}");
                    return outputPath;
                }
                else
                {
                    Debug.WriteLine($"视频转图片失败: {result.ErrorOutput}");
                    return await ConvertVideoToImageFallbackAsync(videoPath, outputDirectory);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"视频转图片异常: {ex.Message}");
                return await ConvertVideoToImageFallbackAsync(videoPath, outputDirectory);
            }
        }
        
        /// <summary>
        /// 视频转图片的备用方法
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>生成的图片路径</returns>
        protected async Task<string> ConvertVideoToImageFallbackAsync(string videoPath, string outputDirectory)
        {
            try
            {
                // 生成唯一的输出文件路径
                string fileName = Path.GetFileNameWithoutExtension(videoPath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_fallback_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg");
                
                // 尝试从视频30%处获取帧
                string arguments = $"-ss 30% -i \"{videoPath}\" -vframes 1 -q:v 2 \"{outputPath}\"";
                
                var result = await _ffmpegManager.ExecuteCommandAsync("视频转图片备用", arguments);
                
                if (result.Success && File.Exists(outputPath))
                {
                    Debug.WriteLine($"视频转图片(备用方法)成功: {outputPath}");
                    return outputPath;
                }
                
                // 如果还是失败，尝试使用.NET的方法处理
                return CreateDefaultImageForVideo(videoPath, outputDirectory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"视频转图片备用方法异常: {ex.Message}");
                return CreateDefaultImageForVideo(videoPath, outputDirectory);
            }
        }
        
        /// <summary>
        /// 为视频创建默认图片
        /// </summary>
        /// <param name="videoPath">视频路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>生成的图片路径</returns>
        protected string CreateDefaultImageForVideo(string videoPath, string outputDirectory)
        {
            try
            {
                string fileName = Path.GetFileName(videoPath);
                string outputPath = Path.Combine(outputDirectory, $"video_default_{Guid.NewGuid().ToString().Substring(0, 8)}.png");
                
                using (Bitmap bitmap = new Bitmap(800, 450))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // 填充背景
                        g.Clear(Color.Black);
                        
                        // 绘制视频图标和文件名
                        using (Font titleFont = new Font("微软雅黑", 16, FontStyle.Bold))
                        {
                            string title = "视频文件";
                            SizeF titleSize = g.MeasureString(title, titleFont);
                            g.DrawString(title, titleFont, Brushes.White, 
                                (bitmap.Width - titleSize.Width) / 2, 150);
                        }
                        
                        using (Font fileFont = new Font("微软雅黑", 12))
                        {
                            string fileText = fileName;
                            if (fileText.Length > 40)
                                fileText = fileText.Substring(0, 37) + "...";
                                
                            SizeF fileSize = g.MeasureString(fileText, fileFont);
                            g.DrawString(fileText, fileFont, Brushes.LightGray, 
                                (bitmap.Width - fileSize.Width) / 2, 200);
                        }
                        
                        // 绘制播放按钮
                        int circleSize = 60;
                        g.FillEllipse(new SolidBrush(Color.FromArgb(150, 255, 255, 255)), 
                            (bitmap.Width - circleSize) / 2, 
                            (bitmap.Height - circleSize) / 2,
                            circleSize, circleSize);
                            
                        // 绘制三角形
                        Point[] triangle = new Point[]
                        {
                            new Point(bitmap.Width / 2 - 10, bitmap.Height / 2 - 15),
                            new Point(bitmap.Width / 2 - 10, bitmap.Height / 2 + 15),
                            new Point(bitmap.Width / 2 + 15, bitmap.Height / 2)
                        };
                        g.FillPolygon(Brushes.Black, triangle);
                    }
                    
                    // 保存图片
                    bitmap.Save(outputPath, ImageFormat.Png);
                }
                
                Debug.WriteLine($"为视频创建了默认图片: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建视频默认图片异常: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 转换图片格式
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <param name="targetFormat">目标格式</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>转换后的图片路径</returns>
        public async Task<string> ConvertImageFormatAsync(string imagePath, ImageFormat targetFormat, string outputDirectory = null)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;
                
            if (string.IsNullOrEmpty(outputDirectory))
                outputDirectory = _tempOutputDir;
                
            try
            {
                // 获取目标格式的扩展名
                string extension = GetImageFormatExtension(targetFormat);
                if (string.IsNullOrEmpty(extension))
                    return null;
                    
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}");
                
                // 使用.NET原生功能转换图片格式
                using (Image image = Image.FromFile(imagePath))
                {
                    await Task.Run(() => image.Save(outputPath, targetFormat));
                }
                
                if (File.Exists(outputPath))
                {
                    Debug.WriteLine($"图片格式转换成功: {outputPath}");
                    return outputPath;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"图片格式转换异常: {ex.Message}");
                
                // 尝试使用FFmpeg进行转换
                return await ConvertImageWithFFmpegAsync(imagePath, targetFormat, outputDirectory);
            }
        }
        
        /// <summary>
        /// 使用FFmpeg转换图片格式
        /// </summary>
        private async Task<string> ConvertImageWithFFmpegAsync(string imagePath, ImageFormat targetFormat, string outputDirectory)
        {
            try
            {
                string extension = GetImageFormatExtension(targetFormat);
                if (string.IsNullOrEmpty(extension))
                    return null;
                    
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_ffmpeg_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}");
                
                // 构建FFmpeg命令
                string arguments = $"-i \"{imagePath}\" \"{outputPath}\"";
                
                var result = await _ffmpegManager.ExecuteCommandAsync("图片格式转换", arguments);
                
                if (result.Success && File.Exists(outputPath))
                {
                    Debug.WriteLine($"使用FFmpeg转换图片格式成功: {outputPath}");
                    return outputPath;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"使用FFmpeg转换图片格式异常: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取ImageFormat对应的文件扩展名
        /// </summary>
        private string GetImageFormatExtension(ImageFormat format)
        {
            if (format.Equals(ImageFormat.Jpeg))
                return ".jpg";
            else if (format.Equals(ImageFormat.Png))
                return ".png";
            else if (format.Equals(ImageFormat.Bmp))
                return ".bmp";
            else if (format.Equals(ImageFormat.Gif))
                return ".gif";
            else if (format.Equals(ImageFormat.Tiff))
                return ".tiff";
            else
                return ".jpg"; // 默认为JPEG
        }
        
        /// <summary>
        /// 创建图片的缩略图
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="preserveAspectRatio">是否保持纵横比</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>缩略图路径</returns>
        public async Task<string> CreateThumbnailAsync(string imagePath, int width, int height, bool preserveAspectRatio = true, string outputDirectory = null)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;
                
            if (string.IsNullOrEmpty(outputDirectory))
                outputDirectory = _tempOutputDir;
                
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string extension = Path.GetExtension(imagePath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_thumb_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}");
                
                // 使用.NET原生功能创建缩略图
                using (Image originalImage = Image.FromFile(imagePath))
                {
                    // 计算缩略图尺寸
                    int thumbWidth = width;
                    int thumbHeight = height;
                    
                    if (preserveAspectRatio)
                    {
                        float ratio = Math.Min((float)width / originalImage.Width, (float)height / originalImage.Height);
                        thumbWidth = (int)(originalImage.Width * ratio);
                        thumbHeight = (int)(originalImage.Height * ratio);
                    }
                    
                    using (Bitmap thumbnail = new Bitmap(width, height))
                    {
                        using (Graphics g = Graphics.FromImage(thumbnail))
                        {
                            // 设置高质量缩放
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            
                            // 填充背景色
                            g.Clear(Color.White);
                            
                            // 计算居中位置
                            int x = (width - thumbWidth) / 2;
                            int y = (height - thumbHeight) / 2;
                            
                            // 绘制缩放后的图片
                            g.DrawImage(originalImage, 
                                new Rectangle(x, y, thumbWidth, thumbHeight),
                                new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                                GraphicsUnit.Pixel);
                        }
                        
                        // 保存缩略图
                        await Task.Run(() => thumbnail.Save(outputPath, originalImage.RawFormat));
                    }
                }
                
                if (File.Exists(outputPath))
                {
                    Debug.WriteLine($"缩略图创建成功: {outputPath}");
                    return outputPath;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建缩略图异常: {ex.Message}");
                
                // 尝试使用FFmpeg创建缩略图
                return await CreateThumbnailWithFFmpegAsync(imagePath, width, height, outputDirectory);
            }
        }
        
        /// <summary>
        /// 使用FFmpeg创建缩略图
        /// </summary>
        private async Task<string> CreateThumbnailWithFFmpegAsync(string imagePath, int width, int height, string outputDirectory)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_thumb_ffmpeg_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg");
                
                // 构建FFmpeg命令，使用scale滤镜调整大小
                string arguments = $"-i \"{imagePath}\" -vf \"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:color=white\" \"{outputPath}\"";
                
                var result = await _ffmpegManager.ExecuteCommandAsync("创建缩略图", arguments);
                
                if (result.Success && File.Exists(outputPath))
                {
                    Debug.WriteLine($"使用FFmpeg创建缩略图成功: {outputPath}");
                    return outputPath;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"使用FFmpeg创建缩略图异常: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 调整图片大小
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="preserveAspectRatio">是否保持纵横比</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>调整后的图片路径</returns>
        public async Task<string> ResizeImageAsync(string imagePath, int width, int height, bool preserveAspectRatio = true, string outputDirectory = null)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;
                
            if (string.IsNullOrEmpty(outputDirectory))
                outputDirectory = _tempOutputDir;
                
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string extension = Path.GetExtension(imagePath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_resized_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}");
                
                using (Image originalImage = Image.FromFile(imagePath))
                {
                    // 计算调整后的尺寸
                    int newWidth = width;
                    int newHeight = height;
                    
                    if (preserveAspectRatio)
                    {
                        float ratio;
                        if ((float)originalImage.Width / originalImage.Height > (float)width / height)
                            ratio = (float)width / originalImage.Width;
                        else
                            ratio = (float)height / originalImage.Height;
                            
                        newWidth = (int)(originalImage.Width * ratio);
                        newHeight = (int)(originalImage.Height * ratio);
                    }
                    
                    using (Bitmap newImage = new Bitmap(newWidth, newHeight))
                    {
                        using (Graphics g = Graphics.FromImage(newImage))
                        {
                            // 设置高质量缩放
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            
                            // 绘制调整大小后的图片
                            g.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                        }
                        
                        // 保存新图片
                        await Task.Run(() => newImage.Save(outputPath, originalImage.RawFormat));
                    }
                }
                
                if (File.Exists(outputPath))
                {
                    Debug.WriteLine($"图片大小调整成功: {outputPath}");
                    return outputPath;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"调整图片大小异常: {ex.Message}");
                
                // 尝试使用FFmpeg调整大小
                return await ResizeImageWithFFmpegAsync(imagePath, width, height, preserveAspectRatio, outputDirectory);
            }
        }
        
        /// <summary>
        /// 使用FFmpeg调整图片大小
        /// </summary>
        private async Task<string> ResizeImageWithFFmpegAsync(string imagePath, int width, int height, bool preserveAspectRatio, string outputDirectory)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string extension = Path.GetExtension(imagePath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_resized_ffmpeg_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}");
                
                // 构建FFmpeg命令
                string scaleFilter = preserveAspectRatio ? 
                    $"scale={width}:{height}:force_original_aspect_ratio=decrease" : 
                    $"scale={width}:{height}";
                    
                string arguments = $"-i \"{imagePath}\" -vf \"{scaleFilter}\" \"{outputPath}\"";
                
                var result = await _ffmpegManager.ExecuteCommandAsync("调整图片大小", arguments);
                
                if (result.Success && File.Exists(outputPath))
                {
                    Debug.WriteLine($"使用FFmpeg调整图片大小成功: {outputPath}");
                    return outputPath;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"使用FFmpeg调整图片大小异常: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 清理临时文件
        /// </summary>
        public void CleanTempFiles()
        {
            try
            {
                if (!Directory.Exists(_tempOutputDir))
                    return;
                    
                // 清理7天前的临时文件
                foreach (string file in Directory.GetFiles(_tempOutputDir))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (DateTime.Now - fileInfo.CreationTime > TimeSpan.FromDays(7))
                    {
                        try { fileInfo.Delete(); } catch { }
                    }
                }
                
                Debug.WriteLine("临时文件清理完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理临时文件异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 清理临时文件
                CleanTempFiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放格式转换器资源异常: {ex.Message}");
            }
        }
    }
}
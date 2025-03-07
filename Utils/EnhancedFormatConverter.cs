using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 增强格式转换器，提供各种媒体格式之间的转换功能
    /// </summary>
    public class EnhancedFormatConverter : IDisposable
    {
        // FFmpeg管理器
        private readonly FFmpegManager _ffmpegManager;
        
        // 临时输出目录
        private readonly string _tempOutputDir;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ffmpegManager">FFmpeg管理器</param>
        public EnhancedFormatConverter(FFmpegManager ffmpegManager)
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
        public async Task<string> ConvertVideoToImageAsync(string videoPath, string outputDirectory)
        {
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
                return null;
            
            if (string.IsNullOrEmpty(outputDirectory) || !Directory.Exists(outputDirectory))
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
                    return outputPath;
                }
                else
                {
                    Debug.WriteLine($"视频转图片失败: {result.ErrorOutput}");
                    
                    // 尝试使用备用方法
                    return await ConvertVideoToImageFallbackAsync(videoPath, outputDirectory);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"视频转图片异常: {ex.Message}");
                
                // 尝试使用备用方法
                return await ConvertVideoToImageFallbackAsync(videoPath, outputDirectory);
            }
        }
        
        /// <summary>
        /// 备用的视频转图片方法，尝试多种截帧位置
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>生成的图片路径</returns>
        private async Task<string> ConvertVideoToImageFallbackAsync(string videoPath, string outputDirectory)
        {
            try
            {
                // 生成唯一的输出文件路径
                string fileName = Path.GetFileNameWithoutExtension(videoPath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_fallback_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg");
                
                // 尝试从视频的30%处截取帧
                string arguments = $"-ss 30% -i \"{videoPath}\" -vframes 1 -q:v 2 \"{outputPath}\"";
                
                // 执行FFmpeg命令
                var result = await _ffmpegManager.ExecuteCommandAsync("视频转图片(备用)", arguments);
                
                if (result.Success && File.Exists(outputPath))
                {
                    return outputPath;
                }
                else
                {
                    // 如果依然失败，创建一个带有视频信息的占位图像
                    return CreateVideoInfoImage(videoPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"备用视频转图片异常: {ex.Message}");
                return CreateVideoInfoImage(videoPath);
            }
        }
        
        /// <summary>
        /// 创建一个包含视频信息的图像
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <returns>生成的图片路径</returns>
        private string CreateVideoInfoImage(string videoPath)
        {
            try
            {
                // 获取视频信息
                string videoFileName = Path.GetFileName(videoPath);
                
                // 创建一个简单的包含视频名称的图像
                string outputPath = Path.Combine(_tempOutputDir, $"video_info_{Guid.NewGuid().ToString().Substring(0, 8)}.png");
                
                // 创建位图
                using (Bitmap bitmap = new Bitmap(1280, 720))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // 填充背景
                        g.Clear(Color.Black);
                        
                        // 绘制视频文件名
                        using (Font font = new Font("Arial", 24))
                        {
                            string text = $"视频: {videoFileName}";
                            SizeF textSize = g.MeasureString(text, font);
                            
                            g.DrawString(text, font, Brushes.White,
                                (bitmap.Width - textSize.Width) / 2,
                                (bitmap.Height - textSize.Height) / 2);
                        }
                        
                        // 绘制提示信息
                        using (Font smallFont = new Font("Arial", 16))
                        {
                            string hint = "无法生成视频截图，使用占位图像";
                            SizeF hintSize = g.MeasureString(hint, smallFont);
                            
                            g.DrawString(hint, smallFont, Brushes.LightGray,
                                (bitmap.Width - hintSize.Width) / 2,
                                (bitmap.Height - hintSize.Height) / 2 + 50);
                        }
                    }
                    
                    // 保存位图
                    bitmap.Save(outputPath, ImageFormat.Png);
                }
                
                return outputPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建视频信息图像异常: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 将文档转换为图片
        /// </summary>
        /// <param name="documentPath">文档文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>生成的图片路径</returns>
        public async Task<string> ConvertDocumentToImageAsync(string documentPath, string outputDirectory)
        {
            if (string.IsNullOrEmpty(documentPath) || !File.Exists(documentPath))
                return null;
            
            if (string.IsNullOrEmpty(outputDirectory) || !Directory.Exists(outputDirectory))
                outputDirectory = _tempOutputDir;
            
            try
            {
                string extension = Path.GetExtension(documentPath).ToLowerInvariant();
                
                if (extension == ".pdf")
                {
                    return await ConvertPdfToImageAsync(documentPath, outputDirectory);
                }
                else if (extension == ".doc" || extension == ".docx" ||
                         extension == ".ppt" || extension == ".pptx" ||
                         extension == ".xls" || extension == ".xlsx")
                {
                    return await ConvertOfficeToImageAsync(documentPath, outputDirectory);
                }
                else if (extension == ".txt" || extension == ".rtf")
                {
                    return CreateTextDocumentImage(documentPath, outputDirectory);
                }
                else
                {
                    // 不支持的文档类型
                    return CreateDocumentInfoImage(documentPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"文档转图片异常: {ex.Message}");
                return CreateDocumentInfoImage(documentPath);
            }
        }
        
        /// <summary>
        /// 将PDF转换为图片
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>生成的图片路径</returns>
        private async Task<string> ConvertPdfToImageAsync(string pdfPath, string outputDirectory)
        {
            try
            {
                // 生成唯一的输出文件路径
                string fileName = Path.GetFileNameWithoutExtension(pdfPath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg");
                
                // 调用FFmpeg转换PDF第一页为图像
                string arguments = $"-i \"{pdfPath}\" -vf \"scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2\" \"{outputPath}\"";
                
                // 执行FFmpeg命令
                var result = await _ffmpegManager.ExecuteCommandAsync("PDF转图片", arguments);
                
                if (result.Success && File.Exists(outputPath))
                {
                    return outputPath;
                }
                else
                {
                    Debug.WriteLine($"PDF转图片失败: {result.ErrorOutput}");
                    return CreateDocumentInfoImage(pdfPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PDF转图片异常: {ex.Message}");
                return CreateDocumentInfoImage(pdfPath);
            }
        }
        
        /// <summary>
        /// 将Office文档转换为图片
        /// </summary>
        /// <param name="officePath">Office文档路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>生成的图片路径</returns>
        private async Task<string> ConvertOfficeToImageAsync(string officePath, string outputDirectory)
        {
            try
            {
                // 由于WPF应用直接操作Office可能会有COM组件问题
                // 这里创建Office文档信息图像作为替代
                return CreateDocumentInfoImage(officePath);
                
                // NOTE: 实际实现可能需要使用其他库或服务来转换Office文档
                // 如LibreOffice、Office自动化或云服务API
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Office文档转图片异常: {ex.Message}");
                return CreateDocumentInfoImage(officePath);
            }
        }
        
        /// <summary>
        /// 创建文本文档图像
        /// </summary>
        /// <param name="textPath">文本文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>生成的图片路径</returns>
        private string CreateTextDocumentImage(string textPath, string outputDirectory)
        {
            try
            {
                string outputPath = Path.Combine(outputDirectory, $"text_{Guid.NewGuid().ToString().Substring(0, 8)}.png");
                
                // 读取文本内容
                string text = File.ReadAllText(textPath);
                
                // 限制文本长度
                if (text.Length > 2000)
                {
                    text = text.Substring(0, 2000) + "...";
                }
                
                // 创建位图
                using (Bitmap bitmap = new Bitmap(1280, 720))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // 填充背景
                        g.Clear(Color.White);
                        
                        // 绘制文本
                        using (Font font = new Font("Consolas", 12))
                        {
                            // 设置文本绘制区域
                            Rectangle rect = new Rectangle(40, 40, bitmap.Width - 80, bitmap.Height - 80);
                            
                            // 绘制标题
                            using (Font titleFont = new Font("Arial", 16, FontStyle.Bold))
                            {
                                string title = Path.GetFileName(textPath);
                                g.DrawString(title, titleFont, Brushes.DarkBlue, 40, 10);
                            }
                            
                            // 绘制文本
                            using (StringFormat format = new StringFormat())
                            {
                                format.Trimming = StringTrimming.Word;
                                g.DrawString(text, font, Brushes.Black, rect, format);
                            }
                        }
                    }
                    
                    // 保存位图
                    bitmap.Save(outputPath, ImageFormat.Png);
                }
                
                return outputPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建文本文档图像异常: {ex.Message}");
                return CreateDocumentInfoImage(textPath);
            }
        }
        
        /// <summary>
        /// 创建文档信息图像
        /// </summary>
        /// <param name="documentPath">文档文件路径</param>
        /// <returns>生成的图片路径</returns>
        private string CreateDocumentInfoImage(string documentPath)
        {
            try
            {
                string documentFileName = Path.GetFileName(documentPath);
                string extension = Path.GetExtension(documentPath).ToLowerInvariant();
                string outputPath = Path.Combine(_tempOutputDir, $"doc_info_{Guid.NewGuid().ToString().Substring(0, 8)}.png");
                
                // 根据扩展名选择颜色
                Color backgroundColor;
                
                switch (extension)
                {
                    case ".pdf":
                        backgroundColor = Color.FromArgb(200, 0, 0);
                        break;
                    case ".doc":
                    case ".docx":
                        backgroundColor = Color.FromArgb(0, 0, 150);
                        break;
                    case ".ppt":
                    case ".pptx":
                        backgroundColor = Color.FromArgb(150, 70, 0);
                        break;
                    case ".xls":
                    case ".xlsx":
                        backgroundColor = Color.FromArgb(0, 100, 0);
                        break;
                    default:
                        backgroundColor = Color.FromArgb(50, 50, 50);
                        break;
                }
                
                // 创建位图
                using (Bitmap bitmap = new Bitmap(1280, 720))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // 填充背景
                        g.Clear(backgroundColor);
                        
                        // 绘制文档图标
                        Rectangle iconRect = new Rectangle(bitmap.Width / 2 - 100, bitmap.Height / 2 - 150, 200, 200);
                        g.FillRectangle(Brushes.White, iconRect);
                        g.DrawRectangle(new Pen(Color.Gray, 2), iconRect);
                        
                        // 绘制扩展名
                        using (Font extFont = new Font("Arial", 24, FontStyle.Bold))
                        {
                            string ext = extension.TrimStart('.').ToUpper();
                            SizeF extSize = g.MeasureString(ext, extFont);
                            
                            g.DrawString(ext, extFont, Brushes.Black,
                                iconRect.Left + (iconRect.Width - extSize.Width) / 2,
                                iconRect.Top + (iconRect.Height - extSize.Height) / 2);
                        }
                        
                        // 绘制文档名称
                        using (Font font = new Font("Arial", 20))
                        {
                            string text = documentFileName;
                            SizeF textSize = g.MeasureString(text, font);
                            
                            // 如果文本太长，截断它
                            if (textSize.Width > bitmap.Width - 80)
                            {
                                while (textSize.Width > bitmap.Width - 80 && text.Length > 10)
                                {
                                    text = text.Substring(0, text.Length - 5) + "...";
                                    textSize = g.MeasureString(text, font);
                                }
                            }
                            
                            g.DrawString(text, font, Brushes.White,
                                (bitmap.Width - textSize.Width) / 2,
                                iconRect.Bottom + 30);
                        }
                    }
                    
                    // 保存位图
                    bitmap.Save(outputPath, ImageFormat.Png);
                }
                
                return outputPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建文档信息图像异常: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 创建GIF预览图
        /// </summary>
        /// <param name="gifPath">GIF文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>生成的预览图路径</returns>
        public async Task<string> CreateGifPreviewAsync(string gifPath, string outputDirectory)
        {
            if (string.IsNullOrEmpty(gifPath) || !File.Exists(gifPath))
                return null;
            
            if (string.IsNullOrEmpty(outputDirectory) || !Directory.Exists(outputDirectory))
                outputDirectory = _tempOutputDir;
            
            try
            {
                // 生成唯一的输出文件路径
                string fileName = Path.GetFileNameWithoutExtension(gifPath);
                string outputPath = Path.Combine(outputDirectory, $"{fileName}_preview_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg");
                
                // 构建FFmpeg命令，提取GIF的第一帧
                string arguments = $"-i \"{gifPath}\" -vframes 1 \"{outputPath}\"";
                
                // 执行FFmpeg命令
                var result = await _ffmpegManager.ExecuteCommandAsync("GIF预览", arguments);
                
                if (result.Success && File.Exists(outputPath))
                {
                    return outputPath;
                }
                else
                {
                    // 如果失败，直接使用原GIF
                    return gifPath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建GIF预览异常: {ex.Message}");
                return gifPath;
            }
        }
        
        /// <summary>
        /// 创建缩略图
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        /// <param name="width">目标宽度</param>
        /// <param name="height">目标高度</param>
        /// <returns>生成的缩略图路径</returns>
        public async Task<string> CreateThumbnailAsync(string imagePath, int width = 192, int height = 108)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;
            
            try
            {
                // 生成唯一的输出文件路径
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string outputPath = Path.Combine(_tempOutputDir, $"{fileName}_thumb_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg");
                
                // 构建FFmpeg命令
                string arguments = $"-i \"{imagePath}\" -vf \"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2\" \"{outputPath}\"";
                
                // 执行FFmpeg命令
                var result = await _ffmpegManager.ExecuteCommandAsync("创建缩略图", arguments);
                
                if (result.Success && File.Exists(outputPath))
                {
                    return outputPath;
                }
                else
                {
                    // 如果失败，使用.NET内置方法创建缩略图
                    return CreateThumbnailWithDotNet(imagePath, width, height);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建缩略图异常: {ex.Message}");
                
                // 尝试使用.NET内置方法
                return CreateThumbnailWithDotNet(imagePath, width, height);
            }
        }
        
        /// <summary>
        /// 使用.NET内置方法创建缩略图
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        /// <param name="width">目标宽度</param>
        /// <param name="height">目标高度</param>
        /// <returns>生成的缩略图路径</returns>
        private string CreateThumbnailWithDotNet(string imagePath, int width, int height)
        {
            try
            {
                // 生成唯一的输出文件路径
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string outputPath = Path.Combine(_tempOutputDir, $"{fileName}_thumb_dotnet_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg");
                
                // 加载原图
                using (Image originalImage = Image.FromFile(imagePath))
                {
                    // 计算缩放比例
                    float scale = Math.Min((float)width / originalImage.Width, (float)height / originalImage.Height);
                    int newWidth = (int)(originalImage.Width * scale);
                    int newHeight = (int)(originalImage.Height * scale);
                    
                    // 创建缩略图
                    using (Bitmap thumbnail = new Bitmap(width, height))
                    {
                        using (Graphics g = Graphics.FromImage(thumbnail))
                        {
                            // 填充背景
                            g.Clear(Color.Black);
                            
                            // 居中绘制图像
                            int x = (width - newWidth) / 2;
                            int y = (height - newHeight) / 2;
                            
                            // 设置高质量插值模式
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(originalImage, new Rectangle(x, y, newWidth, newHeight));
                        }
                        
                        // 保存缩略图
                        thumbnail.Save(outputPath, ImageFormat.Jpeg);
                    }
                }
                
                return outputPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"使用.NET创建缩略图异常: {ex.Message}");
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
                // 删除7天前的临时文件
                if (Directory.Exists(_tempOutputDir))
                {
                    foreach (string file in Directory.GetFiles(_tempOutputDir))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.LastAccessTime < DateTime.Now.AddDays(-7))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch
                            {
                                // 忽略删除失败
                            }
                        }
                    }
                }
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
            catch
            {
                // 忽略清理错误
            }
        }
    }
}
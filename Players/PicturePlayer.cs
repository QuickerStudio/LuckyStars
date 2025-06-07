using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using LuckyStars.Players.Settings;

namespace LuckyStars.Players
{
    [SupportedOSPlatform("windows")]
    public class PicturePlayer
    {
        private readonly ImageBrush backgroundImageBrush;

        /// <summary>
        /// 图片播放器设置
        /// </summary>
        public PicturePlayerSettings Settings { get; }

        public PicturePlayer(ImageBrush backgroundImageBrush)
        {
            this.backgroundImageBrush = backgroundImageBrush ?? throw new ArgumentNullException(nameof(backgroundImageBrush));
            Settings = new PicturePlayerSettings();
        }

        /// <summary>
        /// 显示图片壁纸
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>是否成功显示图片</returns>
        public bool ShowPicture(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return false;
            }

            // 加载图片
            var image = LoadImage(imagePath);
            if (image == null)
            {
                return false;
            }

            if (Settings.TransitionEnabled)
            {
                // 使用动画过渡效果
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(Settings.TransitionDuration));
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(Settings.TransitionDuration));
                fadeOut.Completed += (s, a) =>
                {
                    backgroundImageBrush.ImageSource = image;
                    backgroundImageBrush.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };
                backgroundImageBrush.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else
            {
                // 直接设置图片，无动画效果
                backgroundImageBrush.ImageSource = image;
            }

            return true;
        }

        /// <summary>
        /// 直接设置图片（无动画效果，用于节能模式等场景）
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>是否成功设置图片</returns>
        public bool SetPicture(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return false;
            }

            // 加载图片
            var image = LoadImage(imagePath);
            if (image == null)
            {
                return false;
            }

            // 直接设置图片，无动画效果
            backgroundImageBrush.ImageSource = image;
            return true;
        }

        /// <summary>
        /// 清除当前显示的图片
        /// </summary>
        public void ClearPicture()
        {
            backgroundImageBrush.ImageSource = null;
        }

        /// <summary>
        /// 检查图片是否有效
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>图片是否有效</returns>
        public static bool IsValidImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return false;
            }

            try
            {
                using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                return decoder.Frames.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取图片的宽高比
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>宽高比，如果获取失败则返回0</returns>
        public static double GetImageAspectRatio(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return 0;
            }

            var bitmap = LoadImage(imagePath);
            if (bitmap == null)
            {
                return 0;
            }

            return (double)bitmap.PixelWidth / bitmap.PixelHeight;
        }
        /// <summary>
        /// 加载图片并返回 BitmapImage 对象
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>加载成功返回 BitmapImage 对象，失败返回 null</returns>
        [return: MaybeNull]
        private static BitmapImage LoadImage(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}

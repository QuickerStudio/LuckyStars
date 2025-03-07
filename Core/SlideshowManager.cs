using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LuckyStars
{
    public class SlideshowManager
    {
        public List<string> Images { get; private set; }
        public bool IsActive { get; private set; }
        
        private int _intervalSeconds;
        private Stopwatch _stopwatch;
        private int _currentIndex;
        
        public SlideshowManager()
        {
            Images = new List<string>();
            IsActive = false;
            _intervalSeconds = 10;
            _stopwatch = new Stopwatch();
            _currentIndex = -1;
        }
        
        public void Start(List<string> imagePaths, int intervalSeconds = 10)
        {
            if (imagePaths == null || imagePaths.Count == 0)
                return;
                
            // 过滤出有效的图片路径
            Images = imagePaths.Where(path => IsImageFile(path)).ToList();
            
            if (Images.Count == 0)
                return;
                
            _intervalSeconds = Math.Max(1, intervalSeconds);
            _currentIndex = -1;
            IsActive = true;
            _stopwatch.Restart();
        }
        
        public void Stop()
        {
            IsActive = false;
            _stopwatch.Stop();
            Images.Clear();
        }
        
        public bool ShouldChangeImage()
        {
            if (!IsActive || Images.Count == 0)
                return false;
                
            return _stopwatch.Elapsed.TotalSeconds >= _intervalSeconds;
        }
        
        public string GetNextImage()
        {
            if (!IsActive || Images.Count == 0)
                return null;
                
            _currentIndex = (_currentIndex + 1) % Images.Count;
            _stopwatch.Restart();
            
            return Images[_currentIndex];
        }
        
        public string GetCurrentImage()
        {
            if (!IsActive || Images.Count == 0 || _currentIndex < 0 || _currentIndex >= Images.Count)
                return null;
                
            return Images[_currentIndex];
        }
        
        private bool IsImageFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
                
            string extension = System.IO.Path.GetExtension(path).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".bmp";
        }
    }
}
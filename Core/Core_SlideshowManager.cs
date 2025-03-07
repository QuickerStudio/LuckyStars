using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;

namespace LuckyStars.Core
{
    /// <summary>
    /// 幻灯片播放管理器
    /// </summary>
    public class SlideshowManager : IDisposable
    {
        // 幻灯片切换事件
        public event EventHandler<string> WallpaperChanged;
        
        // 播放状态变更事件
        public event EventHandler<bool> PlaybackStateChanged;
        
        // 文件管理器
        private readonly FileManager _fileManager;
        
        // 壁纸管理器
        private readonly WallpaperManager _wallpaperManager;
        
        // 定时器
        private System.Timers.Timer _slideshowTimer;
        
        // 当前播放列表
        private List<FileManager.WallpaperInfo> _playlist;
        
        // 当前播放索引
        private int _currentIndex = -1;
        
        // 切换间隔（分钟）
        private int _interval = Constants.DefaultSlideshowInterval;
        
        // 是否随机播放
        private bool _isRandomMode = false;
        
        // 是否正在播放
        private bool _isPlaying = false;
        
        // 播放模式
        private PlaybackMode _playbackMode = PlaybackMode.Sequential;
        
        // 过滤条件
        private WallpaperFilter _filter = null;
        
        // 随机数生成器
        private Random _random = new Random();
        
        // 待播放索引队列（随机模式使用）
        private Queue<int> _playQueue = new Queue<int>();
        
        // 播放历史
        private Stack<int> _playHistory = new Stack<int>();
        
        // 取消令牌源
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        /// <summary>
        /// 播放模式枚举
        /// </summary>
        public enum PlaybackMode
        {
            /// <summary>
            /// 顺序播放
            /// </summary>
            Sequential,
            
            /// <summary>
            /// 随机播放
            /// </summary>
            Random,
            
            /// <summary>
            /// 单张循环
            /// </summary>
            SingleRepeat
        }
        
        /// <summary>
        /// 壁纸过滤条件
        /// </summary>
        public class WallpaperFilter
        {
            /// <summary>
            /// 壁纸类型过滤
            /// </summary>
            public FileManager.WallpaperType? Type { get; set; }
            
            /// <summary>
            /// 标签过滤
            /// </summary>
            public string Tag { get; set; }
            
            /// <summary>
            /// 关键词过滤
            /// </summary>
            public string Keyword { get; set; }
            
            /// <summary>
            /// 应用过滤条件
            /// </summary>
            /// <param name="wallpapers">壁纸列表</param>
            /// <returns>过滤后的壁纸列表</returns>
            public List<FileManager.WallpaperInfo> Apply(IEnumerable<FileManager.WallpaperInfo> wallpapers)
            {
                var result = wallpapers;
                
                // 应用类型过滤
                if (Type.HasValue)
                {
                    result = result.Where(w => w.Type == Type.Value);
                }
                
                // 应用标签过滤
                if (!string.IsNullOrEmpty(Tag))
                {
                    result = result.Where(w => w.Tags != null && w.Tags.Contains(Tag, StringComparer.OrdinalIgnoreCase));
                }
                
                // 应用关键词过滤
                if (!string.IsNullOrEmpty(Keyword))
                {
                    result = result.Where(w => 
                        (w.Name != null && w.Name.Contains(Keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (w.Tags != null && w.Tags.Any(t => t.Contains(Keyword, StringComparison.OrdinalIgnoreCase))));
                }
                
                return result.ToList();
            }
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fileManager">文件管理器</param>
        /// <param name="wallpaperManager">壁纸管理器</param>
        public SlideshowManager(FileManager fileManager, WallpaperManager wallpaperManager)
        {
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _wallpaperManager = wallpaperManager ?? throw new ArgumentNullException(nameof(wallpaperManager));
            
            // 初始化定时器
            _slideshowTimer = new System.Timers.Timer();
            _slideshowTimer.Elapsed += SlideshowTimer_Elapsed;
            UpdateTimerInterval();
            
            // 初始化播放列表
            RefreshPlaylist();
        }
        
        /// <summary>
        /// 定时器触发事件
        /// </summary>
        private void SlideshowTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // 暂停定时器，避免重复触发
            _slideshowTimer.Stop();
            
            try
            {
                // 切换到下一个壁纸
                if (_playbackMode != PlaybackMode.SingleRepeat)
                {
                    NextWallpaper();
                }
                else
                {
                    // 单张循环模式下重新应用当前壁纸
                    ApplyCurrentWallpaper();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"幻灯片切换出错: {ex.Message}");
            }
            finally
            {
                // 如果仍在播放状态，则重新启动定时器
                if (_isPlaying)
                {
                    _slideshowTimer.Start();
                }
            }
        }
        
        /// <summary>
        /// 刷新播放列表
        /// </summary>
        public void RefreshPlaylist()
        {
            try
            {
                // 获取所有壁纸
                var allWallpapers = _fileManager.GetAllWallpapers();
                
                // 应用过滤条件
                _playlist = _filter != null 
                    ? _filter.Apply(allWallpapers) 
                    : allWallpapers;
                
                // 检查播放列表是否为空
                if (_playlist.Count == 0)
                {
                    Debug.WriteLine("警告：播放列表为空");
                    Stop();
                    return;
                }
                
                // 如果当前索引无效，重置为0
                if (_currentIndex < 0 || _currentIndex >= _playlist.Count)
                {
                    _currentIndex = 0;
                }
                
                // 如果是随机模式，重新生成播放队列
                if (_playbackMode == PlaybackMode.Random)
                {
                    RegenerateRandomQueue();
                }
                
                Debug.WriteLine($"播放列表已刷新，共 {_playlist.Count} 张壁纸");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"刷新播放列表出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置切换间隔
        /// </summary>
        /// <param name="intervalMinutes">间隔分钟数</param>
        public void SetInterval(int intervalMinutes)
        {
            if (intervalMinutes <= 0)
                intervalMinutes = Constants.DefaultSlideshowInterval;
                
            _interval = intervalMinutes;
            UpdateTimerInterval();
            
            Debug.WriteLine($"幻灯片间隔已设置为 {_interval} 分钟");
        }
        
        /// <summary>
        /// 更新定时器间隔
        /// </summary>
        private void UpdateTimerInterval()
        {
            // 将分钟转换为毫秒
            _slideshowTimer.Interval = _interval * 60 * 1000;
        }
        
        /// <summary>
        /// 设置播放模式
        /// </summary>
        /// <param name="mode">播放模式</param>
        public void SetPlaybackMode(PlaybackMode mode)
        {
            _playbackMode = mode;
            
            // 如果设置为随机模式且之前不是随机模式，重新生成随机队列
            if (mode == PlaybackMode.Random && !_isRandomMode)
            {
                _isRandomMode = true;
                RegenerateRandomQueue();
            }
            else if (mode != PlaybackMode.Random)
            {
                _isRandomMode = false;
            }
            
            Debug.WriteLine($"播放模式已设置为 {mode}");
        }
        
        /// <summary>
        /// 设置壁纸过滤条件
        /// </summary>
        /// <param name="filter">过滤条件</param>
        public void SetFilter(WallpaperFilter filter)
        {
            _filter = filter;
            
            // 刷新播放列表
            RefreshPlaylist();
            
            Debug.WriteLine("已应用壁纸过滤条件");
        }
        
        /// <summary>
        /// 开始幻灯片播放
        /// </summary>
        public void Start()
        {
            // 如果播放列表为空，尝试刷新
            if (_playlist == null || _playlist.Count == 0)
            {
                RefreshPlaylist();
            }
            
            // 检查播放列表是否仍为空
            if (_playlist == null || _playlist.Count == 0)
            {
                Debug.WriteLine("错误：播放列表为空，无法开始播放");
                return;
            }
            
            _isPlaying = true;
            _slideshowTimer.Start();
            
            // 立即应用当前壁纸
            ApplyCurrentWallpaper();
            
            // 触发状态变更事件
            PlaybackStateChanged?.Invoke(this, true);
            
            Debug.WriteLine("幻灯片播放已开始");
        }
        
        /// <summary>
        /// 停止幻灯片播放
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _slideshowTimer.Stop();
            
            // 触发状态变更事件
            PlaybackStateChanged?.Invoke(this, false);
            
            Debug.WriteLine("幻灯片播放已停止");
        }
        
        /// <summary>
        /// 切换到下一个壁纸
        /// </summary>
        public void NextWallpaper()
        {
            if (_playlist == null || _playlist.Count == 0)
            {
                RefreshPlaylist();
                if (_playlist == null || _playlist.Count == 0)
                {
                    Debug.WriteLine("错误：播放列表为空，无法切换下一张");
                    return;
                }
            }
            
            try
            {
                // 保存当前索引到历史
                if (_currentIndex >= 0 && _currentIndex < _playlist.Count)
                {
                    _playHistory.Push(_currentIndex);
                }
                
                // 根据播放模式确定下一个索引
                if (_playbackMode == PlaybackMode.Random)
                {
                    // 随机模式
                    if (_playQueue.Count == 0)
                    {
                        RegenerateRandomQueue();
                    }
                    
                    _currentIndex = _playQueue.Dequeue();
                }
                else
                {
                    // 顺序模式
                    _currentIndex = (_currentIndex + 1) % _playlist.Count;
                }
                
                // 应用当前壁纸
                ApplyCurrentWallpaper();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"切换到下一个壁纸出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 切换到上一个壁纸
        /// </summary>
        public void PreviousWallpaper()
        {
            if (_playlist == null || _playlist.Count == 0)
            {
                RefreshPlaylist();
                if (_playlist == null || _playlist.Count == 0)
                {
                    Debug.WriteLine("错误：播放列表为空，无法切换上一张");
                    return;
                }
            }
            
            try
            {
                // 检查是否有播放历史
                if (_playHistory.Count > 0)
                {
                    _currentIndex = _playHistory.Pop();
                }
                else if (_playbackMode == PlaybackMode.Random)
                {
                    // 随机模式下没有历史记录，生成新的随机索引
                    _currentIndex = _random.Next(_playlist.Count);
                }
                else
                {
                    // 顺序模式下回到上一张
                    _currentIndex = (_currentIndex - 1 + _playlist.Count) % _playlist.Count;
                }
                
                // 应用当前壁纸
                ApplyCurrentWallpaper();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"切换到上一个壁纸出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 切换到指定壁纸
        /// </summary>
        /// <param name="index">壁纸索引</param>
        public void JumpToWallpaper(int index)
        {
            if (_playlist == null || _playlist.Count == 0)
            {
                RefreshPlaylist();
                if (_playlist == null || _playlist.Count == 0)
                {
                    Debug.WriteLine("错误：播放列表为空，无法切换到指定壁纸");
                    return;
                }
            }
            
            try
            {
                // 保存当前索引到历史
                if (_currentIndex >= 0 && _currentIndex < _playlist.Count)
                {
                    _playHistory.Push(_currentIndex);
                }
                
                // 确保索引在有效范围内
                if (index < 0 || index >= _playlist.Count)
                {
                    index = 0;
                }
                
                _currentIndex = index;
                
                // 应用当前壁纸
                ApplyCurrentWallpaper();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"切换到指定壁纸出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 切换到指定壁纸
        /// </summary>
        /// <param name="wallpaperId">壁纸ID</param>
        public void JumpToWallpaper(string wallpaperId)
        {
            if (_playlist == null || _playlist.Count == 0 || string.IsNullOrEmpty(wallpaperId))
            {
                RefreshPlaylist();
                if (_playlist == null || _playlist.Count == 0)
                {
                    Debug.WriteLine("错误：播放列表为空，无法切换到指定壁纸");
                    return;
                }
            }
            
            try
            {
                // 查找指定壁纸的索引
                int index = _playlist.FindIndex(w => w.Id == wallpaperId);
                
                if (index >= 0)
                {
                    // 保存当前索引到历史
                    if (_currentIndex >= 0 && _currentIndex < _playlist.Count)
                    {
                        _playHistory.Push(_currentIndex);
                    }
                    
                    _currentIndex = index;
                    
                    // 应用当前壁纸
                    ApplyCurrentWallpaper();
                }
                else
                {
                    Debug.WriteLine($"错误：找不到ID为 {wallpaperId} 的壁纸");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"切换到指定壁纸出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 应用当前壁纸
        /// </summary>
        private void ApplyCurrentWallpaper()
        {
            if (_playlist == null || _playlist.Count == 0 || _currentIndex < 0 || _currentIndex >= _playlist.Count)
            {
                Debug.WriteLine("错误：无法应用当前壁纸，索引无效");
                return;
            }
            
            try
            {
                // 获取当前壁纸
                FileManager.WallpaperInfo wallpaper = _playlist[_currentIndex];
                
                if (wallpaper == null || string.IsNullOrEmpty(wallpaper.Path))
                {
                    Debug.WriteLine("错误：当前壁纸路径无效");
                    return;
                }
                
                // 验证壁纸文件是否存在
                if (!_fileManager.ValidateWallpaperFile(wallpaper.Id))
                {
                    Debug.WriteLine($"错误：壁纸文件不存在 [{wallpaper.Path}]");
                    
                    // 从播放列表中移除无效壁纸
                    _playlist.RemoveAt(_currentIndex);
                    
                    // 调整当前索引
                    if (_playlist.Count > 0)
                    {
                        _currentIndex = _currentIndex % _playlist.Count;
                        // 递归调用以应用有效的壁纸
                        ApplyCurrentWallpaper();
                    }
                    return;
                }
                
                // 使用壁纸管理器应用壁纸
                _wallpaperManager.SetWallpaper(wallpaper.Path);
                
                // 触发壁纸变更事件
                WallpaperChanged?.Invoke(this, wallpaper.Path);
                
                Debug.WriteLine($"已应用壁纸: {wallpaper.Name} [{wallpaper.Path}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"应用当前壁纸出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 重新生成随机播放队列
        /// </summary>
        private void RegenerateRandomQueue()
        {
            if (_playlist == null || _playlist.Count == 0)
                return;
                
            try
            {
                // 创建索引列表
                List<int> indices = Enumerable.Range(0, _playlist.Count).ToList();
                
                // 如果当前有播放中的壁纸，移除它，避免立即重复播放
                if (_currentIndex >= 0 && _currentIndex < indices.Count)
                {
                    indices.Remove(_currentIndex);
                }
                
                // 打乱索引列表
                ShuffleList(indices);
                
                // 清空并重建播放队列
                _playQueue.Clear();
                foreach (int index in indices)
                {
                    _playQueue.Enqueue(index);
                }
                
                Debug.WriteLine($"已重新生成随机播放队列，共 {_playQueue.Count} 张壁纸");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"重新生成随机播放队列出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 打乱列表
        /// </summary>
        /// <typeparam name="T">列表元素类型</typeparam>
        /// <param name="list">待打乱的列表</param>
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        
        /// <summary>
        /// 获取当前播放状态
        /// </summary>
        /// <returns>是否正在播放</returns>
        public bool IsPlaying()
        {
            return _isPlaying;
        }
        
        /// <summary>
        /// 获取当前播放模式
        /// </summary>
        /// <returns>播放模式</returns>
        public PlaybackMode GetPlaybackMode()
        {
            return _playbackMode;
        }
        
        /// <summary>
        /// 获取当前播放间隔（分钟）
        /// </summary>
        /// <returns>播放间隔</returns>
        public int GetInterval()
        {
            return _interval;
        }
        
        /// <summary>
        /// 获取播放列表
        /// </summary>
        /// <returns>播放列表</returns>
        public List<FileManager.WallpaperInfo> GetPlaylist()
        {
            return _playlist?.ToList() ?? new List<FileManager.WallpaperInfo>();
        }
        
        /// <summary>
        /// 获取当前壁纸
        /// </summary>
        /// <returns>当前壁纸信息</returns>
        public FileManager.WallpaperInfo GetCurrentWallpaper()
        {
            if (_playlist != null && _currentIndex >= 0 && _currentIndex < _playlist.Count)
            {
                return _playlist[_currentIndex];
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取当前播放索引
        /// </summary>
        /// <returns>当前索引</returns>
        public int GetCurrentIndex()
        {
            return _currentIndex;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 取消后台任务
                _cts.Cancel();
                _cts.Dispose();
                
                // 停止定时器
                if (_slideshowTimer != null)
                {
                    _slideshowTimer.Stop();
                    _slideshowTimer.Elapsed -= SlideshowTimer_Elapsed;
                    _slideshowTimer.Dispose();
                    _slideshowTimer = null;
                }
                
                // 清理资源
                _playlist?.Clear();
                _playQueue?.Clear();
                _playHistory?.Clear();
                
                _isPlaying = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放幻灯片管理器资源失败: {ex.Message}");
            }
        }
    }
}
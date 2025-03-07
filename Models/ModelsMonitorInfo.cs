using System;
using System.Drawing;

namespace LuckyStars.Models
{
    /// <summary>
    /// 监视器信息模型，存储显示器相关信息
    /// </summary>
    public class MonitorInfo
    {
        /// <summary>
        /// 监视器唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 监视器设备名称（如：\\.\DISPLAY1）
        /// </summary>
        public string DeviceName { get; set; }
        
        /// <summary>
        /// 监视器友好名称（如：戴尔 P2419H）
        /// </summary>
        public string FriendlyName { get; set; }
        
        /// <summary>
        /// 监视器句柄
        /// </summary>
        public IntPtr MonitorHandle { get; set; } = IntPtr.Zero;
        
        /// <summary>
        /// 监视器区域
        /// </summary>
        public Rectangle Bounds { get; set; }
        
        /// <summary>
        /// 工作区域（排除任务栏等）
        /// </summary>
        public Rectangle WorkingArea { get; set; }
        
        /// <summary>
        /// 是否为主显示器
        /// </summary>
        public bool IsPrimary { get; set; }
        
        /// <summary>
        /// 显示器索引
        /// </summary>
        public int Index { get; set; }
        
        /// <summary>
        /// DPI缩放比例
        /// </summary>
        public float DpiScale { get; set; } = 1.0f;
        
        /// <summary>
        /// 水平DPI
        /// </summary>
        public uint DpiX { get; set; } = 96;
        
        /// <summary>
        /// 垂直DPI
        /// </summary>
        public uint DpiY { get; set; } = 96;
        
        /// <summary>
        /// 刷新率（赫兹）
        /// </summary>
        public int RefreshRate { get; set; } = 60;
        
        /// <summary>
        /// 色深（位）
        /// </summary>
        public int ColorDepth { get; set; } = 32;
        
        /// <summary>
        /// 水平分辨率
        /// </summary>
        public int HorizontalResolution => Bounds.Width;
        
        /// <summary>
        /// 垂直分辨率
        /// </summary>
        public int VerticalResolution => Bounds.Height;
        
        /// <summary>
        /// 纵横比
        /// </summary>
        public float AspectRatio => (float)HorizontalResolution / VerticalResolution;
        
        /// <summary>
        /// 显示器方向（0=正常，1=90度，2=180度，3=270度）
        /// </summary>
        public int Orientation { get; set; } = 0;
        
        /// <summary>
        /// 当前壁纸路径
        /// </summary>
        public string WallpaperPath { get; set; }
        
        /// <summary>
        /// 壁纸缩放模式
        /// </summary>
        public Core.MultiMonitorWallpaperManager.WallpaperScaleMode ScaleMode { get; set; } = Core.MultiMonitorWallpaperManager.WallpaperScaleMode.Fill;
        
        /// <summary>
        /// 是否启用此显示器的壁纸
        /// </summary>
        public bool WallpaperEnabled { get; set; } = true;
        
        /// <summary>
        /// 显示器EDID（扩展显示标识数据）
        /// </summary>
        public string EDID { get; set; }
        
        /// <summary>
        /// 显示器序列号
        /// </summary>
        public string SerialNumber { get; set; }
        
        /// <summary>
        /// 显示器生产厂商
        /// </summary>
        public string Manufacturer { get; set; }
        
        /// <summary>
        /// 显示器型号
        /// </summary>
        public string Model { get; set; }
        
        /// <summary>
        /// 与主显示器的相对位置（上、右、下、左）
        /// </summary>
        public string RelativePosition { get; set; }
        
        /// <summary>
        /// 壁纸缩放偏移X（百分比，-100到100）
        /// </summary>
        public int WallpaperOffsetX { get; set; } = 0;
        
        /// <summary>
        /// 壁纸缩放偏移Y（百分比，-100到100）
        /// </summary>
        public int WallpaperOffsetY { get; set; } = 0;
        
        /// <summary>
        /// 显示器连接类型（HDMI、DisplayPort、DVI等）
        /// </summary>
        public string ConnectionType { get; set; }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public MonitorInfo()
        {
        }
        
        /// <summary>
        /// 使用基本信息初始化监视器
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="bounds">区域</param>
        /// <param name="isPrimary">是否为主显示器</param>
        /// <param name="index">索引</param>
        public MonitorInfo(string deviceName, Rectangle bounds, bool isPrimary, int index)
        {
            DeviceName = deviceName;
            Bounds = bounds;
            WorkingArea = bounds; // 默认与bounds相同，后续可能更新
            IsPrimary = isPrimary;
            Index = index;
            
            // 生成友好名称
            FriendlyName = $"显示器 {index + 1}" + (isPrimary ? " (主)" : "");
        }
        
        /// <summary>
        /// 获取显示器中心点
        /// </summary>
        /// <returns>中心点坐标</returns>
        public Point GetCenterPoint()
        {
            return new Point(
                Bounds.X + (Bounds.Width / 2),
                Bounds.Y + (Bounds.Height / 2)
            );
        }
        
        /// <summary>
        /// 检查点是否在此监视器区域内
        /// </summary>
        /// <param name="point">坐标点</param>
        /// <returns>是否在区域内</returns>
        public bool ContainsPoint(Point point)
        {
            return Bounds.Contains(point);
        }
        
        /// <summary>
        /// 获取此监视器相对于另一监视器的位置
        /// </summary>
        /// <param name="otherMonitor">另一监视器</param>
        /// <returns>相对位置描述（上、下、左、右、相同）</returns>
        public string GetRelativePositionTo(MonitorInfo otherMonitor)
        {
            if (otherMonitor == null)
                return "未知";
                
            Point thisCenter = this.GetCenterPoint();
            Point otherCenter = otherMonitor.GetCenterPoint();
            
            // 计算中心点距离的主要方向
            int dx = thisCenter.X - otherCenter.X;
            int dy = thisCenter.Y - otherCenter.Y;
            
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                // 水平方向更显著
                return dx > 0 ? "右侧" : "左侧";
            }
            else if (Math.Abs(dy) > 0)
            {
                // 垂直方向更显著
                return dy > 0 ? "下方" : "上方";
            }
            
            return "相同位置";
        }
        
        /// <summary>
        /// 检测是否为4K或更高分辨率
        /// </summary>
        /// <returns>是否为高分辨率</returns>
        public bool Is4KOrHigher()
        {
            return HorizontalResolution >= 3840 || VerticalResolution >= 2160;
        }
        
        /// <summary>
        /// 计算显示器面积（平方像素）
        /// </summary>
        /// <returns>显示器面积</returns>
        public long GetScreenArea()
        {
            return (long)Bounds.Width * Bounds.Height;
        }
        
        /// <summary>
        /// 应用特定于此显示器的壁纸设置
        /// </summary>
        /// <param name="path">壁纸路径</param>
        /// <param name="scaleMode">壁纸缩放模式</param>
        public void ApplyWallpaperSettings(string path, Core.MultiMonitorWallpaperManager.WallpaperScaleMode scaleMode)
        {
            WallpaperPath = path;
            ScaleMode = scaleMode;
        }
        
        /// <summary>
        /// 设置显示器的DPI信息
        /// </summary>
        /// <param name="dpiX">水平DPI</param>
        /// <param name="dpiY">垂直DPI</param>
        public void SetDpiInfo(uint dpiX, uint dpiY)
        {
            this.DpiX = dpiX;
            this.DpiY = dpiY;
            this.DpiScale = dpiX / 96.0f;
        }
        
        /// <summary>
        /// 获取显示器描述性字符串
        /// </summary>
        /// <returns>显示器描述</returns>
        public override string ToString()
        {
            return $"{FriendlyName} ({HorizontalResolution}x{VerticalResolution} @ {RefreshRate}Hz)";
        }
        
        /// <summary>
        /// 从另一MonitorInfo对象复制属性
        /// </summary>
        /// <param name="other">源MonitorInfo对象</param>
        public void CopyFrom(MonitorInfo other)
        {
            if (other == null)
                return;
                
            // 不复制Id保持唯一性
            DeviceName = other.DeviceName;
            FriendlyName = other.FriendlyName;
            MonitorHandle = other.MonitorHandle;
            Bounds = other.Bounds;
            WorkingArea = other.WorkingArea;
            IsPrimary = other.IsPrimary;
            Index = other.Index;
            DpiScale = other.DpiScale;
            DpiX = other.DpiX;
            DpiY = other.DpiY;
            RefreshRate = other.RefreshRate;
            ColorDepth = other.ColorDepth;
            Orientation = other.Orientation;
            WallpaperPath = other.WallpaperPath;
            ScaleMode = other.ScaleMode;
            WallpaperEnabled = other.WallpaperEnabled;
            EDID = other.EDID;
            SerialNumber = other.SerialNumber;
            Manufacturer = other.Manufacturer;
            Model = other.Model;
            RelativePosition = other.RelativePosition;
            WallpaperOffsetX = other.WallpaperOffsetX;
            WallpaperOffsetY = other.WallpaperOffsetY;
            ConnectionType = other.ConnectionType;
        }
        
        /// <summary>
        /// 计算与另一监视器的相对位置权重
        /// 用于确定壁纸延伸时的顺序
        /// </summary>
        /// <param name="primary">主监视器</param>
        /// <returns>相对位置权重</returns>
        public int CalculatePositionalWeight(MonitorInfo primary)
        {
            if (primary == null || this.IsPrimary)
                return 0;
                
            // 获取到主显示器的距离
            Point thisCenter = this.GetCenterPoint();
            Point primaryCenter = primary.GetCenterPoint();
            
            int dx = thisCenter.X - primaryCenter.X;
            int dy = thisCenter.Y - primaryCenter.Y;
            
            // 计算权重 - 优先横向，再纵向
            return Math.Abs(dx) * 1000 + Math.Abs(dy);
        }
    }
}
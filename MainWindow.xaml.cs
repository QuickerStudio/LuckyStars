using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
namespace LuckyStars
{
    /// <summary>
    /// 壁纸主窗口，包含鼠标跨层通信与 WebView2 初始化等逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 原有字段...
        private System.Timers.Timer? timer;
        private FileSystemWatcher? folderWatcher;
        private System.Timers.Timer? idleTimer;
        private readonly List<string> imagePaths = new();
        private bool _isWebViewInitialized = false;
        private int currentIndex;
        private readonly string targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "LuckyStarsWallpaper");
        private readonly double[] allowedRatios = new[]
        {
            21.0 / 9.0,
            16.0 / 9.0,
            6.0 / 4.0,
            4.0 / 3.0
        };
        private const int IdleTimeout = 60000;
        private bool pendingRefresh = false;
        private const string RegistryKey = "SOFTWARE\\LuckyStars";
        private const string TimerIntervalValueName = "TimerInterval";

        public enum TimerState
        {
            FiveMinutes = 300000,
            TenMinutes = 600000,
            TwentyMinutes = 1200000,
            Disabled = 0
        }

        // 以下为新增字段：用于跨层鼠标坐标通信
        private HwndSource? hwndSource;
        private System.Timers.Timer? mousePositionTimer;
        private IntPtr _hwnd = IntPtr.Zero;

        public MainWindow()
        {
            InitializeComponent();

            // 原有初始化流程
            EnsureDirectoryExists(targetFolder);
            SetupFolderWatcher();
            InitializeIdleTimer();
            LoadImagePaths();
            int timerInterval = LoadTimerIntervalFromRegistry();
            SetupTimer(timerInterval);
            ShowImage();
            InitializeWebView();

            // 此时 OnSourceInitialized 中会进一步获取窗口句柄并初始化钩子/定时器
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 在 WPF 窗口创建完成后再获取 hwnd (避免 hwnd 为 0 的问题)
            WindowInteropHelper helper = new WindowInteropHelper(this);
            _hwnd = helper.Handle;
            if (_hwnd == IntPtr.Zero)
            {
                MessageBox.Show("窗口句柄获取失败，hwnd 仍然为 0。");
                return;
            }

            // 句柄有效后，开始跨层鼠标坐标通信：启动定时器并注册窗口消息钩子
            StartMouseCoordSender();
        }

        private static void EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static int LoadTimerIntervalFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (key != null)
                {
                    var value = key.GetValue(TimerIntervalValueName);
                    if (value != null && int.TryParse(value.ToString(), out int interval))
                    {
                        return interval;
                    }
                }
            }
            catch (Exception) { }
            return (int)TimerState.FiveMinutes;
        }

        public static void SaveTimerIntervalToRegistry(int interval)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
                key?.SetValue(TimerIntervalValueName, interval);
            }
            catch (Exception) { }
        }

        public void SetTimerInterval(int interval)
        {
            SaveTimerIntervalToRegistry(interval);
            SetupTimer(interval);
        }

        private void SetupTimer(int interval)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= OnTimedEvent;
                timer.Dispose();
                timer = null;
            }
            if (interval == 0)
            {
                return;
            }
            timer = new System.Timers.Timer(interval)
            {
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += OnTimedEvent;
        }

        public TimerState CycleTimerState()
        {
            var currentState = GetCurrentTimerState();
            var nextState = currentState switch
            {
                TimerState.FiveMinutes => TimerState.TenMinutes,
                TimerState.TenMinutes => TimerState.TwentyMinutes,
                TimerState.TwentyMinutes => TimerState.Disabled,
                TimerState.Disabled => TimerState.FiveMinutes,
                _ => TimerState.FiveMinutes
            };

            SetTimerInterval((int)nextState);
            return nextState;
        }

        private static TimerState GetCurrentTimerState()
        {
            int interval = LoadTimerIntervalFromRegistry();
            if (interval == (int)TimerState.FiveMinutes) return TimerState.FiveMinutes;
            if (interval == (int)TimerState.TenMinutes) return TimerState.TenMinutes;
            if (interval == (int)TimerState.TwentyMinutes) return TimerState.TwentyMinutes;
            if (interval == (int)TimerState.Disabled) return TimerState.Disabled;
            return TimerState.FiveMinutes;
        }

        private void InitializeIdleTimer()
        {
            idleTimer = new System.Timers.Timer(IdleTimeout)
            {
                AutoReset = false
            };
            idleTimer.Elapsed += OnIdleTimeout;
        }

        private void ResetIdleTimer()
        {
            if (idleTimer != null)
            {
                idleTimer.Stop();
                idleTimer.Start();
            }
            pendingRefresh = true;
        }

        private void OnIdleTimeout(object? sender, ElapsedEventArgs e)
        {
            if (pendingRefresh)
            {
                Dispatcher.Invoke(() =>
                {
                    LoadImagePaths();
                    pendingRefresh = false;
                });
            }
        }

        private void SetupFolderWatcher()
        {
            try
            {
                folderWatcher = new FileSystemWatcher(targetFolder)
                {
                    Filter = "*.*",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = false
                };

                folderWatcher.Created += OnFolderChanged;
                folderWatcher.Deleted += OnFolderChanged;
                folderWatcher.Renamed += OnFolderRenamed;
            }
            catch (Exception)
            {
                folderWatcher = null;
            }
        }

        private void OnFolderChanged(object sender, FileSystemEventArgs e)
        {
            string extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
            string[] supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };
            if (supportedExtensions.Contains(extension))
            {
                ResetIdleTimer();
            }
        }

        private void OnFolderRenamed(object sender, RenamedEventArgs e)
        {
            string extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
            string[] supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };
            if (supportedExtensions.Contains(extension))
            {
                ResetIdleTimer();
            }
        }

        private void LoadImagePaths()
        {
            try
            {
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
                imagePaths.Clear();
                var extensions = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.tiff" };
                foreach (var ext in extensions)
                {
                    var files = Directory.GetFiles(targetFolder, ext);
                    foreach (var file in files)
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(file, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            double ratio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                            double tolerance = 0.01;
                            if (allowedRatios.Any(ar => Math.Abs(ar - ratio) < tolerance))
                            {
                                imagePaths.Add(file);
                            }
                        }
                        catch (Exception) { }
                    }
                }
                var sortedImagePaths = imagePaths.OrderBy(path => path).ToList();
                imagePaths.Clear();
                imagePaths.AddRange(sortedImagePaths);
            }
            catch (Exception) { }
        }

        private void OnTimedEvent(object? source, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(ShowImage);
        }

        private void ShowImage()
        {
            if (imagePaths.Count == 0)
            {
                return;
            }
            try
            {
                var imageUri = new Uri(imagePaths[currentIndex], UriKind.Absolute);
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = imageUri;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                fadeOut.Completed += (s, a) =>
                {
                    BackgroundImageBrush.ImageSource = image;
                    BackgroundImageBrush.BeginAnimation(OpacityProperty, fadeIn);
                };
                BackgroundImageBrush.BeginAnimation(OpacityProperty, fadeOut);
                currentIndex = (currentIndex + 1) % imagePaths.Count;
            }
            catch (Exception)
            {
                if (imagePaths.Count > 1)
                {
                    currentIndex = (currentIndex + 1) % imagePaths.Count;
                    ShowImage();
                }
            }
        }

        public void RefreshImageList()
        {
            LoadImagePaths();
        }

        public void NextImage()
        {
            ShowImage();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (folderWatcher != null)
            {
                folderWatcher.EnableRaisingEvents = false;
                folderWatcher.Created -= OnFolderChanged;
                folderWatcher.Deleted -= OnFolderChanged;
                folderWatcher.Renamed -= OnFolderRenamed;
                folderWatcher.Dispose();
                folderWatcher = null;
            }
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
            if (idleTimer != null)
            {
                idleTimer.Stop();
                idleTimer.Dispose();
                idleTimer = null;
            }
            StopMouseCoordSender();
        }

        /// <summary>
        /// 跨层通信需要的 Win32 导入
        /// </summary>
        public static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;
            }

            [DllImport("user32.dll")]
            public static extern bool GetCursorPos(out POINT lpPoint);

            // 自定义消息ID，用于发送鼠标坐标（WM_USER+100）
            public const int WM_USER_MOUSE = 0x0400 + 100;

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        }

        /// <summary>
        /// 初始化 WebView2 并指定 UserDataFolder，同时禁用默认下载事件。
        /// </summary>
        private async void InitializeWebView()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LuckyStarsWebViewData"
                );
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                // 启用JS和开发者工具
                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = true;

                webView.CoreWebView2.DownloadStarting += (sender, args) =>
                {
                    args.Cancel = true;
                };
                webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                _isWebViewInitialized = true;

                // 添加控制台消息处理，方便调试
                webView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[WebView2 控制台] {e.Source}: {e.TryGetWebMessageAsString}");
                };

                LoadTestHtml();
                ApplyDpiScale();
                ApplyFullScreenToWebView();
                await Task.Delay(500);
                webView.CoreWebView2?.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载测试 HTML，并包含接收鼠标坐标的 JS 逻辑
        /// </summary>
        public void LoadTestHtml()
        {
            if (!_isWebViewInitialized || webView.CoreWebView2 == null) return;
            // 新增HTML，提供updateMousePosition函数
            string html = @"
<!DOCTYPE html>
<html lang=""zh"">
<head>
    <meta charset=""UTF-8"">
    <title>LuckyStars - 互动壁纸</title>
    <style>
        body {
            margin: 0;
            padding: 0;
            overflow: hidden;
            background-color: transparent;
            width: 100vw;
            height: 100vh;
        }
        #debug {
            position: fixed;
            top: 10px;
            left: 10px;
            background: rgba(0,0,0,0.5);
            color: white;
            padding: 5px;
            font-family: monospace;
            z-index: 1000;
        }
        #cursor {
            position: absolute;
            width: 20px;
            height: 20px;
            background-color: red;
            border-radius: 50%;
            transform: translate(-50%, -50%);
            pointer-events: none; /* 确保元素不会捕获鼠标事件 */
            z-index: 999;
        }
        /* 画布样式设置 */
        canvas {
            position: fixed;  // 固定定位
            top: 0;          // 顶部对齐
            left: 0;         // 左侧对齐
            z-index: -1;     // 置于底层
        }
    </style>
</head>
<body>
    <!-- 调试信息 <div id=""debug"">鼠标位置: X=0, Y=0</div>显示区 -->
   
    
    <!-- 可视化鼠标 <div id=""cursor""></div>指示器 -->
   

    <canvas id=""particleCanvas""></canvas>

    <script>
        // 调试元素
        const debugElement = document.getElementById('debug');
        const cursorElement = document.getElementById('cursor');
        
        // 接收从C#发送的鼠标坐标的函数
        function updateMousePosition(x, y) {
            // 更新调试显示
          //  debugElement.textContent = `鼠标位置: X=${x}, Y=${y}`;
            
            // 移动视觉指示器
           // cursorElement.style.left = x + 'px';
           // cursorElement.style.top = y + 'px';
            
            // 更新鼠标交互参数
            mouse.x = x;
            mouse.y = y;
        }

        // 初始化
        document.addEventListener('DOMContentLoaded', () => {
            console.log('互动壁纸已加载，等待鼠标坐标...');
        });

        // 参数变量集中管理
        const config = {
            particleSize: { min: 1, max: 4 }, // 粒子尺寸范围
            particleSpeed: { min: -1, max: 1 }, // 粒子速度范围
            mouseMaxDist: 200, // 鼠标影响最大距离
            maxLineDistColor: 10000, // 彩色粒子最大连线距离
            maxLineDistDot: 6000, // 点状粒子最大连线距离
            collisionDuration: 10, // 碰撞状态持续时间（帧数）
            glowRadiusFactor: 4.8, // 动态光晕半径因子
            glowRadiusOffset: 1.2, // 动态光晕半径偏移
            glowHueSpeed: 0.3, // 光晕色相变化速度
            mouseLineWidthFactor: 2, // 鼠标连线宽度因子
            mouseLineOpacityFactor: 0.5, // 鼠标连线透明度因子
            mouseLineLengthFactor: 8, // 鼠标连线长度因子
            randomColorChangeSpeed: 0.05 // 光晕颜色随机变化速度
        };

        // 初始化画布
        const canvas = document.getElementById('particleCanvas');
        const ctx = canvas.getContext('2d');
        canvas.width = window.innerWidth;   // 画布宽度等于窗口宽度
        canvas.height = window.innerHeight; // 画布高度等于窗口高度

        // 鼠标交互参数
        const mouse = { 
            x: null,         // 鼠标X坐标
            y: null,         // 鼠标Y坐标
            maxDist: config.mouseMaxDist     // 鼠标影响最大距离
        };

        // ================= 彩色粒子系统 =================
        class ColorParticle {
            constructor(x, y) {
                // 粒子参数
                this.x = x;                 // X坐标
                this.y = y;                 // Y坐标
                this.size = Math.random() * (config.particleSize.max - config.particleSize.min) + config.particleSize.min; // 粒子尺寸
                this.speedX = Math.random() * (config.particleSpeed.max - config.particleSpeed.min) + config.particleSpeed.min; // X轴速度
                this.speedY = Math.random() * (config.particleSpeed.max - config.particleSpeed.min) + config.particleSpeed.min; // Y轴速度
                this.color = `hsl(${Math.random() * 360}, 70%, 50%)`; // 随机HSL颜色
                this.collisionColor = null; // 碰撞时颜色状态
                this.collisionTimer = 0;    // 碰撞状态持续时间（帧数）
                this.isConnectedToMouse = false; // 是否与鼠标连线连接
                this.originalColor = this.color; // 初始颜色
            }

            draw() {
                ctx.fillStyle = this.color;
                ctx.beginPath();
                this.drawStar(ctx, this.x, this.y, this.size, this.size * 2, 5);
                ctx.fill();
            }

            drawStar(ctx, x, y, radius1, radius2, points) {
                let angle = Math.PI / points;
                ctx.beginPath();
                for (let i = 0; i < 2 * points; i++) {
                    let radius = i % 2 === 0 ? radius2 : radius1;
                    ctx.lineTo(x + Math.cos(i * angle) * radius, y + Math.sin(i * angle) * radius);
                }
                ctx.closePath();
            }

            update() {
                // 鼠标引力影响
                if (mouse.x && mouse.y) {
                    const dx = mouse.x - this.x;
                    const dy = mouse.y - this.y;
                    const distance = Math.sqrt(dx * dx + dy * dy);
                    if (distance < mouse.maxDist) {
                        this.speedX += dx * 0.0005; // X轴加速度
                        this.speedY += dy * 0.0005; // Y轴加速度
                    }
                }

                // 位置更新
                this.x += this.speedX;
                this.y += this.speedY;
                
                // 边界反弹处理
                if (this.x > canvas.width - this.size) {
                    this.speedX *= -0.8; // X轴速度衰减
                    this.x = canvas.width - this.size;
                } else if (this.x < this.size) {
                    this.speedX *= -0.8;
                    this.x = this.size;
                }

                if (this.y > canvas.height - this.size) {
                    this.speedY *= -0.8; // Y轴速度衰减
                    this.y = canvas.height - this.size;
                } else if (this.y < this.size) {
                    this.speedY *= -0.8;
                    this.y = this.size;
                }

                // 碰撞状态更新
                if (this.collisionTimer > 0) {
                    this.collisionTimer--;
                    if (this.collisionTimer === 0) {
                        this.collisionColor = null; // 重置碰撞颜色
                    }
                }

                // 光晕颜色更新
                if (this.isConnectedToMouse) {
                    this.color = `hsl(${Math.random() * 360}, 70%, 50%)`; // 随机HSL颜色
                } else {
                    this.color = this.originalColor; // 保持断开后的颜色
                }
            }
        }

        // ================= 点状粒子系统 =================
        class DotParticle {
            constructor() {
                this.x = Math.random() * canvas.width;  // 初始X坐标
                this.y = Math.random() * canvas.height; // 初始Y坐标
                this.xa = Math.random() * (config.particleSpeed.max - config.particleSpeed.min) + config.particleSpeed.min; // X轴加速度
                this.ya = Math.random() * (config.particleSpeed.max - config.particleSpeed.min) + config.particleSpeed.min; // Y轴加速度
                this.color = `hsl(${Math.random() * 360}, 80%, 60%)`; // 颜色设置
                this.maxDist = config.maxLineDistDot;        // 鼠标影响距离阈值（平方值）
                this.collisionColor = null; // 碰撞颜色状态
                this.collisionTimer = 0;    // 碰撞状态计时
                this.isConnectedToMouse = false; // 是否与鼠标连线连接
                this.originalColor = this.color; // 初始颜色
            }

            update() {
                // 鼠标引力影响
                if (mouse.x && mouse.y) {
                    const dx = mouse.x - this.x;
                    const dy = mouse.y - this.y;
                    const distance = dx * dx + dy * dy;
                    if (distance < this.maxDist) {
                        this.xa += dx * 0.0002; // X轴加速度
                        this.ya += dy * 0.0002; // Y轴加速度
                    }
                }

                // 位置更新
                this.x += this.xa;
                this.y += this.ya;
                
                // 边界反弹处理
                this.xa *= (this.x > canvas.width || this.x < 0) ? -0.8 : 1;
                this.ya *= (this.y > canvas.height || this.y < 0) ? -0.8 : 1;

                // 碰撞状态更新
                if (this.collisionTimer > 0) {
                    this.collisionTimer--;
                    if (this.collisionTimer === 0) {
                        this.collisionColor = null;
                    }
                }

                // 光晕颜色更新
                if (this.isConnectedToMouse) {
                    this.color = `hsl(${Math.random() * 360}, 80%, 60%)`; // 随机HSL颜色
                } else {
                    this.color = this.originalColor; // 保持断开后的颜色
                }
            }
        }

        // ================= 系统初始化 =================
        const colorParticles = Array.from({ length: 150 }, () => // 150个彩色粒子
            new ColorParticle(Math.random() * canvas.width, Math.random() * canvas.height)
        );

        const dotParticles = Array.from({ length: 99 }, () => // 99个点状粒子
            new DotParticle()
        );

        // ================= 连线绘制系统 =================
        function drawLines(particles, maxDistance) {
            // 碰撞检测系统
            const collisionPairs = new Set();
            const checkDistance = Math.sqrt(maxDistance) * 0.8; // 预计算检测距离

            // 粒子间碰撞检测
            for (let i = 0; i < particles.length; i++) {
                for (let j = i + 1; j < particles.length; j++) {
                    const a = particles[i];
                    const b = particles[j];
                    const dx = a.x - b.x;
                    const dy = a.y - b.y;
                    const distanceSq = dx * dx + dy * dy;

                    if (distanceSq < (checkDistance * checkDistance)) {
                        const realDistance = Math.sqrt(distanceSq);
                        const minDist = (a.size || 1) + (b.size || 1); // 最小碰撞距离
                        
                        if (realDistance < minDist) {
                            collisionPairs.add([a, b]);
                            // 设置碰撞颜色（90%透明度）
                            a.collisionColor = `hsla(${Math.random() * 360}, 70%, 50%, 0.9)`;
                            b.collisionColor = a.collisionColor;
                            a.collisionTimer = config.collisionDuration; // 维持10帧
                            b.collisionTimer = config.collisionDuration;
                        }
                    }
                }
            }

            // 连线绘制逻辑
            particles.forEach(a => {
                particles.forEach(b => {
                    if (a === b) return;

                    const dx = a.x - b.x;
                    const dy = a.y - b.y;
                    const distance = dx * dx + dy * dy;

                    if (distance < maxDistance) {
                        ctx.beginPath();
                        // 连线颜色优先使用碰撞颜色
                        const lineColor = a.collisionColor || 
                            `hsla(${a.color.split('hsl(')[1].split(')')[0]}, 0.2)`;
                        
                        ctx.strokeStyle = lineColor;
                        ctx.lineWidth = 1 - (Math.sqrt(distance) / Math.sqrt(maxDistance));
                        
                        ctx.moveTo(a.x, a.y);
                        ctx.lineTo(b.x, b.y);
                        ctx.stroke();
                    }
                });

                // 鼠标连线处理
                if (mouse.x && mouse.y) {
                    const dx = a.x - mouse.x;
                    const dy = a.y - mouse.y;
                    const distance = dx * dx + dy * dy;
                    if (distance < maxDistance * config.mouseLineLengthFactor) { // 鼠标连线长度增加8倍
                        ctx.beginPath();
                        ctx.strokeStyle = `hsla(${a.color.split('hsl(')[1].split(')')[0]}, ${config.mouseLineOpacityFactor})`; // 鼠标连线透明度提高30%
                        ctx.lineWidth = config.mouseLineWidthFactor - (Math.sqrt(distance) / Math.sqrt(maxDistance)); // 鼠标连线宽度增加2倍
                        ctx.moveTo(a.x, a.y);
                        ctx.lineTo(mouse.x, mouse.y);
                        ctx.stroke();

                        // 更新粒子连接状态
                        a.isConnectedToMouse = true;
                    } else {
                        // 断开连接后保持颜色
                        a.isConnectedToMouse = false;
                    }
                } else {
                    // 断开连接后保持颜色
                    a.isConnectedToMouse = false;
                }
            });
        }

        // ================= 动画循环系统 =================
        function animate() {
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            // 更新彩色粒子系统
            colorParticles.forEach(p => {
                p.update();
                p.draw();
            });
            drawLines(colorParticles, config.maxLineDistColor); // 最大连线距离10000（平方值）

            // 更新点状粒子系统
            dotParticles.forEach(p => {
                p.update();
                ctx.fillStyle = p.color;
                ctx.fillRect(p.x - 1, p.y - 1, 2, 2); // 绘制2x2像素点
            });
            drawLines(dotParticles, config.maxLineDistDot); // 最大连线距离6000（平方值）

            // ================= 光效系统 ================= 
            let hue = 0; // 全局色相控制
            function applyGlow() {
                ctx.save();
                ctx.globalCompositeOperation = 'lighter'; // 使用叠加混合模式
                
                colorParticles.forEach(p => {
                    // 动态光晕半径（减少40%）
                    const dynamicRadius = p.size * (config.glowRadiusFactor + Math.sin(Date.now() * 0.0008 + p.x) * config.glowRadiusOffset);
                    
                    // 创建径向渐变
                    const gradient = ctx.createRadialGradient(
                        p.x, p.y, 0, 
                        p.x, p.y, dynamicRadius
                    );
                    gradient.addColorStop(0, `hsla(${(hue + p.x / 10) % 360}, 80%, 60%, 0.3)`);
                    gradient.addColorStop(1, 'transparent');

                    // 绘制光晕
                    ctx.fillStyle = gradient;
                    ctx.beginPath();
                    ctx.arc(p.x, p.y, dynamicRadius, 0, Math.PI * 2);
                    ctx.fill();
                });
                
                ctx.restore();
                hue = (hue + config.glowHueSpeed) % 360; // 色相变化速度
            }

            // 修改后的动画循环
            const originalAnimate = animate;
            animate = function() {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                
                // 更新粒子系统
                colorParticles.forEach(p => {
                    p.update();
                    p.draw();
                });
                drawLines(colorParticles, config.maxLineDistColor);
                
                dotParticles.forEach(p => {
                    p.update();
                    ctx.fillStyle = p.color;
                    ctx.fillRect(p.x - 1, p.y - 1, 2, 2);
                });
                drawLines(dotParticles, config.maxLineDistDot);
                
                applyGlow(); // 应用光效
                requestAnimationFrame(animate);
            };

            requestAnimationFrame(animate);
        }

        // ================= 事件监听系统 =================
        window.addEventListener('mousemove', e => {
            updateMousePosition(e.clientX, e.clientY);
        });

        window.addEventListener('mouseout', () => {
            mouse.x = null;
            mouse.y = null;
        });

        window.addEventListener('resize', () => {
            // 窗口大小调整处理
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;
            // 重置粒子位置
            colorParticles.forEach(p => {
                p.x = Math.random() * canvas.width;
                p.y = Math.random() * canvas.height;
            });
            dotParticles.forEach(p => {
                p.x = Math.random() * canvas.width;
                p.y = Math.random() * canvas.height;
            });
        });

        animate();
    </script>
</body>
</html>";
            try
            {
                webView.CoreWebView2.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载 HTML 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyDpiScale()
        {
            if (!_isWebViewInitialized || webView.CoreWebView2 == null) return;
            var dpiInfo = VisualTreeHelper.GetDpi(this);
            webView.ZoomFactor = dpiInfo.DpiScaleX;
        }

        public void ApplyFullScreenToWebView()
        {
            if (webView == null) return;
            try
            {
                webView.Width = double.NaN;
                webView.Height = double.NaN;
                webView.HorizontalAlignment = HorizontalAlignment.Stretch;
                webView.VerticalAlignment = VerticalAlignment.Stretch;
                if (this.ActualWidth > 0 && this.ActualHeight > 0)
                {
                    webView.Width = this.ActualWidth;
                    webView.Height = this.ActualHeight;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置 WebView 全屏失败: {ex.Message}");
            }
        }

        public void SetWebViewVisibility(bool isVisible)
        {
            if (!_isWebViewInitialized || webView.CoreWebView2 == null) return;
            webView.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public WebView2 GetWebView()
        {
            return webView;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyFullScreenToWebView();
            ApplyDpiScale();
        }

        #region 跨层鼠标坐标通信扩展

        /// <summary>
        /// 初始化鼠标坐标定时器和窗口消息钩子
        /// </summary>
        private void StartMouseCoordSender()
        {
            // 获取窗口句柄并添加消息钩子
            var helper = new WindowInteropHelper(this);
            hwndSource = HwndSource.FromHwnd(helper.Handle);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
            }

            // 启动定时器，每 100 毫秒发送一次鼠标坐标
            mousePositionTimer = new System.Timers.Timer(10)
            {
                AutoReset = true,
                Enabled = true
            };
            mousePositionTimer.Elapsed += (s, e) => SendMouseCoordinates();
        }

        /// <summary>
        /// 关闭鼠标坐标发送定时器和移除消息钩子
        /// </summary>
        private void StopMouseCoordSender()
        {
            if (mousePositionTimer != null)
            {
                mousePositionTimer.Stop();
                mousePositionTimer.Dispose();
                mousePositionTimer = null;
            }
            if (hwndSource != null)
            {
                hwndSource.RemoveHook(WndProc);
                hwndSource = null;
            }
        }

        /// <summary>
        /// 从系统获取鼠标坐标，并发送到目标窗口（本例中仍然是主窗口）。
        /// 然后在WndProc里通过ExecuteScriptAsync发送到WebView2。
        /// </summary>
        private void SendMouseCoordinates()
        {
            if (NativeMethods.GetCursorPos(out NativeMethods.POINT pt))
            {
                // 将 X 和 Y 坐标打包到 lParam 中：低 16 位为 X，高 16 位为 Y
                IntPtr lParam = (IntPtr)(((pt.Y & 0xFFFF) << 16) | (pt.X & 0xFFFF));
                // 此处 targetHandle 为本窗口句柄
                var targetHandle = _hwnd;
                NativeMethods.SendMessage(targetHandle, (uint)NativeMethods.WM_USER_MOUSE, IntPtr.Zero, lParam);
            }
        }

        /// <summary>
        /// 窗口消息钩子，用于接收自定义鼠标坐标消息，
        /// 再通过 ExecuteScriptAsync 把数据发往 WebView2 JavaScript。
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_USER_MOUSE)
            {
                int xPos = lParam.ToInt32() & 0xFFFF;
                int yPos = (lParam.ToInt32() >> 16) & 0xFFFF;
                System.Diagnostics.Debug.WriteLine($"[跨层通信] 收到鼠标坐标：X={xPos}, Y={yPos}");

                // 将鼠标坐标发送到 WebView2 的 JS 端
                if (_isWebViewInitialized && webView?.CoreWebView2 != null)
                {
                    // 通过JS函数 updateMousePosition(x,y) 更新
                    string jsCode = $"if(typeof updateMousePosition === 'function') {{ updateMousePosition({xPos}, {yPos}); }}";
                    Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            await webView.CoreWebView2.ExecuteScriptAsync(jsCode);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"执行JS代码失败: {ex.Message}");
                        }
                    });
                }

                handled = true;
            }
            return IntPtr.Zero;
        }

        #endregion
    }

    // In the MouseCoordSender class, explicitly qualify Timer:
    public class MouseCoordSender
    {
        private readonly System.Timers.Timer _timer;

        public MouseCoordSender()
        {
            _timer = new System.Timers.Timer(10); // 每 100 毫秒获取一次坐标
            _timer.Elapsed += (s, e) =>
            {
                if (MainWindow.NativeMethods.GetCursorPos(out MainWindow.NativeMethods.POINT pt))
                {
                    Console.WriteLine($"[MouseCoordSender] X={pt.X}, Y={pt.Y}");
                }
            };
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();
    }
}
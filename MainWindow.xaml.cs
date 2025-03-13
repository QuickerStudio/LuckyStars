using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace LuckyStars
{
    public partial class MainWindow : Window
    {
        private bool _isWebViewInitialized = false;
        private readonly List<string> imagePaths;
        private int currentIndex;
        private System.Timers.Timer timer; // 不再是readonly，以便可以修改和重新创建
        private FileSystemWatcher? folderWatcher;
        private System.Timers.Timer? idleTimer;

        // 文件夹路径分开
        private readonly string targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "LuckyStarsWallpaper");

        // 定义需要支持的纵横比（21:9、16:9、6:4、4:3）
        private readonly double[] allowedRatios =
        {
            21.0 / 9.0,
            16.0 / 9.0,
            6.0 / 4.0,
            4.0 / 3.0
        };

        // 闲置超时时间（毫秒）
        private const int IdleTimeout = 60000; // 60秒无变化后进入休眠
        private bool pendingRefresh = false;

        // 注册表键名
        private const string RegistryKey = "SOFTWARE\\LuckyStars";
        private const string TimerIntervalValueName = "TimerInterval";

        // 定时器间隔状态
        public enum TimerState
        {
            FiveMinutes = 300000,    // 5分钟 = 300000毫秒
            TenMinutes = 600000,     // 10分钟 = 600000毫秒
            TwentyMinutes = 1200000, // 20分钟 = 1200000毫秒
            Disabled = 0             // 停用
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
            // 初始化只读字段
            imagePaths = new List<string>();
            // 确保目标文件夹存在
            EnsureDirectoryExists(targetFolder);

            // 设置文件监视器
            SetupFolderWatcher();

            // 初始化闲置计时器
            InitializeIdleTimer();
            // 加载图片
            LoadImagePaths();

            // 从注册表读取计时器间隔
            int timerInterval = LoadTimerIntervalFromRegistry();

            // 创建并启动定时器
            SetupTimer(timerInterval);
            // 显示初始图片
            ShowImage();
        }





        // 加载注册表中的计时器间隔设置
        private int LoadTimerIntervalFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(TimerIntervalValueName);
                        if (value != null && int.TryParse(value.ToString(), out int interval))
                        {
                            return interval;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 忽略注册表访问错误
            }

            // 默认5分钟
            return (int)TimerState.FiveMinutes;
        }

        // 保存计时器间隔到注册表
        public void SaveTimerIntervalToRegistry(int interval)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
                {
                    if (key != null)
                    {
                        key.SetValue(TimerIntervalValueName, interval);
                    }
                }
            }
            catch (Exception)
            {
                // 忽略注册表访问错误
            }
        }

        // 设置定时器间隔
        public void SetTimerInterval(int interval)
        {
            // 保存到注册表
            SaveTimerIntervalToRegistry(interval);

            // 更新定时器
            SetupTimer(interval);
        }

        // 设置和启动定时器
        private void SetupTimer(int interval)
        {
            // 停止现有的定时器
            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= OnTimedEvent;
                timer.Dispose();
            }

            // 如果间隔为0，表示停用定时器
            if (interval == 0)
            {
                timer = null;
                return;
            }

            // 创建新的定时器
            timer = new System.Timers.Timer(interval);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        // 获取当前定时器状态
        public TimerState GetCurrentTimerState()
        {
            int interval = LoadTimerIntervalFromRegistry();

            if (interval == (int)TimerState.FiveMinutes) return TimerState.FiveMinutes;
            if (interval == (int)TimerState.TenMinutes) return TimerState.TenMinutes;
            if (interval == (int)TimerState.TwentyMinutes) return TimerState.TwentyMinutes;
            if (interval == (int)TimerState.Disabled) return TimerState.Disabled;

            // 如果是其他值，默认为5分钟
            return TimerState.FiveMinutes;
        }

        // 循环切换定时器状态
        public TimerState CycleTimerState()
        {
            TimerState currentState = GetCurrentTimerState();
            TimerState nextState;

            switch (currentState)
            {
                case TimerState.FiveMinutes:
                    nextState = TimerState.TenMinutes;
                    break;
                case TimerState.TenMinutes:
                    nextState = TimerState.TwentyMinutes;
                    break;
                case TimerState.TwentyMinutes:
                    nextState = TimerState.Disabled;
                    break;
                case TimerState.Disabled:
                default:
                    nextState = TimerState.FiveMinutes;
                    break;
            }

            SetTimerInterval((int)nextState);
            return nextState;
        }

        private void InitializeIdleTimer()
        {
            idleTimer = new System.Timers.Timer(IdleTimeout);
            idleTimer.AutoReset = false; // 只触发一次
            idleTimer.Elapsed += OnIdleTimeout;
        }

        private void ResetIdleTimer()
        {
            if (idleTimer != null)
            {
                idleTimer.Stop();
                idleTimer.Start();
            }
            pendingRefresh = true; // 标记需要刷新
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
                // 创建文件监视器
                folderWatcher = new FileSystemWatcher(targetFolder)
                {
                    // 监视所有支持的图片文件格式
                    Filter = "*.*",
                    // 监视文件的创建、删除、更改和重命名
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                                   NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    // 启用监视
                    EnableRaisingEvents = true,
                    // 包括子文件夹
                    IncludeSubdirectories = false
                };

                // 添加事件处理程序
                folderWatcher.Created += OnFolderChanged;
                folderWatcher.Deleted += OnFolderChanged;
                folderWatcher.Renamed += OnFolderRenamed;
            }
            catch (Exception)
            {
                // 如果监视器设置失败，只是继续执行，不显示错误
                folderWatcher = null;
            }
        }

        private void OnFolderChanged(object sender, FileSystemEventArgs e)
        {
            // 检查是否是支持的图片格式
            string extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };

            if (supportedExtensions.Contains(extension))
            {
                // 重置闲置计时器，延迟刷新
                ResetIdleTimer();
            }
        }

        private void OnFolderRenamed(object sender, RenamedEventArgs e)
        {
            // 检查是否是支持的图片格式
            string extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };

            if (supportedExtensions.Contains(extension))
            {
                // 重置闲置计时器，延迟刷新
                ResetIdleTimer();
            }
        }

        private static void EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
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

                var extensions = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.tiff" }; // 增加支持 TIFF 格式
                foreach (var ext in extensions)
                {
                    var files = Directory.GetFiles(targetFolder, ext);

                    // 遍历并筛选符合要求的图片
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

                            // 获取宽高比
                            double ratio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                            // 设置一个允许的浮动范围，用于对比宽高比是否接近（可酌情调整）
                            double tolerance = 0.01;

                            // 判断是否符合任何一个目标比例
                            if (allowedRatios.Any(ar => Math.Abs(ar - ratio) < tolerance))
                            {
                                imagePaths.Add(file);
                            }
                        }
                        catch (Exception)
                        {
                            // 加载图片失败，静默忽略，尝试下一张
                        }
                    }
                }

                // 根据文件名排序
                var sortedImagePaths = imagePaths.OrderBy(path => path).ToList();
                imagePaths.Clear();
                imagePaths.AddRange(sortedImagePaths);
            }
            catch (Exception)
            {
                // 整体加载过程出错，也忽略错误
            }
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

        // 在窗口关闭时进行清理
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
            }

            if (idleTimer != null)
            {
                idleTimer.Stop();
                idleTimer.Dispose();
                idleTimer = null;
            }
        }


        private async void InitializeWebView()
        {
            try
            {
                await webView.EnsureCoreWebView2Async();
                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

                _isWebViewInitialized = true;
                LoadTestHtml();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadTestHtml()
        {
            if (!_isWebViewInitialized || webView.CoreWebView2 == null) return;

            string html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>LuckyStars</title>
    <style>
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
    <canvas id=""particleCanvas""></canvas>

    <script>
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
                    const distanceSq = dx*dx + dy*dy;

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
                    const distance = dx*dx + dy*dy;

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
                    const distance = dx*dx + dy*dy;
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
                    const dynamicRadius = p.size * (config.glowRadiusFactor + Math.sin(Date.now()*0.0008 + p.x)*config.glowRadiusOffset);
                    
                    // 创建径向渐变
                    const gradient = ctx.createRadialGradient(
                        p.x, p.y, 0, 
                        p.x, p.y, dynamicRadius
                    );
                    gradient.addColorStop(0, `hsla(${(hue + p.x/10) % 360}, 80%, 60%, 0.3)`);
                    gradient.addColorStop(1, 'transparent');

                    // 绘制光晕
                    ctx.fillStyle = gradient;
                    ctx.beginPath();
                    ctx.arc(p.x, p.y, dynamicRadius, 0, Math.PI*2);
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
            mouse.x = e.clientX;
            mouse.y = e.clientY;
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

            if (isVisible)
            {
                webView.Visibility = Visibility.Visible;
                webView.CoreWebView2?.Reload();
            }
            else
            {
                webView.Visibility = Visibility.Collapsed;
            }
        }

        public WebView2 GetWebView()
        {
            return webView;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyFullScreenToWebView();
        }




    }
}
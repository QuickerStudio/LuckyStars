using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using LuckyStars.Utils;

namespace LuckyStars.Managers
{
    /// <summary>
    /// 文件传输管理器 - 使用智能文件传输服务，防止线程泄漏和资源占用
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class FileTransferManager
    {
        private readonly MainWindow _mainWindow;
        private readonly SmartFileTransferService _transferService;

        // 静态字段，确保根目录路径在整个应用程序中唯一
        private static readonly string _rootTargetFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "LuckyStarsWallpaper"
        );

        // 静态构造函数
        static FileTransferManager()
        {
            // 不再在这里创建根目录，而是依赖 FileSystemMonitor
            // 这里只是记录一下根目录路径
            Console.WriteLine($"FileTransferManager 初始化，根目录路径: {_rootTargetFolder}");
        }

        public FileTransferManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            // 使用静态字段作为目标文件夹路径，确保唯一性
            // 创建智能文件传输服务
            _transferService = new SmartFileTransferService(_rootTargetFolder);

            // 订阅根目录缺失事件
            _transferService.RootDirectoryMissing += OnRootDirectoryMissing;
        }

        /// <summary>
        /// 处理根目录缺失事件
        /// </summary>
        private void OnRootDirectoryMissing(object? sender, string directoryPath)
        {
            Console.WriteLine($"检测到根目录不存在: {directoryPath}");
            Console.WriteLine("警告：根目录应由 FileSystemMonitor 创建，而不是 FileTransferManager");

            // 不再在这里创建根目录，而是依赖 FileSystemMonitor
            // 这里只是记录一个警告
        }

        /// <summary>
        /// 处理文件夹拖放
        /// </summary>
        public async Task ProcessFolderDrop(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            try
            {
                // 使用文件传输服务处理文件夹
                var (_, _, firstAudioFile) = await _transferService.TransferFolderAsync(folderPath);

                // 如果有音频文件，播放它
                if (firstAudioFile != null && File.Exists(firstAudioFile))
                    _mainWindow?.HandleMusicFileDrop(firstAudioFile);
            }
            catch
            {
                // 忽略错误
            }
        }

        /// <summary>
        /// 处理单个文件拖放
        /// </summary>
        public (bool success, string? audioPath) ProcessSingleFileDrop(string filePath, bool playAudio = true)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return (false, null);

            try
            {
                // 使用文件传输服务处理单个文件
                var (success, audioPath) = _transferService.TransferSingleFile(filePath, playAudio);

                // 如果是音频文件，根据参数决定是否播放
                if (audioPath != null && playAudio)
                    _mainWindow?.HandleMusicFileDrop(audioPath);

                return (success, audioPath);
            }
            catch
            {
                return (false, null);
            }
        }
    }
}

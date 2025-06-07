using System;
using System.IO;
using System.Windows;
using LuckyStars.UI;

namespace LuckyStars.Managers.TrayManagement.DropHandling
{
    /// <summary>
    /// 托盘拖放处理器，负责处理文件拖放到托盘图标的功能
    /// </summary>
    public class TrayDropHandler
    {
        private readonly MainWindow _mainWindow;
        private readonly TrayDropWindow _dropWindow;
        private readonly FileTransferManager _fileTransferManager;

        /// <summary>
        /// 初始化托盘拖放处理器
        /// </summary>
        /// <param name="mainWindow">主窗口实例</param>
        /// <param name="dropWindow">拖放窗口</param>
        public TrayDropHandler(MainWindow mainWindow, TrayDropWindow dropWindow)
        {
            _mainWindow = mainWindow;
            _dropWindow = dropWindow;

            // 初始化文件传输管理器
            _fileTransferManager = new FileTransferManager(mainWindow);

            // 注册文件拖放事件
            _dropWindow.FileDropped += OnFileDropped;
        }

        /// <summary>
        /// 处理文件拖放事件
        /// </summary>
        public void OnFileDropped(string[] files)
        {
            string? audioFilePath = null; // 记录找到的音频文件路径

            // 处理所有文件
            foreach (var filePath in files)
            {
                if (Directory.Exists(filePath))
                {
                    // 启动文件夹处理任务
                    _ = _fileTransferManager.ProcessFolderDrop(filePath);
                }
                else
                {
                    // 处理单个文件，但不自动播放音频
                    var (success, audioPath) = _fileTransferManager.ProcessSingleFileDrop(filePath, false);

                    // 如果是音频文件且还没有找到音频文件，则记录这个文件
                    if (success && audioPath != null && audioFilePath == null)
                    {
                        audioFilePath = audioPath;
                    }
                }
            }

            // 如果有音频文件，则播放它
            if (audioFilePath != null)
            {
                // 使用Dispatcher确保在UI线程上播放
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _mainWindow.HandleMusicFileDrop(audioFilePath);
                });
            }

            // 文件接收完成后，隐藏拖放窗口并重置拖拽状态
            _dropWindow?.Hide();
        }

        /// <summary>
        /// 获取文件传输管理器
        /// </summary>
        public FileTransferManager FileTransferManager => _fileTransferManager;
    }
}

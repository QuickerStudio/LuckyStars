using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 依赖项管理器，负责管理和下载应用程序依赖的外部组件
    /// </summary>
    public class DependencyManager : IDisposable
    {
        private readonly string _dependenciesDirectory;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, DependencyInfo> _registeredDependencies;
        private bool _isInitialized = false;

        /// <summary>
        /// 依赖项信息类
        /// </summary>
        public class DependencyInfo
        {
            /// <summary>
            /// 依赖项名称
            /// </summary>
            public string Name { get; set; }
            
            /// <summary>
            /// 依赖项版本
            /// </summary>
            public string Version { get; set; }
            
            /// <summary>
            /// 下载地址
            /// </summary>
            public string DownloadUrl { get; set; }
            
            /// <summary>
            /// 文件哈希值（MD5）
            /// </summary>
            public string FileHash { get; set; }
            
            /// <summary>
            /// 本地路径
            /// </summary>
            public string LocalPath { get; set; }
            
            /// <summary>
            /// 是否为压缩包
            /// </summary>
            public bool IsArchive { get; set; }
            
            /// <summary>
            /// 是否为必需项
            /// </summary>
            public bool IsRequired { get; set; }
            
            /// <summary>
            /// 是否已安装
            /// </summary>
            public bool IsInstalled { get; private set; }
            
            /// <summary>
            /// 安装状态变更事件
            /// </summary>
            public event EventHandler<bool> InstallationStatusChanged;
            
            /// <summary>
            /// 设置安装状态
            /// </summary>
            public void SetInstalled(bool installed)
            {
                if (IsInstalled != installed)
                {
                    IsInstalled = installed;
                    InstallationStatusChanged?.Invoke(this, installed);
                }
            }
            
            /// <summary>
            /// 获取安装文件夹路径
            /// </summary>
            public string GetInstallFolder()
            {
                return Path.GetDirectoryName(LocalPath);
            }
        }
        
        /// <summary>
        /// 依赖项下载进度事件
        /// </summary>
        public class DependencyDownloadProgressEventArgs : EventArgs
        {
            /// <summary>
            /// 依赖项名称
            /// </summary>
            public string DependencyName { get; }
            
            /// <summary>
            /// 下载进度百分比
            /// </summary>
            public int ProgressPercentage { get; }
            
            /// <summary>
            /// 已下载字节数
            /// </summary>
            public long BytesReceived { get; }
            
            /// <summary>
            /// 总字节数
            /// </summary>
            public long TotalBytes { get; }
            
            /// <summary>
            /// 构造函数
            /// </summary>
            public DependencyDownloadProgressEventArgs(string dependencyName, int progressPercentage, long bytesReceived, long totalBytes)
            {
                DependencyName = dependencyName;
                ProgressPercentage = progressPercentage;
                BytesReceived = bytesReceived;
                TotalBytes = totalBytes;
            }
        }
        
        /// <summary>
        /// 依赖项下载完成事件
        /// </summary>
        public class DependencyDownloadCompletedEventArgs : EventArgs
        {
            /// <summary>
            /// 依赖项名称
            /// </summary>
            public string DependencyName { get; }
            
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Success { get; }
            
            /// <summary>
            /// 错误消息
            /// </summary>
            public string ErrorMessage { get; }
            
            /// <summary>
            /// 本地路径
            /// </summary>
            public string LocalPath { get; }
            
            /// <summary>
            /// 构造函数 - 成功
            /// </summary>
            public DependencyDownloadCompletedEventArgs(string dependencyName, string localPath)
            {
                DependencyName = dependencyName;
                Success = true;
                LocalPath = localPath;
            }
 public DependencyDownloadCompletedEventArgs(string dependencyName, string errorMessage)
            {
                DependencyName = dependencyName;
                Success = false;
                ErrorMessage = errorMessage;
                LocalPath = null;
            }
        }

        /// <summary>
        /// 依赖项下载进度事件
        /// </summary>
        public event EventHandler<DependencyDownloadProgressEventArgs> DependencyDownloadProgress;
        
        /// <summary>
        /// 依赖项下载完成事件
        /// </summary>
        public event EventHandler<DependencyDownloadCompletedEventArgs> DependencyDownloadCompleted;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dependenciesDirectory">依赖项存储目录</param>
        public DependencyManager(string dependenciesDirectory)
        {
            _dependenciesDirectory = dependenciesDirectory ?? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "LuckyStars", "Dependencies");
            
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LuckyStars-DependencyManager/1.0");
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // 较长的超时时间用于大型依赖项
            
            _registeredDependencies = new Dictionary<string, DependencyInfo>();
            
            // 确保依赖项目录存在
            if (!Directory.Exists(_dependenciesDirectory))
            {
                Directory.CreateDirectory(_dependenciesDirectory);
            }
        }
        
        /// <summary>
        /// 初始化依赖项管理器
        /// </summary>
        /// <returns>是否初始化成功</returns>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
                return true;
            
            try
            {
                // 检查并创建依赖项目录
                if (!Directory.Exists(_dependenciesDirectory))
                {
                    Directory.CreateDirectory(_dependenciesDirectory);
                }
                
                // 检查已注册的依赖项是否存在
                foreach (var dependency in _registeredDependencies.Values)
                {
                    dependency.SetInstalled(IsDependencyInstalled(dependency));
                }
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化依赖项管理器失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 注册依赖项
        /// </summary>
        /// <param name="name">依赖项名称</param>
        /// <param name="version">版本号</param>
        /// <param name="downloadUrl">下载地址</param>
        /// <param name="localPath">本地路径</param>
        /// <param name="fileHash">文件哈希值</param>
        /// <param name="isArchive">是否为压缩包</param>
        /// <param name="isRequired">是否为必需项</param>
        /// <returns>依赖项信息</returns>
        public DependencyInfo RegisterDependency(
            string name, 
            string version, 
            string downloadUrl, 
            string localPath = null, 
            string fileHash = null, 
            bool isArchive = false, 
            bool isRequired = true)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(downloadUrl))
            {
                throw new ArgumentException("依赖项名称和下载地址不能为空");
            }
            
            // 如果未指定本地路径，则使用默认路径
            localPath = localPath ?? Path.Combine(_dependenciesDirectory, name, 
                isArchive ? $"{name}-{version}.zip" : Path.GetFileName(new Uri(downloadUrl).AbsolutePath));
            
            // 创建依赖项信息
            var dependencyInfo = new DependencyInfo
            {
                Name = name,
                Version = version,
                DownloadUrl = downloadUrl,
                LocalPath = localPath,
                FileHash = fileHash,
                IsArchive = isArchive,
                IsRequired = isRequired
            };
            
            // 检查并设置安装状态
            dependencyInfo.SetInstalled(IsDependencyInstalled(dependencyInfo));
            
            // 添加到已注册依赖项字典
            _registeredDependencies[name] = dependencyInfo;
            
            Debug.WriteLine($"注册依赖项: {name} {version}, 状态: {(dependencyInfo.IsInstalled ? "已安装" : "未安装")}");
            
            return dependencyInfo;
        }
        
        /// <summary>
        /// 检查依赖项是否已安装
        /// </summary>
        /// <param name="dependencyInfo">依赖项信息</param>
        /// <returns>是否已安装</returns>
        private bool IsDependencyInstalled(DependencyInfo dependencyInfo)
        {
            try
            {
                // 检查文件是否存在
                if (!File.Exists(dependencyInfo.LocalPath))
                {
                    return false;
                }
                
                // 如果有指定哈希值，则验证文件完整性
                if (!string.IsNullOrEmpty(dependencyInfo.FileHash))
                {
                    string calculatedHash = CalculateFileHash(dependencyInfo.LocalPath);
                    if (!string.Equals(calculatedHash, dependencyInfo.FileHash, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"依赖项 {dependencyInfo.Name} 哈希值不匹配, 预期: {dependencyInfo.FileHash}, 实际: {calculatedHash}");
                        return false;
                    }
                }
                
                // 如果是压缩包，还需要检查解压后的内容是否存在
                if (dependencyInfo.IsArchive)
                {
                    string extractDir = Path.Combine(Path.GetDirectoryName(dependencyInfo.LocalPath), "bin");
                    if (!Directory.Exists(extractDir) || Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories).Length == 0)
                    {
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查依赖项 {dependencyInfo.Name} 安装状态时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 计算文件的MD5哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>MD5哈希值字符串</returns>
        private string CalculateFileHash(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
                
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// 安装依赖项
        /// </summary>
        /// <param name="name">依赖项名称</param>
        /// <param name="force">是否强制重新安装</param>
        /// <returns>安装结果</returns>
        public async Task<bool> InstallDependencyAsync(string name, bool force = false)
        {
            if (!_registeredDependencies.TryGetValue(name, out DependencyInfo dependency))
            {
                Debug.WriteLine($"尝试安装未注册的依赖项: {name}");
                return false;
            }
            
            try
            {
                // 如果已经安装且不需要强制重新安装，则直接返回成功
                if (dependency.IsInstalled && !force)
                {
                    Debug.WriteLine($"依赖项 {name} 已安装，跳过安装");
                    return true;
                }
                
                // 确保上级目录存在
                string directory = Path.GetDirectoryName(dependency.LocalPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 下载依赖项
                bool downloadSuccess = await DownloadDependencyAsync(dependency);
                if (!downloadSuccess)
                {
                    Debug.WriteLine($"依赖项 {name} 下载失败");
                    return false;
                }
                
                // 如果是压缩包，则解压
                if (dependency.IsArchive)
                {
                    string extractDir = Path.Combine(Path.GetDirectoryName(dependency.LocalPath), "bin");
                    
                    // 如果解压目录已存在，先尝试清空
                    if (Directory.Exists(extractDir) && force)
                    {
                        try
                        {
                            Directory.Delete(extractDir, true);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"清空解压目录失败: {ex.Message}");
                        }
                    }
                    
                    // 创建解压目录
                    if (!Directory.Exists(extractDir))
                    {
                        Directory.CreateDirectory(extractDir);
                    }
                    
                    // 解压文件
                    await Task.Run(() =>
                    {
                        try
                        {
                            ZipFile.ExtractToDirectory(dependency.LocalPath, extractDir, overwriteFiles: true);
                            Debug.WriteLine($"依赖项 {name} 解压成功");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"解压依赖项 {name} 时出错: {ex.Message}");
                            throw; // 重新抛出异常
                        }
                    });
                }
                
                // 标记为已安装
                dependency.SetInstalled(true);
                
                Debug.WriteLine($"依赖项 {name} 安装成功");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"安装依赖项 {name} 时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 下载依赖项
        /// </summary>
        /// <param name="dependency">依赖项信息</param>
        /// <returns>下载结果</returns>
        private async Task<bool> DownloadDependencyAsync(DependencyInfo dependency)
        {
            try
            {
                // 创建临时文件名用于下载
                string tempFilePath = dependency.LocalPath + ".downloading";
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(dependency.LocalPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 开始下载
                using (var response = await _httpClient.GetAsync(dependency.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"下载依赖项 {dependency.Name} 失败, HTTP状态码: {response.StatusCode}");
                        DependencyDownloadCompleted?.Invoke(this, new DependencyDownloadCompletedEventArgs(
                            dependency.Name, $"HTTP错误: {response.StatusCode}"));
                        return false;
                    }
                    
                    long totalBytes = response.Content.Headers.ContentLength ?? -1;
                    
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var downloadStream = await response.Content.ReadAsStreamAsync())
                    {
                        byte[] buffer = new byte[8192];
                        long bytesReceived = 0;
                        int bytesRead;
                        
                        while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            
                            bytesReceived += bytesRead;
                            
                            // 报告进度
                            int progressPercentage = totalBytes > 0 
                                ? (int)((bytesReceived * 100) / totalBytes) 
                                : -1;
                                
                            DependencyDownloadProgress?.Invoke(this, new DependencyDownloadProgressEventArgs(
                                dependency.Name, progressPercentage, bytesReceived, totalBytes));
                        }
                    }
                }
                
                // 下载完成后，移动到正式位置
                if (File.Exists(dependency.LocalPath))
                {
                    File.Delete(dependency.LocalPath);
                }
                File.Move(tempFilePath, dependency.LocalPath);
                
                // 验证文件完整性
                if (!string.IsNullOrEmpty(dependency.FileHash))
                {
                    string calculatedHash = CalculateFileHash(dependency.LocalPath);
                    if (!string.Equals(calculatedHash, dependency.FileHash, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"依赖项 {dependency.Name} 哈希值验证失败, 预期: {dependency.FileHash}, 实际: {calculatedHash}");
                        File.Delete(dependency.LocalPath); // 删除不匹配的文件
                        
                        DependencyDownloadCompleted?.Invoke(this, new DependencyDownloadCompletedEventArgs(
                            dependency.Name, "文件哈希值验证失败"));
                        return false;
                    }
                }
                
                // 触发下载完成事件
                DependencyDownloadCompleted?.Invoke(this, new DependencyDownloadCompletedEventArgs(
                    dependency.Name, dependency.LocalPath));
                    
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"下载依赖项 {dependency.Name} 时出错: {ex.Message}");
                DependencyDownloadCompleted?.Invoke(this, new DependencyDownloadCompletedEventArgs(
                    dependency.Name, $"下载错误: {ex.Message}"));
                return false;
            }
        }
        
        /// <summary>
        /// 获取依赖项信息
        /// </summary>
        /// <param name="name">依赖项名称</param>
        /// <returns>依赖项信息，不存在则返回null</returns>
        public DependencyInfo GetDependency(string name)
        {
            if (_registeredDependencies.TryGetValue(name, out DependencyInfo dependency))
            {
                return dependency;
            }
            return null;
        }
        
        /// <summary>
        /// 获取所有已注册的依赖项
        /// </summary>
        /// <returns>依赖项列表</returns>
        public List<DependencyInfo> GetAllDependencies()
        {
            return _registeredDependencies.Values.ToList();
        }
        
        /// <summary>
        /// 检查是否安装了所有必需的依赖项
        /// </summary>
        /// <returns>是否安装了所有必需依赖项</returns>
        public bool AreRequiredDependenciesInstalled()
        {
            return _registeredDependencies.Values
                .Where(d => d.IsRequired)
                .All(d => d.IsInstalled);
        }
        
        /// <summary>
        /// 获取未安装的依赖项列表
        /// </summary>
        /// <returns>未安装的依赖项列表</returns>
        public List<DependencyInfo> GetMissingDependencies()
        {
            return _registeredDependencies.Values
                .Where(d => !d.IsInstalled)
                .ToList();
        }
        
        /// <summary>
        /// 安装所有必需的依赖项
        /// </summary>
        /// <param name="progress">进度回调</param>
        /// <returns>安装结果</returns>
        public async Task<bool> InstallRequiredDependenciesAsync(IProgress<(string, int)> progress = null)
        {
            var requiredDeps = _registeredDependencies.Values
                .Where(d => d.IsRequired && !d.IsInstalled)
                .ToList();
                
            if (requiredDeps.Count == 0)
            {
                Debug.WriteLine("所有必需依赖项已安装");
                return true;
            }
            
            Debug.WriteLine($"开始安装 {requiredDeps.Count} 个必需依赖项");
            
            int totalInstalled = 0;
            bool allSuccess = true;
            
            foreach (var dependency in requiredDeps)
            {
                progress?.Report((dependency.Name, (totalInstalled * 100) / requiredDeps.Count));
                
                bool success = await InstallDependencyAsync(dependency.Name);
                if (!success)
                {
                    Debug.WriteLine($"安装依赖项 {dependency.Name} 失败");
                    allSuccess = false;
                }
                
                totalInstalled++;
                progress?.Report((dependency.Name, (totalInstalled * 100) / requiredDeps.Count));
            }
            
            Debug.WriteLine($"依赖项安装完成, 结果: {(allSuccess ? "全部成功" : "部分失败")}");
            return allSuccess;
        }
        
        /// <summary>
        /// 卸载依赖项
        /// </summary>
        /// <param name="name">依赖项名称</param>
        /// <returns>卸载结果</returns>
        public bool UninstallDependency(string name)
        {
            if (!_registeredDependencies.TryGetValue(name, out DependencyInfo dependency))
            {
                Debug.WriteLine($"尝试卸载未注册的依赖项: {name}");
                return false;
            }
            
            try
            {
                // 删除依赖项文件
                if (File.Exists(dependency.LocalPath))
                {
                    File.Delete(dependency.LocalPath);
                }
                
                // 如果是压缩包，删除解压的目录
                if (dependency.IsArchive)
                {
                    string extractDir = Path.Combine(Path.GetDirectoryName(dependency.LocalPath), "bin");
                    if (Directory.Exists(extractDir))
                    {
                        Directory.Delete(extractDir, true);
                    }
                }
                
                // 标记为未安装
                dependency.SetInstalled(false);
                
                Debug.WriteLine($"依赖项 {name} 卸载成功");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"卸载依赖项 {name} 时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 清理未使用的依赖项
        /// </summary>
        /// <returns>清理结果</returns>
        public bool CleanupUnusedDependencies()
        {
            try
            {
                // 获取所有注册的依赖项目录
                var registeredDirs = new HashSet<string>(_registeredDependencies.Values
                    .Select(d => Path.GetDirectoryName(d.LocalPath)));
                
                // 获取实际存在的依赖项目录
                string[] existingDirs = Directory.GetDirectories(_dependenciesDirectory);
                
                // 找出未注册但存在的目录
                var unusedDirs = existingDirs
                    .Where(dir => !registeredDirs.Contains(dir))
                    .ToList();
                
                // 删除未使用的目录
                foreach (string dir in unusedDirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        Debug.WriteLine($"已删除未使用的依赖项目录: {dir}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"删除未使用的依赖项目录 {dir} 时出错: {ex.Message}");
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理未使用的依赖项时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
# 生成测试文件脚本
# 用于创建200个音乐文件、200个视频和200个图片，用于测试文件传输服务

# 设置基本路径
$basePath = "C:\TestFiles"

# 创建主文件夹
if (-not (Test-Path $basePath)) {
    New-Item -Path $basePath -ItemType Directory | Out-Null
    Write-Host "创建主文件夹: $basePath"
}

# 创建子文件夹
$musicFolder = Join-Path $basePath "Music"
$videoFolder = Join-Path $basePath "Video"
$imageFolder = Join-Path $basePath "Images"

foreach ($folder in @($musicFolder, $videoFolder, $imageFolder)) {
    if (-not (Test-Path $folder)) {
        New-Item -Path $folder -ItemType Directory | Out-Null
        Write-Host "创建文件夹: $folder"
    }
}

# 生成随机字节数据的函数
function Generate-RandomBytes {
    param (
        [int]$size
    )
    
    $bytes = New-Object byte[] $size
    $rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
    $rng.GetBytes($bytes)
    return $bytes
}

# 生成音乐文件
Write-Host "开始生成200个音乐文件..."
$musicExtensions = @(".mp3", ".wav", ".ogg", ".flac", ".aac", ".wma", ".m4a")
for ($i = 1; $i -le 200; $i++) {
    $extension = $musicExtensions[(Get-Random -Minimum 0 -Maximum $musicExtensions.Length)]
    $fileName = "music_$($i.ToString('000'))$extension"
    $filePath = Join-Path $musicFolder $fileName
    
    # 生成随机大小的文件 (100KB - 2MB)
    $fileSize = Get-Random -Minimum 100KB -Maximum 2MB
    $bytes = Generate-RandomBytes -size $fileSize
    [System.IO.File]::WriteAllBytes($filePath, $bytes)
    
    if ($i % 20 -eq 0) {
        Write-Host "已生成 $i 个音乐文件"
    }
}

# 生成视频文件
Write-Host "开始生成200个视频文件..."
$videoExtensions = @(".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v")
for ($i = 1; $i -le 200; $i++) {
    $extension = $videoExtensions[(Get-Random -Minimum 0 -Maximum $videoExtensions.Length)]
    $fileName = "video_$($i.ToString('000'))$extension"
    $filePath = Join-Path $videoFolder $fileName
    
    # 生成随机大小的文件 (1MB - 10MB)
    $fileSize = Get-Random -Minimum 1MB -Maximum 10MB
    $bytes = Generate-RandomBytes -size $fileSize
    [System.IO.File]::WriteAllBytes($filePath, $bytes)
    
    if ($i % 20 -eq 0) {
        Write-Host "已生成 $i 个视频文件"
    }
}

# 生成图片文件
Write-Host "开始生成200个图片文件..."
$imageExtensions = @(".jpg", ".png", ".gif", ".bmp", ".tiff", ".webp")
for ($i = 1; $i -le 200; $i++) {
    $extension = $imageExtensions[(Get-Random -Minimum 0 -Maximum $imageExtensions.Length)]
    $fileName = "image_$($i.ToString('000'))$extension"
    $filePath = Join-Path $imageFolder $fileName
    
    # 生成随机大小的文件 (50KB - 500KB)
    $fileSize = Get-Random -Minimum 50KB -Maximum 500KB
    $bytes = Generate-RandomBytes -size $fileSize
    [System.IO.File]::WriteAllBytes($filePath, $bytes)
    
    if ($i % 20 -eq 0) {
        Write-Host "已生成 $i 个图片文件"
    }
}

Write-Host "所有测试文件生成完成！"
Write-Host "音乐文件: $musicFolder"
Write-Host "视频文件: $videoFolder"
Write-Host "图片文件: $imageFolder"
Write-Host ""
Write-Host "文件总数: 600个 (200个音乐文件 + 200个视频文件 + 200个图片文件)"
Write-Host "注意: 这些文件只包含随机数据，不能实际播放或查看，仅用于测试文件传输功能。"

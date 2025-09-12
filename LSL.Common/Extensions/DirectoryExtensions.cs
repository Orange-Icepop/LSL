namespace LSL.Common.Extensions;

public enum DirectoryCopyMode
{
    CopyDirectory,
    CopyContentsOnly
}

public static class DirectoryExtensions
{
    public static async Task CopyDirectoryAsync(
        string sourceDirectoryPath,
        string destinationDirectoryPath,
        DirectoryCopyMode copyMode = DirectoryCopyMode.CopyDirectory,
        FileOverwriteMode overwrite = FileOverwriteMode.Throw,
        int bufferSize = 81920,
        IProgress<long>? progress = null,
        IProgress<string>? fileNameProgress = null,
        CancellationToken cancellationToken = default)
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(sourceDirectoryPath))
            throw new ArgumentException("Invalid source directory path", nameof(sourceDirectoryPath));
        
        if (string.IsNullOrWhiteSpace(destinationDirectoryPath))
            throw new ArgumentException("Invalid destination directory path", nameof(destinationDirectoryPath));

        // 检查源目录是否存在
        if (!Directory.Exists(sourceDirectoryPath))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectoryPath}");

        // 计算最终目标路径
        string effectiveDestinationPath = copyMode == DirectoryCopyMode.CopyDirectory
            ? Path.Combine(destinationDirectoryPath, Path.GetFileName(sourceDirectoryPath))
            : destinationDirectoryPath;

        // 创建目标目录（如果不存在）
        Directory.CreateDirectory(effectiveDestinationPath);

        // 递归拷贝目录内容
        await CopyDirectoryRecursiveAsync(
            sourceDirectoryPath,
            effectiveDestinationPath,
            overwrite,
            bufferSize,
            progress,
            fileNameProgress,
            cancellationToken);
    }

    private static async Task CopyDirectoryRecursiveAsync(
        string sourceDir,
        string targetDir,
        FileOverwriteMode overwrite,
        int bufferSize,
        IProgress<long>? progress,
        IProgress<string>? fileNameProgress,
        CancellationToken cancellationToken)
    {
        // 用于累计拷贝的总字节数
        long totalBytesCopied = 0;

        // 创建目标目录（确保存在）
        Directory.CreateDirectory(targetDir);

        // 拷贝所有文件
        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            string fileName = Path.GetFileName(filePath);
            string destPath = Path.Combine(targetDir, fileName);
            
            // 获取文件大小用于进度报告
            long fileSize = new FileInfo(filePath).Length;
            
            // 拷贝文件
            await FileExtensions.CopyFileAsync(
                sourcePath: filePath,
                destinationPath: destPath,
                overwrite: overwrite,
                bufferSize: bufferSize,
                progress: null, // 不报告单个文件进度
                cancellationToken: cancellationToken);
            
            // 更新总进度
            totalBytesCopied += fileSize;
            progress?.Report(totalBytesCopied);
            fileNameProgress?.Report(filePath);
        }

        // 递归处理子目录
        foreach (var subDirPath in Directory.GetDirectories(sourceDir))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            string subDirName = Path.GetFileName(subDirPath);
            string newTargetDir = Path.Combine(targetDir, subDirName);
            
            await CopyDirectoryRecursiveAsync(
                sourceDir: subDirPath,
                targetDir: newTargetDir,
                overwrite: overwrite,
                bufferSize: bufferSize,
                progress: progress,
                fileNameProgress: fileNameProgress,
                cancellationToken: cancellationToken);
        }
    }
}
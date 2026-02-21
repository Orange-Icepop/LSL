namespace LSL.Common.Utilities.FileSystem;

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
        bool recursive = false,
        bool skipErrors = false,
        DirectoryCopyMode copyMode = DirectoryCopyMode.CopyDirectory,
        FileOverwriteMode overwrite = FileOverwriteMode.Throw,
        int bufferSize = 81920,
        IProgress<long>? totalBytesCopiedProgress = null,
        IProgress<string>? fileInProgress = null,
        CancellationToken cancellationToken = default)
    {
        // 参数验证
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
        if (string.IsNullOrWhiteSpace(sourceDirectoryPath))
            throw new ArgumentException("Invalid source directory path", nameof(sourceDirectoryPath));

        if (string.IsNullOrWhiteSpace(destinationDirectoryPath))
            throw new ArgumentException("Invalid destination directory path", nameof(destinationDirectoryPath));

        // 检查源目录是否存在
        if (!Directory.Exists(sourceDirectoryPath))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectoryPath}");

        // 计算最终目标路径
        destinationDirectoryPath = copyMode is DirectoryCopyMode.CopyDirectory
            ? Path.Combine(destinationDirectoryPath, new DirectoryInfo(sourceDirectoryPath).Name)
            : destinationDirectoryPath;

        // 创建目标目录（如果不存在）
        Directory.CreateDirectory(destinationDirectoryPath);

        // 递归拷贝目录内容
        if (recursive)
        {
            await CopyDirectoryContentRecursiveAsync(
                sourceDirectoryPath,
                destinationDirectoryPath,
                skipErrors,
                overwrite,
                bufferSize,
                totalBytesCopiedProgress,
                fileInProgress,
                new CopyProgressState(),
                cancellationToken);
        }
        else
        {
            long totalBytesCopied = 0;
            // 拷贝所有文件
            foreach (var filePath in Directory.EnumerateFiles(sourceDirectoryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                fileInProgress?.Report(filePath);
                var fileName = Path.GetFileName(filePath);
                var destPath = Path.Combine(destinationDirectoryPath, fileName);

                // 获取文件大小用于进度报告
                // 若没有要求报告则不创建FileInfo
                var fileSize = totalBytesCopiedProgress is null ? 0 : new FileInfo(filePath).Length;

                // 拷贝文件
                try
                {
                    await FileExtensions.CopyFileAsync(
                        filePath,
                        destPath,
                        overwrite,
                        bufferSize,
                        null, // 不报告单个文件进度
                        cancellationToken);
                }
                catch (Exception)
                {
                    if (!skipErrors) throw;
                    fileInProgress?.Report($"Error: {filePath} failed to copy");
                }

                // 更新总进度
                totalBytesCopied += fileSize;
                totalBytesCopiedProgress?.Report(totalBytesCopied);
            }
        }

        fileInProgress?.Report("Copy finished");
    }

    private static async Task CopyDirectoryContentRecursiveAsync(
        string sourceDir,
        string targetDir,
        bool skipErrors,
        FileOverwriteMode overwrite,
        int bufferSize,
        IProgress<long>? totalBytesCopiedProgress,
        IProgress<string>? fileInProgress,
        CopyProgressState progress,
        CancellationToken cancellationToken)
    {
        // 创建目标目录（确保存在）
        Directory.CreateDirectory(targetDir);

        // 拷贝所有文件
        foreach (var filePath in Directory.EnumerateFiles(sourceDir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.FileNameInProgress = filePath;
            fileInProgress?.Report(progress.FileNameInProgress);
            var fileName = Path.GetFileName(filePath);
            var destPath = Path.Combine(targetDir, fileName);

            // 获取文件大小用于进度报告
            // 若没有要求报告则不创建FileInfo
            var fileSize = totalBytesCopiedProgress is null ? 0 : new FileInfo(filePath).Length;

            // 拷贝文件
            try
            {
                await FileExtensions.CopyFileAsync(
                    filePath,
                    destPath,
                    overwrite,
                    bufferSize,
                    null, // 不报告单个文件进度
                    cancellationToken);
            }
            catch (Exception)
            {
                if (!skipErrors) throw;
                fileInProgress?.Report($"Error: {progress.FileNameInProgress} failed to copy");
            }

            // 更新总进度
            progress.BytesCopied += fileSize;
            totalBytesCopiedProgress?.Report(progress.BytesCopied);
        }

        // 递归处理子目录
        foreach (var subDirPath in Directory.EnumerateDirectories(sourceDir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subDirName = Path.GetFileName(subDirPath);
            var newTargetDir = Path.Combine(targetDir, subDirName);

            await CopyDirectoryContentRecursiveAsync(
                subDirPath,
                newTargetDir,
                skipErrors,
                overwrite,
                bufferSize,
                totalBytesCopiedProgress,
                fileInProgress,
                progress,
                cancellationToken);
        }
    }

    public static async Task DeleteDirectoryAsync(string targetDir, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException("The target directory doesn't exist");

        cancellationToken.ThrowIfCancellationRequested();

        // delete all files
        var files = Directory.EnumerateFiles(targetDir);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Delete(file);
        }

        // delete all dirs recursively
        var subDirs = Directory.EnumerateDirectories(targetDir);
        foreach (var subDir in subDirs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DeleteDirectoryAsync(subDir, cancellationToken);
        }

        // delete current directory
        Directory.Delete(targetDir, false);
    }

    private class CopyProgressState
    {
        public long BytesCopied;
        public string FileNameInProgress = string.Empty;
    }
}
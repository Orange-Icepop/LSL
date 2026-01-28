namespace LSL.Common.Utilities.FileSystem;

public enum FileOverwriteMode
{
    Throw,
    Skip,
    Overwrite
}

public static class FileExtensions
{
    public static async Task CopyFileAsync(string sourcePath,
        string destinationPath,
        FileOverwriteMode overwrite = FileOverwriteMode.Throw,
        int bufferSize = 81920,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 参数验证
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Invalid source file path", nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Invalid destination file path", nameof(destinationPath));
        
        // 处理符号链接
        if (File.GetAttributes(sourcePath).HasFlag(FileAttributes.ReparsePoint))
        {
            var target = File.ResolveLinkTarget(sourcePath, false);
            if (target is null) return;
            switch (target)
            {
                case FileInfo fileInfo:
                    File.CreateSymbolicLink(destinationPath, fileInfo.FullName);
                    break;
                case DirectoryInfo directoryInfo:
                    Directory.CreateSymbolicLink(destinationPath, directoryInfo.FullName);
                    break;
            }
            progress?.Report(0);
            return;
        }

        // 检查源文件是否存在
        if (Directory.Exists(sourcePath)) throw new ArgumentException("The source is a directory, not a file", nameof(sourcePath));
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source file not exists", sourcePath);

        // 处理目标文件覆盖
        if (Directory.Exists(destinationPath)) throw new IOException("A directory of the same name already exists at the destination");
        if (File.Exists(destinationPath))
        {
            switch (overwrite)
            {
                case FileOverwriteMode.Overwrite:
                    File.Delete(destinationPath);
                    break;
                case FileOverwriteMode.Throw:
                    throw new IOException("Target file exists but overwrite disabled");
                case FileOverwriteMode.Skip:
                    return;
            }
        }
        // 使用异步文件流复制
        await using var sourceStream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var destStream = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            FileOptions.Asynchronous);
        await CopyStreamAsync(
            sourceStream,
            destStream,
            bufferSize,
            progress,
            cancellationToken);
    }

    private static async Task CopyStreamAsync(
        FileStream source,
        FileStream destination,
        int bufferSize,
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }
}
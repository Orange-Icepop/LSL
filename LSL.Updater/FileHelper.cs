using ICSharpCode.SharpZipLib.Zip;

namespace LSL.Updater;

internal static class FileHelper
{
    internal static string Move(string baseDir, string targetDir, string fileName)
    {
        Directory.CreateDirectory(targetDir);
        var fin = Path.Combine(targetDir, fileName);
        File.Copy(Path.Combine(baseDir, fileName), fin);
        return fin;
    }

    internal static void CopyDir(string baseDir, string targetDir, IEnumerable<string> excludeFile,
        IEnumerable<string> excludeDirectory, bool subOnly = false)
    {
        var root = new DirectoryInfo(baseDir);
        if (!subOnly) targetDir = Path.Combine(targetDir, root.Name);
        Directory.CreateDirectory(targetDir);
        var ef = new HashSet<string>(excludeFile, StringComparer.OrdinalIgnoreCase);
        var ed = new HashSet<string>(excludeDirectory, StringComparer.OrdinalIgnoreCase);
        foreach (var fileInfo in root.GetFiles())
        {
            if (fileInfo.LinkTarget is not null) continue; // 跳过符号链接文件
            if (fileInfo.Exists && !ef.Contains(fileInfo.Name))
            {
                fileInfo.CopyTo(Path.Combine(targetDir, fileInfo.Name), true);
            }
        }

        foreach (var dirInfo in root.GetDirectories())
        {
            if (dirInfo.LinkTarget is not null) continue; // 跳过符号链接目录
            if (dirInfo.Exists && !ed.Contains(dirInfo.Name))
            {
                CopyDir(Path.Combine(baseDir, dirInfo.Name), Path.Combine(targetDir, dirInfo.Name), [], [], true);
            }
        }
    }

    internal static bool UnZip(string zipFile, string targetDir, string? password = null)
    {
        if (!File.Exists(zipFile)) return false;
        Directory.CreateDirectory(targetDir);
        using var zipStream = new ZipInputStream(File.OpenRead(zipFile));
        if (password != null) zipStream.Password = password;
        while (zipStream.GetNextEntry() is { } entry)
        {
            if (entry.IsDirectory || string.IsNullOrEmpty(entry.Name))
                continue;
            string fullPath = Path.Combine(targetDir, entry.Name.Replace('/', Path.DirectorySeparatorChar));
            string? dirPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dirPath)) Directory.CreateDirectory(dirPath);
            using var fileStream = File.Create(fullPath);
            zipStream.CopyTo(fileStream);
        }

        return true;
    }

    internal static void UpdateFile(string baseDir, string updateFileDir)
    {
        int retry = 3;
        while (retry-- > 0)
        {
            try
            {
                CopyDir(updateFileDir, baseDir, [], ["LSL", "Logs"], true);
                break;
            }
            catch (IOException ex)
            {
                Console.WriteLine("A file error encountered:");
                Console.WriteLine(ex.Message);
                Console.Write("Retry chances left:");
                Console.Write(retry);
                Console.WriteLine();
                Thread.Sleep(500);
            }
        }
    }
}
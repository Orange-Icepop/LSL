using ICSharpCode.SharpZipLib.Zip;

namespace LSL.Common.Utilities;

public static class ZipHelper
{
    public static bool UnZip(string zipFile, string targetDir, string? password = null)
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
}
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LSL.Common.Utilities;

public static partial class MemoryInfo
{
    // 获取系统总内存（跨平台实现）
    public static long GetTotalSystemMemory()
    {
        
        var gcInfo = GC.GetGCMemoryInfo();
        return gcInfo.TotalAvailableMemoryBytes;
        // 额，好像下面这些没用了
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return (long)GetWindowsTotalMemory();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return (long)GetLinuxTotalMemory();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return (long)GetMacOSTotalMemory();
        }
        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    #region 平台特定内存获取实现
    // Windows内存获取
    private static ulong GetWindowsTotalMemory()
    {
        MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };
        return GlobalMemoryStatusEx(ref memStatus) ? memStatus.ullTotalPhys : 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    // Linux内存获取
    private static ulong GetLinuxTotalMemory()
    {
        const string path = "/proc/meminfo";
        if (!File.Exists(path)) return 0;

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("MemTotal:"))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && ulong.TryParse(parts[1], out var totalKb))
                {
                    return totalKb * 1024; // 转换为字节
                }
            }
        }
        return 0;
    }

    // macOS内存获取
    private static ulong GetMacOSTotalMemory()
    {
        const string command = "sysctl -n hw.memsize";
        var output = RunCommand("/bin/bash", $"-c \"{command}\"");
        return ulong.TryParse(output, out var memoryBytes) ? memoryBytes : 0;
    }

    private static string RunCommand(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        return process?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;
    }
    #endregion

}
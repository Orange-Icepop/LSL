using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LSL.Common.Models;
using LSL.Common.Models.Minecraft;
using LSL.Common.Options;

namespace LSL.Common.Utilities.Minecraft;

public static class JavaFinder
{
    public static Task<List<JavaInfo>> GetInstalledJavaInfosAsync()// 异步获取已安装的Java信息-总方法
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsJavaInfos();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxJavaInfos();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return GetMacOSJavaInfos();
        }
        throw new PlatformNotSupportedException();
    }

    private static async Task<List<JavaInfo>> GetWindowsJavaInfos()// 获取Windows系统中的Java信息
    {
        ConcurrentBag<JavaInfo> javaInfos = [];
        var pathEnv = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);// 获取系统环境变量中的Path
        if (pathEnv is null) throw new Exception("Environment variable path not found");
        string[] pathParts = pathEnv.Split(Path.PathSeparator);

        await Parallel.ForEachAsync(pathParts, ConcurrencyOptions.ConcurrencyLimit, async (pathPart, _) =>
        {
            string javaPath = Path.Combine(pathPart, "java.exe");
            string javawPath = Path.Combine(pathPart, "javaw.exe");
            if (File.Exists(javaPath))
            {
                var javaInfo = await GetJavaInfo(javaPath);
                if (javaInfo != null)
                {
                    javaInfos.Add(javaInfo);
                }
            }
            else if (File.Exists(javawPath))
            {
                var javaInfo = await GetJavaInfo(javawPath);
                if (javaInfo != null)
                {
                    javaInfos.Add(javaInfo);
                }
            }
        });
        return javaInfos.ToList();
    }

    private static async Task<List<JavaInfo>> GetLinuxJavaInfos()// 获取Linux系统中的Java信息
    {
        ConcurrentBag<JavaInfo> javaInfos = [];
        string[] jvmDirs = ["/usr/lib/jvm", "/usr/lib32/jvm", "/usr/lib64/jvm"];// 利用Java虚拟机目录查找

        foreach (var jvmDir in jvmDirs)
        {
            if (!Directory.Exists(jvmDir)) continue;
            await Parallel.ForEachAsync(Directory.GetDirectories(jvmDir), ConcurrencyOptions.ConcurrencyLimit,
                async (subDir, _) =>
                {
                    // 查找java可执行文件
                    string javaPath = Path.Combine(subDir, "bin", "java");
                    if (File.Exists(javaPath))
                    {
                        var javaInfo = await GetJavaInfo(javaPath);
                        if (javaInfo != null)
                        {
                            javaInfos.Add(javaInfo);
                        }
                    }
                });
        }
        /*
        // 查找通过包管理器安装的Java
        var whichResult = ExecuteCommand("which", "java");
        if (!string.IsNullOrEmpty(whichResult))
        {
            var javaInfo = await GetJavaInfo(whichResult.Trim());
            if (javaInfo != null)
            {
                javaInfos.Add(javaInfo);
            }
        }*/
        return javaInfos.ToList();
    }

    private static async Task<List<JavaInfo>> GetMacOSJavaInfos()// 获取MacOS系统中的Java信息
    {
        ConcurrentBag<JavaInfo> javaInfos = [];
        const string jvmDir = "/Library/Java/JavaVirtualMachines";// 利用Java虚拟机目录查找

        if (Directory.Exists(jvmDir))
        {
            await Parallel.ForEachAsync(Directory.GetDirectories(jvmDir), ConcurrencyOptions.ConcurrencyLimit,
                async (subDir, _) =>
                {
                    // 查找java可执行文件
                    string javaPath = Path.Combine(subDir, "Contents", "Home", "bin", "java");
                    if (File.Exists(javaPath))
                    {
                        var javaInfo = await GetJavaInfo(javaPath);
                        if (javaInfo != null)
                        {
                            javaInfos.Add(javaInfo);
                        }
                    }
                });
        }

        return javaInfos.ToList();
    }

    public static async Task<JavaInfo?> GetJavaInfo(string javaPath)// 使用java -version获取Java信息
    {
        try
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(output))
            {
                var lines = output.Split([Environment.NewLine], StringSplitOptions.None);
                if (lines.Length > 1)
                {
                    string version = "Unknown";
                    string vendor = "Unknown";

                    // get vendor
                    string vendorLine = lines[1];
                    var vendorParts = vendorLine.Split(' ');
                    if (vendorParts.Length >= 3)
                    {
                        if (vendorLine.Contains("Java(TM) SE Runtime Environment"))
                        {
                            vendor = "Oracle";
                        }
                        if (vendorLine.Contains("OpenJDK Runtime Environment"))
                        {
                            vendor = "OpenJDK";
                        }
                        if (vendorLine.Contains("Temurin"))
                        {
                            vendor = "Adoptium";
                        }
                        if (vendorLine.Contains("Corretto"))
                        {
                            vendor = "Corretto";
                        }
                        if (vendorLine.Contains("IBM"))
                        {
                            vendor = "IBM";
                        }
                        if (vendorLine.Contains("Microsoft"))
                        {
                            vendor = "Microsoft";
                        }
                        if (vendorLine.Contains("Zulu"))
                        {
                            vendor = "Zulu";
                        }
                    }
                    // get version
                    string versionLine = lines[0];
                    var versionParts = versionLine.Split(' ');
                    if (versionParts.Length >= 3)
                    {
                        version = versionParts[2].Trim('"');
                    }
                    if (vendor == "Oracle" && version.StartsWith("1."))
                    {
                        version = version[2..];
                    }
                    // get architecture
                    string architectureLine = lines[2];
                    var architecture = architectureLine.Contains("64-Bit") ? "64-Bit" : "32-Bit";
                    return new JavaInfo(javaPath, version, vendor, architecture);
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ExecuteCommand(string command, string args)
    {
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }
}
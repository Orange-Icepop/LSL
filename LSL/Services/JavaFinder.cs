using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LSL.IPC;

namespace LSL.Services
{
    public class JavaFinder
    {
        public static async Task<List<JavaInfo>> GetInstalledJavaInfosAsync()// 异步获取已安装的Java信息-总方法
        {
            var javaInfos = new List<JavaInfo>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await Task.Run(() => GetWindowsJavaInfos(javaInfos));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await Task.Run(() => GetLinuxJavaInfos(javaInfos));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await Task.Run(() => GetMacOSJavaInfos(javaInfos));
            }

            return javaInfos;
        }

        private static void GetWindowsJavaInfos(List<JavaInfo> javaInfos)// 获取Windows系统中的Java信息
        {
            var pathEnv = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);// 获取系统环境变量中的Path
            if (pathEnv == null) throw new Exception("Environment variable path not found");
            string[] pathParts = pathEnv.Split(Path.PathSeparator);

            foreach (var pathPart in pathParts)
            {
                string javaPath = Path.Combine(pathPart, "java.exe");
                string javawPath = Path.Combine(pathPart, "javaw.exe");
                if (File.Exists(javaPath))
                {
                    var javaInfo = GetJavaInfo(javaPath);
                    if (javaInfo != null)
                    {
                        javaInfos.Add(javaInfo);
                    }
                    continue;
                }
                else if (File.Exists(javawPath))
                {
                    var javaInfo = GetJavaInfo(javawPath);
                    if (javaInfo != null)
                    {
                        javaInfos.Add(javaInfo);
                    }
                    continue;
                }
            }
        }

        private static void GetLinuxJavaInfos(List<JavaInfo> javaInfos)// 获取Linux系统中的Java信息
        {
            string[] jvmDirs = ["/usr/lib/jvm", "/usr/lib32/jvm", "/usr/lib64/jvm"];// 利用Java虚拟机目录查找

            foreach (var jvmDir in jvmDirs)
            {
                if (!Directory.Exists(jvmDir)) continue;
                foreach (var subdir in Directory.GetDirectories(jvmDir))
                {
                    // 查找java可执行文件
                    string javaPath = Path.Combine(subdir, "bin", "java");
                    if (File.Exists(javaPath))
                    {
                        var javaInfo = GetJavaInfo(javaPath);
                        if (javaInfo != null)
                        {
                            javaInfos.Add(javaInfo);
                        }
                    }
                }
            }

            // 查找通过包管理器安装的Java
            var whichResult = ExecuteCommand("which", "java");
            if (!string.IsNullOrEmpty(whichResult))
            {
                var javaInfo = GetJavaInfo(whichResult.Trim());
                if (javaInfo != null)
                {
                    javaInfos.Add(javaInfo);
                }
            }
        }

        private static void GetMacOSJavaInfos(List<JavaInfo> javaInfos)// 获取MacOS系统中的Java信息
        {
            string jvmDir = "/Library/Java/JavaVirtualMachines";// 利用Java虚拟机目录查找

            if (Directory.Exists(jvmDir))
            {
                foreach (var subdir in Directory.GetDirectories(jvmDir))
                {
                    // 查找java可执行文件
                    string javaPath = Path.Combine(subdir, "Contents", "Home", "bin", "java");
                    if (File.Exists(javaPath))
                    {
                        var javaInfo = GetJavaInfo(javaPath);
                        if (javaInfo != null)
                        {
                            javaInfos.Add(javaInfo);
                        }
                    }
                }
            }
        }

        public static JavaInfo? GetJavaInfo(string javaPath)// 使用java -version获取Java信息
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
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    if (lines.Length > 1)
                    {
                        string version = "Unknown";
                        string vendor = "Unknown";
                        string architecture = "Unknown";

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
                            version = version.Substring(2);
                        }
                        // get architecture
                        string architectureLine = lines[2];
                        if (architectureLine.Contains("64-Bit"))
                        {
                            architecture = "64-Bit";
                        }
                        else
                        {
                            architecture = "32-Bit";
                        }
                        return new JavaInfo(javaPath, version, vendor, architecture);
                    }
                    else return null;
                }
                else return null;
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
}

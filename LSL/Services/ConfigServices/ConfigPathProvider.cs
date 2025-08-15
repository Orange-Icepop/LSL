using System;
using System.IO;

namespace LSL.Services.ConfigServices;
/// <summary> The static class to provide paths for logic config processing. </summary>
public static class ConfigPathProvider
{
    public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

    public static readonly string LSLFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL");

    public static readonly string ConfigFilePath = Path.Combine(LSLFolder, "Config.json");

    public static readonly string ServerConfigPath = Path.Combine(LSLFolder, "ServersConfig.json");

    public static readonly string JavaListPath = Path.Combine(LSLFolder, "JavaList.json");

    public static readonly string ServersFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Servers");

    public static bool HasReadWriteAccess(string folderPath)
    {
        // 检查文件夹是否存在
        if (!Directory.Exists(folderPath))
        {
            try
            {
                // 测试写权限
                Directory.CreateDirectory(folderPath);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        // 执行文件读写测试
        string testFilePath = Path.Combine(folderPath, $"permission_test_{Guid.NewGuid()}.tmp");

        try
        {
            // 测试写权限
            File.WriteAllText(testFilePath, "test");

            // 测试读权限
            var cont = File.ReadAllText(testFilePath);
            return cont == "test";
        }
        catch (UnauthorizedAccessException) // 权限不足
        {
            return false;
        }
        catch (IOException) // 其他 I/O 错误
        {
            return false;
        }
        finally
        {
            // 清理测试文件
            try
            {
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);
            }
            catch{}
        }
    }
}

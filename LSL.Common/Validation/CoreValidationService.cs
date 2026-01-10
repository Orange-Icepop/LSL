using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using LSL.Common.Models;
using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Validation;

/// <summary>
/// The specified validator for recognizing different types of server core.
/// </summary>
public static class CoreValidationService
{
    public static ServiceResult<ServerCoreType> Validate(string? filePath)// 校验核心类型
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return ServiceResult.Fail<ServerCoreType>("选定的路径为空");

        }
        if (!File.Exists(filePath))
        {
            return ServiceResult.Fail<ServerCoreType>("选定的文件/路径不存在");
        }
        var jarMainClassResult = GetMainClass(filePath);
        if (jarMainClassResult.IsError)
        {
            return ServiceResult.Fail<ServerCoreType>(jarMainClassResult.Error);
        }

        var type = jarMainClassResult.Result switch
        {
            "net.minecraft.server.MinecraftServer" => ServerCoreType.Vanilla,
            "net.minecraft.bundler.Main" => ServerCoreType.Vanilla,
            "net.minecraft.client.Main" => ServerCoreType.Client,
            "net.minecraftforge.installer.SimpleInstaller" => ServerCoreType.ForgeInstaller,
            "net.fabricmc.installer.Main" => ServerCoreType.FabricInstaller,
            "net.fabricmc.installer.ServerLauncher" => ServerCoreType.Fabric,
            "io.papermc.paperclip.Paperclip" => ServerCoreType.Akarin,
            "io.izzel.arclight.server.Launcher" => ServerCoreType.Arclight,
            "catserver.server.CatServerLaunch" => ServerCoreType.CatServer,
            "foxlaunch.FoxServerLauncher" => ServerCoreType.CatServer,
            "org.bukkit.craftbukkit.Main" => ServerCoreType.CraftBukkit,
            "org.bukkit.craftbukkit.bootstrap.Main" => ServerCoreType.CraftBukkit,
            "io.papermc.paperclip.Main" => ServerCoreType.Paper,
            "org.leavesmc.leavesclip.Main" => ServerCoreType.Leaves,
            "net.md_5.bungee.Bootstrap" => ServerCoreType.LightFall,
            "com.mohistmc.MohistMCStart" => ServerCoreType.Mohist,
            "com.mohistmc.MohistMC" => ServerCoreType.Mohist,
            "com.destroystokyo.paperclip.Paperclip" => ServerCoreType.Paper,
            "com.velocitypowered.proxy.Velocity" => ServerCoreType.Velocity,
            _ => ServerCoreType.Unknown,
        };
        return ServiceResult.Success(type);
    }
    // the following code is taken and modified from https://github.com/Orange-Icepop/JavaMainClassFinder
    public static ServiceResult<string> GetMainClass(string jarFilePath)
    {
        try
        {
            using var stream = new FileStream(jarFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: false);
            using var zipFile = new ZipFile(stream);
            foreach (ZipEntry entry in zipFile)
            {
                if (entry.IsDirectory) continue;
                if (entry.Name != "META-INF/MANIFEST.MF") continue;
                using var fStream = zipFile.GetInputStream(entry);
                if (fStream is null) return ServiceResult.Fail<string>("Unable to read MANIFEST.MF");
                using var reader = new StreamReader(fStream, Encoding.UTF8);
                var manifestContent = reader.ReadToEnd();
                var mc = FindMainClassLine(manifestContent);
                if (mc is null) return ServiceResult.Fail<string>("Cannot find Main-Class property in MANIFEST.MF");
            }

            return ServiceResult.Fail<string>("Cannot find MANIFEST.MF");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ServiceResult.Fail<string>("Access denied: " + ex.Message);
        }
        catch (IOException ex)
        {
            return ServiceResult.Fail<string>("Error reading file: " + ex.Message);
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail<string>("Error reading jar file: " + ex.Message);
        }
    }

    public static string? FindMainClassLine(string manifestContent)
    {
        var lines = manifestContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        return (from line in lines where line.StartsWith("Main-Class:") select line["Main-Class:".Length..].Trim()).FirstOrDefault();
    }
}
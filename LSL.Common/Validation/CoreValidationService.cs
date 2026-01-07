using ICSharpCode.SharpZipLib.Zip;
using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Validation;

/// <summary>
/// The specified validator for recognizing different types of server core.
/// </summary>
public static class CoreValidationService
{

    public static ServerCoreType Validate(string? filePath, out string errorMessage)// 校验核心类型
    {
        errorMessage = "";
        if (string.IsNullOrEmpty(filePath))
        {
            errorMessage = "选定的路径为空";
            return ServerCoreType.Error;

        }
        if (!File.Exists(filePath))
        {
            errorMessage = "选定的文件/路径不存在";
            return ServerCoreType.Error;
        }
        var jarMainClass = GetMainClass(filePath);
        if (jarMainClass == null) return ServerCoreType.Unknown;
        else if (jarMainClass.StartsWith("Access denied") || jarMainClass.StartsWith("Error"))
        {
            errorMessage = jarMainClass;
            return ServerCoreType.Error;
        }
        else
        {
            return jarMainClass switch
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
        }
    }
    // the following code is taken from https://github.com/Orange-Icepop/JavaMainClassFinder
    public static string? GetMainClass(string jarFilePath)
    {
        try
        {
            using var stream = new FileStream(jarFilePath, FileMode.Open);
            using var zipFile = new ZipFile(stream);
            foreach (ZipEntry entry in zipFile)
            {
                if (entry.IsDirectory) continue;
                if (entry.Name != "META-INF/MANIFEST.MF") continue; // 对于较新版本的MC，MANIFEST.MF中应当包含Main-Class字段
                using var fStream = zipFile.GetInputStream(entry);
                if (fStream is null) return null;
                using var reader = new StreamReader(fStream);
                var manifestContent = reader.ReadToEnd();
                return FindMainClassLine(manifestContent);
            }

            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine("Access denied: " + ex.Message);
            return "Access denied: " + ex.Message;
        }
        catch (IOException ex)
        {
            Console.WriteLine("IO error: " + ex.Message);
            return "Error reading file: " + ex.Message;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading jar file: " + ex.Message);
            return "Error reading jar file: " + ex.Message;
        }
    }

    public static string? FindMainClassLine(string manifestContent)
    {
        var lines = manifestContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        return (from line in lines where line.StartsWith("Main-Class:") select line["Main-Class:".Length..].Trim()).FirstOrDefault();
    }
}
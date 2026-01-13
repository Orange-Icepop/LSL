using System.Collections.Frozen;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using LSL.Common.Models;
using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Utilities;

/// <summary>
/// The specified validator for recognizing different types of server core.
/// </summary>
public static class CoreTypeHelper
{
    public static async Task<ServiceResult<ServerCoreType>> GetCoreType(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return ServiceResult.Fail<ServerCoreType>(new ArgumentNullException(nameof(filePath)));
        if (!File.Exists(filePath)) return ServiceResult.Fail<ServerCoreType>(new FileNotFoundException($"Cannot find core file {filePath}"));

        var jarMainClassResult = await GetMainClass(filePath).ConfigureAwait(false);
        if (jarMainClassResult.IsError) return ServiceResult.Fail<ServerCoreType>(jarMainClassResult.Error);
        return ServiceResult.Success(s_coreTypeMap.GetValueOrDefault(jarMainClassResult.Result, ServerCoreType.Unknown));
    }

    // the following code is taken and modified from https://github.com/Orange-Icepop/JavaMainClassFinder
    private static async Task<ServiceResult<string>> GetMainClass(string jarFilePath)
    {
        try
        {
            await using var stream = new FileStream(jarFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920, useAsync: true);
            using var zipFile = new ZipFile(stream);
            var entry = zipFile.GetEntry("META-INF/MANIFEST.MF");
            if (entry is null || entry.IsDirectory) return ServiceResult.Fail<string>("MANIFEST.MF not found");
            await using var fStream = zipFile.GetInputStream(entry);
            if (fStream is null) return ServiceResult.Fail<string>("Unable to read MANIFEST.MF");
            using var reader = new StreamReader(fStream, Encoding.UTF8);
            var manifestContent = await reader.ReadToEndAsync().ConfigureAwait(false);
            var mc = FindMainClassLine(manifestContent);
            return mc is null
                ? ServiceResult.Fail<string>("Cannot find Main-Class property in MANIFEST.MF")
                : ServiceResult.Success(mc);
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

    private static string? FindMainClassLine(string manifestContent)
    {
        var lines = manifestContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        return (from line in lines where line.StartsWith("Main-Class:") select line["Main-Class:".Length..].Trim())
            .FirstOrDefault();
    }

    private static readonly FrozenDictionary<string, ServerCoreType> s_coreTypeMap =
        new Dictionary<string, ServerCoreType>()
        {
            ["net.minecraft.server.MinecraftServer"] = ServerCoreType.Vanilla,
            ["net.minecraft.bundler.Main"] = ServerCoreType.Vanilla,
            ["net.minecraft.client.Main"] = ServerCoreType.Client,
            ["net.minecraftforge.installer.SimpleInstaller"] = ServerCoreType.ForgeInstaller,
            ["net.fabricmc.installer.Main"] = ServerCoreType.FabricInstaller,
            ["net.minecraftforge.bootstrap.shim.Main"] = ServerCoreType.ForgeShim,
            ["net.minecraftforge.fml.relauncher.ServerLaunchWrapper"] = ServerCoreType.OldForge,
            ["net.fabricmc.installer.ServerLauncher"] = ServerCoreType.Fabric,
            ["io.papermc.paperclip.Paperclip"] = ServerCoreType.Akarin,
            ["io.izzel.arclight.server.Launcher"] = ServerCoreType.Arclight,
            ["catserver.server.CatServerLaunch"] = ServerCoreType.CatServer,
            ["foxlaunch.FoxServerLauncher"] = ServerCoreType.CatServer,
            ["org.bukkit.craftbukkit.Main"] = ServerCoreType.CraftBukkit,
            ["org.bukkit.craftbukkit.bootstrap.Main"] = ServerCoreType.CraftBukkit,
            ["io.papermc.paperclip.Main"] = ServerCoreType.Paper,
            ["org.leavesmc.leavesclip.Main"] = ServerCoreType.Leaves,
            ["net.md_5.bungee.Bootstrap"] = ServerCoreType.LightFall,
            ["com.mohistmc.MohistMCStart"] = ServerCoreType.Mohist,
            ["com.mohistmc.MohistMC"] = ServerCoreType.Mohist,
            ["com.destroystokyo.paperclip.Paperclip"] = ServerCoreType.Paper,
            ["com.velocitypowered.proxy.Velocity"] = ServerCoreType.Velocity,
        }.ToFrozenDictionary();
}
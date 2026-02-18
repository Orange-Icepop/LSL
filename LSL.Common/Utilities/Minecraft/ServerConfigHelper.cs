using FluentResults;
using LSL.Common.Models.ServerConfig;

namespace LSL.Common.Utilities.Minecraft;

public static class ServerConfigHelper
{
    /// <summary>
    /// Read the LSL server config of the specified server folder.
    /// </summary>
    /// <param name="path">The server's directory</param>
    /// <param name="v1Read">Determine whether the first version of ServerConfig (lslconfig.json) should be included</param>
    /// <returns></returns>
    public static async Task<Result<LocatedServerConfig>> ReadSingleConfigAsync(string path, bool v1Read = false)
    {
        if (!Directory.Exists(path))
            return Result.Fail<LocatedServerConfig>(new Error("Target server doesn't exist."));
        // try v2
        var confPath = Path.Combine(path, "lsl-configs", ServerConfigV2.ConfigFileName);
        if (File.Exists(confPath))
            return await ServerConfigV2.Deserialize(await File.ReadAllTextAsync(confPath))
                .Bind(config => config.StandardizeAsync(path));


        if (!v1Read)
            return Result.Fail<LocatedServerConfig>(new Error(
                "Target path doesn't contain any server config file of LSL"));
        // try v1
        confPath = Path.Combine(path, "lslconfig.json");
        if (File.Exists(confPath))
            return await ServerConfigV1.Deserialize(await File.ReadAllTextAsync(confPath))
                .Bind(config => config.StandardizeAsync(path));
        return Result.Fail<LocatedServerConfig>(new Error(
            "Target path doesn't contain any server config file of LSL"));
    }
    
}
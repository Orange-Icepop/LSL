using LSL.Common.Models;
using LSL.Common.Models.ServerConfig;

namespace LSL.Common.Utilities;

public static class ServerConfigHelper
{
    /// <summary>
    /// Read the LSL server config of the specified server folder.
    /// </summary>
    /// <param name="path">The server's directory</param>
    /// <param name="v1Read">Determine whether the first version of ServerConfig (lslconfig.json) should be included</param>
    /// <returns></returns>
    public static async Task<ServiceResult<LocatedServerConfig>> ReadSingleConfigAsync(string path, bool v1Read = false)
    {
        if (!Directory.Exists(path))
            return ServiceResult.Fail<LocatedServerConfig>(new ArgumentException("Target server doesn't exist.",
                nameof(path)));
        // try v2
        var confPath = Path.Combine(path, "lsl-configs", ServerConfigV2.ConfigFileName);
        if (File.Exists(confPath))
            return await ServerConfigV2.Deserialize(await File.ReadAllTextAsync(confPath))
                .Then(async config => await config.StandardizeAsync(path));
        
        
        
        if (!v1Read) return ServiceResult.Fail<LocatedServerConfig>(new ArgumentException(
            "Target path doesn't contain any server config file of LSL",
            nameof(path)));
        // try v1
        confPath = Path.Combine(path, "lslconfig.json");
        if (File.Exists(confPath))
            return await ServerConfigV1.Deserialize(await File.ReadAllTextAsync(confPath))
                .Then(async config => await config.StandardizeAsync(path));
        return ServiceResult.Fail<LocatedServerConfig>(new ArgumentException(
            "Target path doesn't contain any server config file of LSL",
            nameof(path)));
    }
    
}
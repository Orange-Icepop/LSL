using System.Text.Json;
using LSL.Common.Exceptions;
using LSL.Common.Models;
using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Utilities;

public static class ServerConfigHelper
{
    /// <summary>
    /// Read the LSL server config of the specified server folder.
    /// </summary>
    /// <param name="path">The server's directory</param>
    /// <param name="ignoreWarnings">When enabled, the method will ignore all the keys that are missing or don't pass validation, using the default value of the config instead.</param>
    /// <returns></returns>
    public static async Task<ServiceResult<PathedServerConfig>> ReadSingleConfigAsync(string path,
        bool ignoreWarnings = false)
    {
        if (!Directory.Exists(path))
            return ServiceResult.Fail<PathedServerConfig>(new ArgumentException("Target path doesn't exist.",
                nameof(path)));
        var confPath = Path.Combine(path, "lslconfig.json");
        if (!File.Exists(confPath))
            return ServiceResult.Fail<PathedServerConfig>(new ArgumentException(
                "Target path doesn't have an lslconfig.json file.",
                nameof(path)));
        return DeserializeConfig(path, JsonDocument.Parse(await File.ReadAllTextAsync(confPath)).RootElement,
            ignoreWarnings);
    }

    private static ServiceResult<PathedServerConfig> DeserializeConfig(string path, JsonElement configRoot,
        bool ignoreWarnings = false)
    {
        if (configRoot.TryGetProperty("version", out var version))
        {
            // config v1 (no version property)
            return ServerConfigV1.TryDeserialize(configRoot, ignoreWarnings, out var result)
                ? ServiceResult.Success(result.WrapPath(path))
                : ServiceResult.Fail<PathedServerConfig>(
                    new ServerConfigUnparsableException("Cannot parse the server config."));
        }

        return version.GetInt32() switch
        {
            2 =>
                // config v2
                ServerConfigV2.TryDeserialize(configRoot, ignoreWarnings, out var result)
                    ? ServiceResult.Success(result.WrapPath(path))
                    : ServiceResult.Fail<PathedServerConfig>(
                        new ServerConfigUnparsableException("Cannot parse the server config.")),
            _ => ServiceResult.Fail<PathedServerConfig>(
                new ServerConfigUnparsableException("Could not ensure the server config's version."))
        };
    }
}
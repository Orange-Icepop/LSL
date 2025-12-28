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
        return await DeserializeConfig(JsonDocument.Parse(await File.ReadAllTextAsync(confPath)).RootElement,
            ignoreWarnings);
    }

    private static async Task<ServiceResult<PathedServerConfig>> DeserializeConfig(JsonElement configRoot,
        bool ignoreWarnings = false)
    {
        if (configRoot.TryGetProperty("version", out var version))
        {
            // config v1
            return ServerConfigV1.TryParse(configRoot, ignoreWarnings, out var result)
                ? ServiceResult.Success(result.Standardize())
                : ServiceResult.Fail<PathedServerConfig>(
                    new ServerConfigUnparsableException("Cannot parse the server config."));
        }

        return ServiceResult.Fail<PathedServerConfig>(
            new ServerConfigUnparsableException("Could not ensure the server config's version."));
    }

    private static PathedServerConfig Standardize(this ServerConfigV1 config)
    {
    }
}
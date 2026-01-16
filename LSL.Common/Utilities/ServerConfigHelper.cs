using System.Text;
using System.Text.Json;
using LSL.Common.Exceptions;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Utilities;

public static class ServerConfigHelper
{
    /// <summary>
    /// Read the LSL server config of the specified server folder.
    /// </summary>
    /// <param name="path">The server's directory</param>
    /// <returns></returns>
    public static async Task<ServiceResult<PathedServerConfig>> ReadSingleConfigAsync(string path)
    {
        if (!Directory.Exists(path))
            return ServiceResult.Fail<PathedServerConfig>(new ArgumentException("Target path doesn't exist.",
                nameof(path)));
        var confPath = Path.Combine(path, "lslconfig.json");
        if (!File.Exists(confPath))
            return ServiceResult.Fail<PathedServerConfig>(new ArgumentException(
                "Target path doesn't have an lslconfig.json file.",
                nameof(path)));
        await using var stream = File.OpenRead(confPath);
        using var doc = await JsonDocument.ParseAsync(stream);
        return await ExplainConfigAsync(path, doc.RootElement);
    }

    private static async Task<ServiceResult<PathedServerConfig>> ExplainConfigAsync(string path, JsonElement configRoot)
    {
        if (!configRoot.TryGetProperty("version", out var version)) return await DeserializeConfigAsync<ServerConfigV1>(path, configRoot);

        return version.GetInt32() switch
        {
            2 => // config v2
                await DeserializeConfigAsync<ServerConfigV2>(path, configRoot),
            _ => ServiceResult.Fail<PathedServerConfig>(
                new ServerConfigUnparsableException("Could not ensure the server config's version."))
        };
    }

    private static async Task<ServiceResult<PathedServerConfig>> DeserializeConfigAsync<T>(string path, JsonElement configRoot) where T : IServerConfig<T>
    {
        var dResult = T.Deserialize(configRoot);
        if (dResult.IsError) return ServiceResult.Fail<PathedServerConfig>(dResult.Error);
        var fResult = await dResult.Result.Standardize(path).CheckAndFixAsync();
        if (fResult.IsError)
            return ServiceResult.Fail<PathedServerConfig>(
                new ServerConfigUnparsableException("Cannot parse the server config.", fResult.Error));
        if (fResult.IsWarning || dResult.IsWarning)
        {
            var warning = new StringBuilder();
            warning.AppendLineIfNotNullOrEmpty(dResult.Error?.Message);
            warning.AppendLineIfNotNullOrEmpty(fResult.Error?.Message);
            return ServiceResult.Warning(fResult.Result, warning.ToString());
        }
        return ServiceResult.Success(fResult.Result);
    }
}
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
    public static async Task<ServiceResult<LocatedServerConfig>> ReadSingleConfigAsync(string path)
    {
        if (!Directory.Exists(path))
            return ServiceResult.Fail<LocatedServerConfig>(new ArgumentException("Target path doesn't exist.",
                nameof(path)));
        var confPath = Path.Combine(path, "lslconfig.json");
        if (!File.Exists(confPath))
            return ServiceResult.Fail<LocatedServerConfig>(new ArgumentException(
                "Target path doesn't have an lslconfig.json file.",
                nameof(path)));
        await using var stream = File.OpenRead(confPath);
        using var doc = await JsonDocument.ParseAsync(stream);
        return await ExplainConfigAsync(path, doc.RootElement);
    }

    private static async Task<ServiceResult<LocatedServerConfig>> ExplainConfigAsync(string path, JsonElement configRoot)
    {
        if (!configRoot.TryGetProperty("configVersion", out var version))
            return await DeserializeConfigAsync<ServerConfigV1>(path, configRoot);

        if (version.ValueKind != JsonValueKind.Number)
            return ServiceResult.Fail<LocatedServerConfig>(
                new JsonException("The configVersion property of this server config is not a number."));

        return version.GetInt32() switch
        {
            1 => await DeserializeConfigAsync<ServerConfigV1>(path, configRoot),
            2 => // config v2
                await DeserializeConfigAsync<ServerConfigV2>(path, configRoot),
            _ => ServiceResult.Fail<LocatedServerConfig>(
                new ServerConfigUnparsableException("Could not ensure the server config's version."))
        };
    }

    private static async Task<ServiceResult<LocatedServerConfig>> DeserializeConfigAsync<T>(string path,
        JsonElement configRoot) where T : IServerConfig<T>
    {
        var dResult = T.Deserialize(configRoot);
        if (dResult.IsError) return ServiceResult.Fail<LocatedServerConfig>(dResult.Error);
        var fResult = await dResult.Result.Standardize(path).CheckAndFixAsync();
        if (fResult.IsError)
            return ServiceResult.Fail<LocatedServerConfig>(
                new ServerConfigUnparsableException("Cannot parse the server config.", fResult.Error));
        if (fResult.IsWarning || dResult.IsWarning)
            return ServiceResult.Warning(fResult.Result,
                new StringBuilder()
                    .AppendLineIfNotNullOrEmpty(dResult.Error?.Message)
                    .AppendLineIfNotNullOrEmpty(fResult.Error?.Message)
                    .ToString());
        return ServiceResult.Success(fResult.Result);
    }
}
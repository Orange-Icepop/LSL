using System.Collections.Immutable;
using FluentResults;
using Tomlyn;

namespace LSL.Common.Models.ServerConfig;

[TomlModel]
public partial record ServerConfigV2 : IServerConfig<ServerConfigV2>
{
    public ServerConfigV2()
    {
        StatusMonitoring = ServerType is not ServerCoreType.Mohist;
    }

    public string? Name { get; init; } = string.Empty;
    public ServerCoreType? ServerType { get; init; } = ServerCoreType.Error;

    public CommonCoreConfigV1? CommonCoreInfo { get; init; }
    public ForgeCoreConfigV1? ForgeCoreInfo { get; init; }
    public string? JavaPath { get; init; }
    public uint? MinMemory { get; init; }
    public uint? MaxMemory { get; init; }
    public ImmutableList<string>? ExtraJvmArgs { get; init; }
    public bool? StatusMonitoring { get; init; }

    public static Result<ServerConfigV2> Deserialize(string content)
    {
        return Toml.TryToModel<ServerConfigV2>(content, out var config, out var error)
            ? Result.Ok(config)
            : Result.Fail<ServerConfigV2>(error.ToString());
    }

    public Task<Result<LocatedServerConfig>> StandardizeAsync(string path)
    {
        return LocatedServerConfig.CreateAsync(path,
            Name, ServerType, CommonCoreInfo, ForgeCoreInfo, JavaPath, MinMemory, MaxMemory, ExtraJvmArgs,
            StatusMonitoring);
    }

    public string Serialize()
    {
        return Toml.FromModel(this);
    }

    public static string ConfigFileName => "lsl-config-v2.toml";

    public static bool Exists(string path)
    {
        return File.Exists(Path.Combine(path, ConfigFileName));
    }

    public async Task<Result> WriteToFileAsync(string path)
    {
        try
        {
            await File.WriteAllTextAsync(Path.Combine(path, ConfigFileName), Serialize());
            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail(new Error($"An error occured while writing {nameof(ServerConfigV2)}.").CausedBy(e));
        }
    }

    public static async Task<Result<LocatedServerConfig>> ReadFromFileAsync(string path)
    {
        try
        {
            var context = await File.ReadAllTextAsync(Path.Combine(path, ConfigFileName));
            return await Deserialize(context).Bind(config => config.StandardizeAsync(path));
        }
        catch (Exception e)
        {
            return Result.Fail<LocatedServerConfig>(new Error($"An error occured while reading {nameof(ServerConfigV2)}.").CausedBy(e));
        }
    }
}
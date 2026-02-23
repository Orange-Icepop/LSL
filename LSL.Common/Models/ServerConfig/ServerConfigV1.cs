using System.Text.Json;
using FluentResults;
using LSL.Common.Options;
using Tomlyn;

namespace LSL.Common.Models.ServerConfig;

public record ServerConfigV1 : IServerConfig<ServerConfigV1>
{
    private ServerConfigV1()
    {
    }

    public string? Name { get; init; }
    public string? UsingJava { get; init; }
    public string? CoreName { get; init; }
    public uint? MinMemory { get; init; }
    public uint? MaxMemory { get; init; }
    public string? ExtJvm { get; init; }

    public static string ConfigFileName => "lslconfig.json";

    public static Result<ServerConfigV1> Deserialize(string json)
    {
        var result = new ServerConfigV1();
        try
        {
            result = JsonSerializer.Deserialize(json, SnakeJsonOptions.Default.ServerConfigV1) ?? result;
        }
        catch (Exception e)
        {
            return Result.Fail<ServerConfigV1>(new Error($"An error occured while deserializing {nameof(ServerConfigV1)}").CausedBy(e));
        }

        return Result.Ok(result);
    }

    public Task<Result<LocatedServerConfig>> StandardizeAsync(string path)
    {
        return LocatedServerConfig.CreateAsync(path, Name, ServerCoreType.Error,
            new CommonCoreConfigV1 { JarName = CoreName ?? string.Empty }, null, UsingJava, MinMemory, MaxMemory,
            [..ExtJvm?.Split(' ') ?? []], true);
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, SnakeJsonOptions.Default.ServerConfigV1);
    }

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
            return Result.Fail(new Error($"An error occured while writing {nameof(ServerConfigV1)}").CausedBy(e));
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
            return Result.Fail<LocatedServerConfig>(new Error($"An error occured while reading {nameof(ServerConfigV1)}").CausedBy(e));
        }
    }

    public static ServerConfigV1 Create(string serverName, string usingJava, string coreName, uint minMemory,
        uint maxMemory,
        string extJvm)
    {
        return new ServerConfigV1
        {
            Name = serverName,
            UsingJava = usingJava,
            CoreName = coreName,
            MinMemory = minMemory,
            MaxMemory = maxMemory,
            ExtJvm = extJvm
        };
    }
}
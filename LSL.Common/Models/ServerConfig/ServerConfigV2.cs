using LSL.Common.Results;
using Tomlyn;


namespace LSL.Common.Models.ServerConfig;

public class ServerConfigV2 : IServerConfig<ServerConfigV2>
{
    public ServerConfigV2()
    {
        EnablePreLaunchProtection = ServerType is not ServerCoreType.Mohist;
    }
    public string? Name { get; set; } = string.Empty;
    public ServerCoreType? ServerType { get; set; } = ServerCoreType.Error;
    
    public CommonCoreConfigV1? CommonCoreInfo { get; set; }
    public ForgeCoreConfigV1? ForgeCoreInfo { get; set; }
    public string? JavaPath { get; set; }
    public uint? MinMemory { get; set; }
    public uint? MaxMemory { get; set; }
    public List<string>? ExtraJvmArgs { get; set; }
    public bool? EnablePreLaunchProtection { get; set; }

    public static Result<ServerConfigV2> Deserialize(string content)
    {
        return Toml.TryToModel<ServerConfigV2>(content, out var config, out var error)
            ? Result.Success(config)
            : Result.Fail<ServerConfigV2>(error.ToString());
    }

    public Task<Result<LocatedServerConfig>> StandardizeAsync(string path) => LocatedServerConfig.CreateAsync(path,
        Name, ServerType, CommonCoreInfo, ForgeCoreInfo, JavaPath, MinMemory, MaxMemory, ExtraJvmArgs,
        EnablePreLaunchProtection);

    public string Serialize() => Toml.FromModel(this);
    
    public static string ConfigFileName => "lsl-config-v2.toml";
        
    public static bool Exists(string path) => File.Exists(Path.Combine(path, ConfigFileName));
    
    public async Task<Result> WriteToFileAsync(string path)
    {
        try
        {
            await File.WriteAllTextAsync(Path.Combine(path, ConfigFileName), Serialize());
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Fail(e);
        }
    }

    public static async Task<Result<LocatedServerConfig>> ReadFromFileAsync(string path)
    {
        try
        {
            var context = await File.ReadAllTextAsync(Path.Combine(path, ConfigFileName));
            return await Deserialize(context).BindAsync(config => config.StandardizeAsync(path));
        }
        catch (Exception e)
        {
            return Result.Fail<LocatedServerConfig>(e);
        }
    }
}
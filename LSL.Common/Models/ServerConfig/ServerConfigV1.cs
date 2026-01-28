using System.Text.Json;
using LSL.Common.Options;

namespace LSL.Common.Models.ServerConfig;


public class ServerConfigV1 : IServerConfig<ServerConfigV1>
{
    private ServerConfigV1() { }
    public string? Name { get; set; }
    public string? UsingJava { get; set; }
    public string? CoreName { get; set; }
    public uint? MinMemory { get; set; }
    public uint? MaxMemory { get; set; }
    public string? ExtJvm { get; set; }

    public static string ConfigFileName => "lslconfig.json";

    public static ServerConfigV1 Create(string serverName, string usingJava, string coreName, uint minMemory,
        uint maxMemory,
        string extJvm) => new()
    {
        Name = serverName,
        UsingJava = usingJava,
        CoreName = coreName,
        MinMemory = minMemory,
        MaxMemory = maxMemory,
        ExtJvm = extJvm
    };

    public static ServiceResult<ServerConfigV1> Deserialize(string json)
    {
        var result = new ServerConfigV1();
        try
        {
            result = JsonSerializer.Deserialize(json, SnakeJsonOptions.Default.ServerConfigV1) ?? result;
        }
        catch (Exception e)
        {
            return ServiceResult.Fail<ServerConfigV1>(e);
        }
        return ServiceResult.Success(result);
    }

    public Task<ServiceResult<LocatedServerConfig>> StandardizeAsync(string path) => LocatedServerConfig.CreateAsync(path, Name, ServerCoreType.Error,
        new CommonCoreConfigV1 { JarName = CoreName ?? string.Empty }, null, UsingJava, MinMemory, MaxMemory,
        [..ExtJvm?.Split(' ') ?? []], true);

    public string Serialize() => JsonSerializer.Serialize(this, SnakeJsonOptions.Default.ServerConfigV1);
    
    public static bool Exists(string path) => File.Exists(Path.Combine(path, ConfigFileName));
    
    public async Task<ServiceResult> WriteToFileAsync(string path)
    {
        try
        {
            await File.WriteAllTextAsync(Path.Combine(path, ConfigFileName), Serialize());
            return ServiceResult.Success();
        }
        catch (Exception e)
        {
            return ServiceResult.Fail(e);
        }
    }

    public static async Task<ServiceResult<LocatedServerConfig>> ReadFromFileAsync(string path)
    {
        try
        {
            var context = await File.ReadAllTextAsync(Path.Combine(path, ConfigFileName));
            return await Deserialize(context).BindAsync(config => config.StandardizeAsync(path));
        }
        catch (Exception e)
        {
            return ServiceResult.Fail<LocatedServerConfig>(e);
        }
    }
}
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LSL.Common.Models.ServerConfig;


public class ServerConfigV1 : IServerConfig<ServerConfigV1>
{
    private ServerConfigV1() { }
    public int ConfigVersion => 1;
    public string? Name { get; set; }
    public string? UsingJava { get; set; }
    public string? CoreName { get; set; }
    public uint? MinMemory { get; set; }
    public uint? MaxMemory { get; set; }
    public string? ExtJvm { get; set; }


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

    private static readonly JsonSerializerOptions s_snakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static ServiceResult<ServerConfigV1> Deserialize(string json)
    {
        var result = new ServerConfigV1();
        try
        {
            result = JsonSerializer.Deserialize<ServerConfigV1>(json, s_snakeCaseOptions) ?? result;
        }
        catch (JsonException e)
        {
            return ServiceResult.Fail<ServerConfigV1>(e);
        }
        return ServiceResult.Success(result);
    }

    public Task<ServiceResult<LocatedServerConfig>> StandardizeAsync(string path) => LocatedServerConfig.CreateAsync(path, Name, ServerCoreType.Error,
        new CommonCoreConfigV1 { JarName = CoreName ?? string.Empty }, null, UsingJava, MinMemory, MaxMemory,
        [..ExtJvm?.Split(' ') ?? []], true);

    public string Serialize() => JsonSerializer.Serialize(this, s_snakeCaseOptions);
}
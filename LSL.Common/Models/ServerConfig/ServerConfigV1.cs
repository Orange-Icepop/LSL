using LSL.Common.Utilities;
using Newtonsoft.Json;

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

    public static ServerConfigV1 Deserialize(string json) =>
        JsonConvert.DeserializeObject<ServerConfigV1>(json, NsJsonOptions.SnakeCaseOptions) ?? new ServerConfigV1();

    public Task<ServiceResult<LocatedServerConfig>> StandardizeAsync(string path) => LocatedServerConfig.CreateAsync(path, Name, ServerCoreType.Error,
        new CommonCoreConfigV1 { JarName = CoreName ?? string.Empty }, null, UsingJava, MinMemory, MaxMemory,
        [..ExtJvm?.Split(' ') ?? []], true);

    public string Serialize() => JsonConvert.SerializeObject(this, NsJsonOptions.SnakeCaseOptions);
}
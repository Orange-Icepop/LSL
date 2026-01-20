using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LSL.Common.Models.ServerConfigs;

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

    [JsonIgnore] private static readonly JsonSerializerSettings s_serializerOptions = new()
    {
        Formatting = Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        Error = (_, args) => args.ErrorContext.Handled = true,
    };

    public static ServerConfigV1 Deserialize(string json) =>
        JsonConvert.DeserializeObject<ServerConfigV1>(json, s_serializerOptions) ?? new ServerConfigV1();

    public Task<ServiceResult<PathedServerConfig>> StandardizeAsync(string path) => PathedServerConfig.CreateAsync(path, Name, ServerCoreType.Error,
        new CommonCoreConfigV1 { JarName = CoreName ?? string.Empty }, null, UsingJava, MinMemory, MaxMemory,
        ExtJvm?.Split(' '), true);

    public string Serialize() => JsonConvert.SerializeObject(this, s_serializerOptions);
}
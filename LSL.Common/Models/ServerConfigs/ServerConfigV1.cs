using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static LSL.Common.Extensions.JsonPropertyExtensions;

namespace LSL.Common.Models.ServerConfigs;

public class ServerConfigV1 : IServerConfig<ServerConfigV1>
{
    private ServerConfigV1() { }
    public int ConfigVersion => 1;
    public string Name { get; set; } = string.Empty;
    public string UsingJava { get; set; } = string.Empty;
    public string CoreName { get; set; } = string.Empty;
    public uint MinMemory { get; set; } = 1024;
    public uint MaxMemory { get; set; } = 4096;
    public string ExtJvm { get; set; } = string.Empty;


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

    [JsonIgnore] public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        NumberHandling = JsonNumberHandling.Strict,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public static ServiceResult<ServerConfigV1> Deserialize(JsonElement configRoot)
    {
        var result = new ServerConfigV1();
        List<string> warnings = [];
        Action<string> onError = s => warnings.Add(s);
        bool coreDetectable = true;
        configRoot.ParseStringProperty(nameof(CoreName),
            s => result.CoreName = s,
            _ => coreDetectable = false,
            keyNamingPolicy: JsonKnownNamingPolicy.SnakeCaseLower);
        if (!coreDetectable) return ServiceResult.Fail<ServerConfigV1>("core_name is missing");

        configRoot.ParseStringProperty(nameof(Name),
            s => result.Name = s,
            s =>
            {
                result.Name = "Nameless Server";
                warnings.Add(s);
            },
            keyNamingPolicy: JsonKnownNamingPolicy.SnakeCaseLower);

        configRoot.ParseJavaProperty(nameof(UsingJava),
            s => result.UsingJava = s,
            onError,
            keyNamingPolicy: JsonKnownNamingPolicy.SnakeCaseLower);

        configRoot.ParseUIntProperty(nameof(MinMemory),
            u => result.MinMemory = u,
            onError,
            keyNamingPolicy: JsonKnownNamingPolicy.SnakeCaseLower);

        configRoot.ParseUIntProperty(nameof(MaxMemory),
            u => result.MaxMemory = u,
            onError,
            keyNamingPolicy: JsonKnownNamingPolicy.SnakeCaseLower);

        configRoot.ParseStringProperty(nameof(ExtJvm),
            s => result.ExtJvm = s,
            _ => result.ExtJvm = "-Dlog4j2.formatMsgNoLookups=true",
            enableEmpty: true,
            keyNamingPolicy: JsonKnownNamingPolicy.SnakeCaseLower);

        return warnings.Count != 0
            ? ServiceResult.Warning(result, new StringBuilder().AppendJoin('\n', warnings).ToString())
            : ServiceResult.Success(result);
    }

    public PathedServerConfig Standardize(string path) => new(path, Name, ServerCoreType.Error,
        new CommonCoreConfigV1 { JarName = CoreName }, null, UsingJava, MinMemory, MaxMemory,
        ExtJvm.Split(' '), true);

    public string Serialize() => JsonSerializer.Serialize(this, SerializerOptions);
}
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static LSL.Common.Extensions.JsonPropertyExtensions;

namespace LSL.Common.Models.ServerConfigs;

public class ServerConfigV1 : IServerConfig<ServerConfigV1>
{
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
        configRoot.ParseStringProperty("core_name",
            onSuccess: s => result.CoreName = s,
            onFail: _ => coreDetectable = false);
        if (!coreDetectable) return ServiceResult.Fail<ServerConfigV1>("core_name is missing");

        configRoot.ParseStringProperty("name",
            onSuccess: s => result.Name = s,
            onFail: s =>
            {
                result.Name = "Nameless Server";
                warnings.Add(s);
            });

        configRoot.ParseJavaProperty("using_java",
            onSuccess: s => result.UsingJava = s,
            onFail: onError);

        configRoot.ParseUIntProperty("min_memory",
            onSuccess: u => result.MinMemory = u,
            onFail: onError);

        configRoot.ParseUIntProperty("max_memory",
            onSuccess: u => result.MaxMemory = u,
            onFail: onError);

        configRoot.ParseStringProperty("ext_jvm",
            onSuccess: s => result.ExtJvm = s,
            onFail: _ => result.ExtJvm = "-Dlog4j2.formatMsgNoLookups=true",
            enableEmpty: true);

        if (warnings.Count > 0)
            return ServiceResult.Warning(result,
                new StringBuilder().AppendJoin('\n', warnings).ToString());
        return ServiceResult.Success(result);
    }

    public PathedServerConfig Standardize(string path) => new(path, Name, ServerCoreType.Error,
        new CommonCoreConfigV1 { JarName = CoreName }, null, UsingJava, MinMemory, MaxMemory,
        ExtJvm.Split(' '), true);

    public string Serialize() => JsonSerializer.Serialize(this, SerializerOptions);
}
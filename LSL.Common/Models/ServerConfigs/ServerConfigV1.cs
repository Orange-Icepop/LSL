using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static LSL.Common.Utilities.Json.JsonPropertyValidationHelper;

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

    public static ServiceResult<ServerConfigV1> Deserialize(JsonElement configRoot, bool ignoreWarnings)
    {
        var result = new ServerConfigV1();
        List<string> warnings = [];
        Action<string> onError = s => warnings.Add(s);
        
        StringHandler(onSuccess: s => result.Name = s).Invoke(configRoot, "name", onError);
        JavaHandler(onSuccess: s => result.UsingJava = s).Invoke(configRoot, "using_java", onError);
        StringHandler(onSuccess: s => result.CoreName = s).Invoke(configRoot, "core_name", onError);
        UIntHandler(onSuccess: u => result.MinMemory = u ).Invoke(configRoot, "min_memory", onError);
        UIntHandler(onSuccess: u => result.MaxMemory = u ).Invoke(configRoot, "max_memory", onError);
        StringHandler(onSuccess: s => result.ExtJvm = s, enableEmpty:true).Invoke(configRoot, "ext_jvm", _ => result.ExtJvm = "-Dlog4j2.formatMsgNoLookups=true");

        if (result.MinMemory > result.MaxMemory) warnings.Add("Minimum memory shouldn't be greater than maximum memory");
        
        if (warnings.Count > 0)
            return ignoreWarnings
                ? ServiceResult.Warning(result,
                    new StringBuilder().AppendJoin('\n', warnings).ToString())
                : ServiceResult.Fail<ServerConfigV1>(
                    new StringBuilder().AppendJoin('\n', warnings).ToString());
        return ServiceResult.Success(result);
    }

    public PathedServerConfig Standardize(string path) => new(path, Name, UsingJava, CoreName, MinMemory, MaxMemory, ExtJvm);

    public string Serialize() => JsonSerializer.Serialize(this, SerializerOptions);
}
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using LSL.Common.Utilities.Json;
using Newtonsoft.Json;
using static LSL.Common.Extensions.JsonPropertyExtensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace LSL.Common.Models.ServerConfigs;

public class ServerConfigV2 : IServerConfig<ServerConfigV2>
{
    private ServerConfigV2()
    {
        EnablePreLaunchProtection = ServerType is not ServerCoreType.Mohist;
    }

    public int ConfigVersion { get; } = 2;
    public string Name { get; set; } = string.Empty;
    public ServerCoreType ServerType { get; set; } = ServerCoreType.Error;

    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(ForgeCoreInfo))]
    private bool IsForge => ServerType is ServerCoreType.Forge;
    
    public CommonCoreConfigV1? CommonCoreInfo { get; set; } = null;
    public ForgeCoreConfigV1? ForgeCoreInfo { get; set; } = null;
    public string JavaPath { get; set; } = string.Empty;
    public uint MinMemory { get; set; } = 1024;
    public uint MaxMemory { get; set; } = 4096;
    public string[] ExtraJvmArgs { get; set; } = [];
    public bool EnablePreLaunchProtection { get; set; } = true;

    public static ServiceResult<ServerConfigV2> Deserialize(JsonElement configRoot)
    {
        var result = new ServerConfigV2();
        List<string> warnings = [];
        Action<string> onError = s => warnings.Add(s);

        configRoot.ParseStringProperty("name",
            onSuccess: s => result.Name = s,
            onFail: onError);
        
        configRoot.ParseEnumProperty<ServerCoreType>("serverType",
            onSuccess: t => result.ServerType = t,
            onFail: null);
        
        if(configRoot.TryGetProperty("commonCoreInfo", out var coreInfo)) result.CommonCoreInfo = CommonCoreConfigV1.Deserialize(coreInfo).Result;
        if(configRoot.TryGetProperty("forgeCoreInfo", out var forgeInfo)) result.ForgeCoreInfo = ForgeCoreConfigV1.Deserialize(forgeInfo).Result;
        
        configRoot.ParseJavaProperty("javaPath",
            onSuccess: s => result.JavaPath = s,
            onFail: onError);

        configRoot.ParseUIntProperty("minMemory",
            onSuccess: u => result.MinMemory = u,
            onFail: onError);

        configRoot.ParseUIntProperty("maxMemory",
            onSuccess: u => result.MaxMemory = u,
            onFail: onError);

        configRoot.ParseStringArrayProperty("extraJvmArgs",
            onSuccess: s => result.ExtraJvmArgs = s,
            onFail: _ => result.ExtraJvmArgs = ["-Dlog4j2.formatMsgNoLookups=true"],
            ignoreEmpty: true);

        configRoot.ParseBoolProperty("enablePreLaunchProtection",
            onSuccess: b => result.EnablePreLaunchProtection = b,
            onFail: _ => result.EnablePreLaunchProtection = result.ServerType is not ServerCoreType.Mohist);

        if (warnings.Count > 0)
            return ServiceResult.Warning(result,
                    new StringBuilder().AppendJoin('\n', warnings).ToString());
        return ServiceResult.Success(result);
    }

    public PathedServerConfig Standardize(string path) => new(path, Name, ServerType, CommonCoreInfo, ForgeCoreInfo, JavaPath, MinMemory, MaxMemory, ExtraJvmArgs, EnablePreLaunchProtection);

    public string Serialize() => JsonSerializer.Serialize(this, ConfigSerializerOptions.Default);
}
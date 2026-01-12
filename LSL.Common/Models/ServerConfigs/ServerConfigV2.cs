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
    public string UsingJava { get; set; } = string.Empty;
    public string CoreName { get; set; } = string.Empty;
    public uint MinMemory { get; set; } = 1024;
    public uint MaxMemory { get; set; } = 4096;
    public string[] ExtJvm { get; set; } = [];

    public bool EnablePreLaunchProtection { get; set; } = true;

    public ServerCoreType ServerType { get; set; } = ServerCoreType.Error;

    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(ForgeInfo))]
    private bool IsForge => ServerType is ServerCoreType.Forge;

    public ForgeConfigV1? ForgeInfo { get; set; } = null;

    public static ServiceResult<ServerConfigV2> Deserialize(JsonElement configRoot)
    {
        var result = new ServerConfigV2();
        List<string> warnings = [];
        Action<string> onError = s => warnings.Add(s);
        bool coreDetectable = true;
        configRoot.ParseStringProperty("coreName", 
            onSuccess: s => result.CoreName = s, 
            onFail: _ => coreDetectable = false);
        if (!coreDetectable) return ServiceResult.Fail<ServerConfigV2>("coreName is missing");

        configRoot.ParseStringProperty("name",
            onSuccess: s => result.Name = s,
            onFail: onError);

        configRoot.ParseJavaProperty("usingJava",
            onSuccess: s => result.UsingJava = s,
            onFail: onError);

        configRoot.ParseUIntProperty("minMemory",
            onSuccess: u => result.MinMemory = u,
            onFail: onError);

        configRoot.ParseUIntProperty("maxMemory",
            onSuccess: u => result.MaxMemory = u,
            onFail: onError);

        configRoot.ParseStringArrayProperty("extJvm",
            onSuccess: s => result.ExtJvm = s,
            onFail: _ => result.ExtJvm = ["-Dlog4j2.formatMsgNoLookups=true"],
            ignoreEmpty: true);

        configRoot.ParseEnumProperty<ServerCoreType>("serverType",
            onSuccess: t => result.ServerType = t,
            onFail: _ => { });

        configRoot.ParseBoolProperty("enablePreLaunchProtection",
            onSuccess: b => result.EnablePreLaunchProtection = b,
            onFail: _ => result.EnablePreLaunchProtection = result.ServerType is not ServerCoreType.Mohist);
        if(configRoot.TryGetProperty("forgeInfo", out var forgeInfo)) result.ForgeInfo = ForgeConfigV1.Deserialize(forgeInfo).Result;

        if (warnings.Count > 0)
            return ServiceResult.Warning(result,
                    new StringBuilder().AppendJoin('\n', warnings).ToString());
        return ServiceResult.Success(result);
    }

    public PathedServerConfig Standardize(string path) => new(path, Name, UsingJava, CoreName, MinMemory, MaxMemory, ExtJvm, EnablePreLaunchProtection, ServerType, ForgeInfo);

    public string Serialize() => JsonSerializer.Serialize(this, ConfigSerializerOptions.Default);
}
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using LSL.Common.Utilities.Json;
using Newtonsoft.Json;
using static LSL.Common.Utilities.Json.JsonPropertyValidationHelper;
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
        StringHandler(onSuccess: s => result.CoreName = s).Invoke(configRoot, "coreName", _ => coreDetectable = false);
        if (!coreDetectable) return ServiceResult.Fail<ServerConfigV2>("coreName is missing");
        
        StringHandler(onSuccess: s => result.Name = s).Invoke(configRoot, "name", onError);
        JavaHandler(onSuccess: s => result.UsingJava = s).Invoke(configRoot, "usingJava", onError);
        UIntHandler(onSuccess: u => result.MinMemory = u ).Invoke(configRoot, "minMemory", onError);
        UIntHandler(onSuccess: u => result.MaxMemory = u ).Invoke(configRoot, "maxMemory", onError);
        StringArrayHandler(onSuccess: s => result.ExtJvm = s, ignoreEmpty:true).Invoke(configRoot, "extJvm", _ => result.ExtJvm =
            ["-Dlog4j2.formatMsgNoLookups=true"]);
        EnumHandler<ServerCoreType>(onSuccess: t => result.ServerType = t).Invoke(configRoot, "serverType");
        BoolHandler(onSuccess: b => result.EnablePreLaunchProtection = b).Invoke(configRoot, "enablePreLaunchProtection", _ => result.EnablePreLaunchProtection = result.ServerType is not ServerCoreType.Mohist);
        if(configRoot.TryGetProperty("forgeInfo", out var forgeInfo)) result.ForgeInfo = ForgeConfigV1.Deserialize(forgeInfo).Result;

        if (warnings.Count > 0)
            return ServiceResult.Warning(result,
                    new StringBuilder().AppendJoin('\n', warnings).ToString());
        return ServiceResult.Success(result);
    }

    public PathedServerConfig Standardize(string path) => new(path, Name, UsingJava, CoreName, MinMemory, MaxMemory, ExtJvm, EnablePreLaunchProtection, ServerType, ForgeInfo);

    public string Serialize() => JsonSerializer.Serialize(this, ConfigSerializerOptions.Default);
}
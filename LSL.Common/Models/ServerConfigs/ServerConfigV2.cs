using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Newtonsoft.Json;

namespace LSL.Common.Models.ServerConfigs;

public class ServerConfigV2 : IServerConfig<ServerConfigV2>
{
    private ServerConfigV2()
    {
        EnablePreLaunchProtection = ServerType != ServerCoreType.Mohist;
    }

    public int ConfigVersion { get; } = 2;
    public string Name { get; set; } = string.Empty;
    public string UsingJava { get; set; } = string.Empty;
    public string CoreName { get; set; } = string.Empty;
    public uint MinMemory { get; set; } = 1024;
    public uint MaxMemory { get; set; } = 4096;
    public string ExtJvm { get; set; } = string.Empty;

    public bool EnablePreLaunchProtection { get; set; } = true;

    public ServerCoreType ServerType { get; set; } = ServerCoreType.Unknown;

    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(ForgeInfo))]
    private bool IsForge => ServerType == ServerCoreType.Forge;

    public ForgeConfigV1? ForgeInfo { get; set; } = null;

    public static ServiceResult<ServerConfigV2> Deserialize(JsonElement configRoot, bool ignoreWarnings)
    {
    }

    public PathedServerConfig WrapPath(string path)
    {
    }

    public string Serialize()
    {
    }
}
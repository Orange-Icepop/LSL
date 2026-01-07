using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Newtonsoft.Json;

namespace LSL.Common.Models.ServerConfigs;

public class ServerConfigV2 : IServerConfig<ServerConfigV2>
{
    private ServerConfigV2()
    {
        EnablePreLaunchProtection = ServerType == ServerCoreType.Mohist;
        IsForge = ServerType == ServerCoreType.Forge;
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
    public bool IsForge { get; set; } = false;

    [JsonIgnore] public ForgeConfig? ForgeInfo { get; set; } = null;
    public static bool TryDeserialize(JsonElement configRoot, bool ignoreWarnings, [NotNullWhen(true)] out ServerConfigV2? result)
    {
        
    }

    public PathedServerConfig WrapPath(string path)
    {
        
    }

    public string Serialize()
    {
        
    }
}
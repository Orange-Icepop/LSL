using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Newtonsoft.Json;

namespace LSL.Common.Models.ServerConfigs;

public class ServerConfigV2 : IServerConfig<ServerConfigV2>
{
    public int ConfigVersion { get; } = 2;
    public string Name { get; set; } = string.Empty;
    public string UsingJava { get; set; } = string.Empty;
    public string CoreName { get; set; } = string.Empty;
    public uint MinMemory { get; set; } = 1024;
    public uint MaxMemory { get; set; } = 4096;
    public string ExtJvm { get; set; } = string.Empty;

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
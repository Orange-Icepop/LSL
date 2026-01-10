using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace LSL.Common.Models.ServerConfigs;

public class PathedServerConfig(
    string serverPath,
    string serverName,
    string usingJava,
    string coreName,
    uint minMemory,
    uint maxMemory,
    string extJvm,
    bool enablePreLaunchProtection,
    ServerCoreType serverType,
    ForgeConfigV1? forgeInfo)
{
    public string ServerPath { get; set; } = serverPath;
    public string ServerName { get; set; } = serverName;
    public string UsingJava { get; set; } = usingJava;
    public string CoreName { get; set; } = coreName;
    public uint MinMemory { get; set; } = minMemory;
    public uint MaxMemory { get; set; } = maxMemory;
    public string ExtJvm { get; set; } = extJvm;
    public bool EnablePreLaunchProtection { get; set; } = enablePreLaunchProtection;
    public ServerCoreType ServerType { get; set; } = serverType;

    [MemberNotNullWhen(true, nameof(ForgeInfo))]
    public bool IsForge => ServerType == ServerCoreType.Forge;
    public ForgeConfigV1? ForgeInfo { get; set; } = forgeInfo;

    public static PathedServerConfig Empty =>
        new(string.Empty, string.Empty, string.Empty, string.Empty, 1024, 4096, string.Empty, true,
            ServerCoreType.Unknown, null);

    public IndexedServerConfig AsIndexed(int serverId) => new(serverId, this);
}
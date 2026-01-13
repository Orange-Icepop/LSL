using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace LSL.Common.Models.ServerConfigs;

public class IndexedServerConfig(int serverId, PathedServerConfig pathedConfig)
{
    public PathedServerConfig PathedConfig { get; private set; } = pathedConfig;
    public int ServerId { get; set; } = serverId;
    public string ServerPath => PathedConfig.ServerPath;
    public string ServerName => PathedConfig.ServerName;
    public ServerCoreType ServerType => PathedConfig.ServerType;

    [MemberNotNullWhen(true, nameof(ForgeCoreInfo))]
    public bool IsForge => PathedConfig.IsForge;

    public CommonCoreConfigV1? CommonCoreInfo => PathedConfig.CommonCoreInfo;
    public ForgeCoreConfigV1? ForgeCoreInfo => PathedConfig.ForgeCoreInfo;
    public string UsingJava => PathedConfig.UsingJava;
    public uint MinMemory => PathedConfig.MinMemory;
    public uint MaxMemory => PathedConfig.MaxMemory;
    public List<string> ExtJvm => PathedConfig.ExtJvm;
    public bool EnablePreLaunchProtection => PathedConfig.EnablePreLaunchProtection;

    public IndexedServerConfig(IndexedServerConfig config) // 深拷贝构造函数
        : this(config.ServerId, config.ServerPath, config.ServerName, config.ServerType, config.CommonCoreInfo, config.ForgeCoreInfo, config.UsingJava,
            config.MinMemory, config.MaxMemory, config.ExtJvm.ToArray(), config.EnablePreLaunchProtection)
    {
    }

    /// <param name="serverId">The server's register ID in LSL.</param>
    /// <param name="serverPath">The server folder's path.</param>
    /// <param name="serverName">The server's name.</param>
    /// <param name="serverType">The type of the server.</param>
    /// <param name="commonInfo">CommonCoreConfig instance recording the jar file name of the common server.</param>
    /// <param name="forgeInfo">ForgeCoreConfig instance recording the library of the forge server.</param>
    /// <param name="usingJava">The executable java file path.</param>
    /// <param name="minMemory">The minimum JVM allocated RAM.</param>
    /// <param name="maxMemory">The maximum JVM allocated RAM.</param>
    /// <param name="extJvm">The extent JVM parameters.</param>
    /// <param name="enablePreLaunchProtection">Disable the operation before the server's complete start. May lock permanently in some types of server such as Mohist.</param>
    public IndexedServerConfig(int serverId,
        string serverPath,
        string serverName,
        ServerCoreType serverType,
        CommonCoreConfigV1? commonInfo,
        ForgeCoreConfigV1? forgeInfo,
        string usingJava,
        uint minMemory,
        uint maxMemory,
        string[] extJvm,
        bool enablePreLaunchProtection) : this(serverId,
        new PathedServerConfig(serverPath, serverName, serverType, commonInfo, forgeInfo, usingJava, minMemory,
            maxMemory, extJvm, enablePreLaunchProtection))
    {
    }

    /// <summary>
    /// Returns a server info which will be recognized as not added.
    /// </summary>
    public static IndexedServerConfig None =>
        new(-1, "", "未添加服务器", ServerCoreType.Unknown, null, null, "", 0, 0, [], false);
}

public static class ServerConfigExtensions
{
    public static Dictionary<int, IndexedServerConfig> CloneToDict(
        this FrozenDictionary<int, IndexedServerConfig> serverConfigs)
    {
        var result = new Dictionary<int, IndexedServerConfig>();
        foreach (var (k, v) in serverConfigs)
        {
            result.TryAdd(k, new IndexedServerConfig(v));
        }
        return result;
    }
}
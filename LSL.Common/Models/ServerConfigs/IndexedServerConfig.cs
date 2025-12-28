using System.Collections.Frozen;

namespace LSL.Common.Models.ServerConfigs;

public class IndexedServerConfig
{
    public PathedServerConfig PathedConfig { get; private set; }
    public int ServerId { get; set; }
    public string ServerPath => PathedConfig.ServerPath;
    public string ServerName => PathedConfig.ServerName;
    public string UsingJava => PathedConfig.UsingJava;
    public string CoreName => PathedConfig.CoreName;
    public uint MinMemory => PathedConfig.MinMemory;
    public uint MaxMemory => PathedConfig.MaxMemory;
    public string ExtJvm => PathedConfig.ExtJvm;

    public IndexedServerConfig(IndexedServerConfig config) // 深拷贝构造函数
        : this(config.ServerId, config.ServerPath, config.ServerName, config.UsingJava, config.CoreName,
            config.MinMemory, config.MaxMemory, config.ExtJvm)
    {
    }

    /// <param name="serverId">The server's register ID in LSL.</param>
    /// <param name="serverPath">The server folder's path.</param>
    /// <param name="serverName">The server's name.</param>
    /// <param name="usingJava">The executable java file path.</param>
    /// <param name="coreName">The core file name of the server.</param>
    /// <param name="minMemory">The minimum JVM allocated RAM.</param>
    /// <param name="maxMemory">The maximum JVM allocated RAM.</param>
    /// <param name="extJvm">The extent JVM parameters.</param>
    public IndexedServerConfig(int serverId,
        string serverPath,
        string serverName,
        string usingJava,
        string coreName,
        uint minMemory,
        uint maxMemory,
        string extJvm) : this(serverId,
        new PathedServerConfig(serverPath, serverName, usingJava, coreName, minMemory, maxMemory, extJvm))
    {
    }

    public IndexedServerConfig(int serverId, PathedServerConfig pathedConfig)
    {
        ServerId = serverId;
        PathedConfig = pathedConfig;
    }

    /// <summary>
    /// Returns a server info which will be recognized as not added.
    /// </summary>
    public static IndexedServerConfig None => new(-1, "", "未添加服务器", "", "", 0, 0, "");
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
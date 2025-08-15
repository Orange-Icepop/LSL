using System.Collections.Frozen;

namespace LSL.Common.Models;

/// <summary>
/// 
/// </summary>
/// <param name="serverId">The server's register ID in LSL.</param>
/// <param name="serverPath">The server folder's path.</param>
/// <param name="name">The server's name.</param>
/// <param name="usingJava">The executable java file path.</param>
/// <param name="coreName">The core file name of the server.</param>
/// <param name="minMemory">The minimum JVM allocated RAM.</param>
/// <param name="maxMemory">The maximum JVM allocated RAM.</param>
/// <param name="extJvm">The extend JVM parameters.</param>
public class ServerConfig(
    int serverId,
    string serverPath,
    string name,
    string usingJava,
    string coreName,
    uint minMemory,
    uint maxMemory,
    string extJvm)
{
    public int server_id { get; set; } = serverId;
    public string server_path { get; set; } = serverPath;
    public string name { get; set; } = name;
    public string using_java { get; set; } = usingJava;
    public string core_name { get; set; } = coreName;
    public uint min_memory { get; set; } = minMemory;
    public uint max_memory { get; set; } = maxMemory;
    public string ext_jvm { get; set; } = extJvm;

    public ServerConfig(ServerConfig config) // 深拷贝构造函数
        : this(config.server_id, config.server_path, config.name, config.using_java, config.core_name, config.min_memory, config.max_memory, config.ext_jvm){}

    /// <summary>
    /// Returns a server info which will be recognized as not added.
    /// </summary>
    public static ServerConfig None => new ServerConfig(-1, "", "未添加服务器", "", "", 0, 0, "");
}

public static class ServerConfigExtensions
{
    public static Dictionary<int, ServerConfig> Clone2Dict(
        this FrozenDictionary<int, ServerConfig> serverConfigs)
    {
        var result = new Dictionary<int, ServerConfig>();
        foreach (var serverConfig in serverConfigs)
        {
            result.TryAdd(serverConfig.Key, new ServerConfig(serverConfig.Value));
        }
        return result;
    }
}

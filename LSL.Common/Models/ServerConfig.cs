using System.Collections.Frozen;
using Newtonsoft.Json;

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
/// <param name="extJvm">The extent JVM parameters.</param>
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
    [JsonProperty("server_id")]
    public int ServerId { get; set; } = serverId;
    [JsonProperty("server_path")]
    public string ServerPath { get; set; } = serverPath;
    [JsonProperty("name")]
    public string Name { get; set; } = name;
    [JsonProperty("using_java")]
    public string UsingJava { get; set; } = usingJava;
    [JsonProperty("core_name")]
    public string CoreName { get; set; } = coreName;
    [JsonProperty("min_memory")]
    public uint MinMemory { get; set; } = minMemory;
    [JsonProperty("max_memory")]
    public uint MaxMemory { get; set; } = maxMemory;
    [JsonProperty("ext_jvm")]
    public string ExtJvm { get; set; } = extJvm;

    public ServerConfig(ServerConfig config) // 深拷贝构造函数
        : this(config.ServerId, config.ServerPath, config.Name, config.UsingJava, config.CoreName, config.MinMemory, config.MaxMemory, config.ExtJvm){}

    /// <summary>
    /// Returns a server info which will be recognized as not added.
    /// </summary>
    [JsonIgnore]
    public static ServerConfig None => new(-1, "", "未添加服务器", "", "", 0, 0, "");
}

public static class ServerConfigExtensions
{
    public static Dictionary<int, ServerConfig> Clone2Dict(
        this FrozenDictionary<int, ServerConfig> serverConfigs)
    {
        var result = new Dictionary<int, ServerConfig>();
        foreach (var (k,v) in serverConfigs)
        {
            result.TryAdd(k, new ServerConfig(v));
        }
        return result;
    }
}

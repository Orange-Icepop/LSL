using System.Collections.Immutable;
using System.ComponentModel;
using Mutty;

namespace LSL.Common.Models.ServerConfig;

[MutableGeneration]
public record IndexedServerConfig
{
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
    /// <param name="statusMonitoring">
    /// Monitor the running/online status.
    /// May work unproperly in some types of server such as Mohist, disable it in these situations.
    /// </param>
    public IndexedServerConfig(int serverId,
        string serverPath,
        string serverName,
        ServerCoreType serverType,
        CommonCoreConfigV1? commonInfo,
        ForgeCoreConfigV1? forgeInfo,
        string usingJava,
        uint minMemory,
        uint maxMemory,
        List<string> extJvm,
        bool statusMonitoring) : this(serverId,
        new LocatedServerConfig(serverPath, serverName, serverType, commonInfo, forgeInfo, usingJava, minMemory,
            maxMemory, extJvm, statusMonitoring))
    {
    }

    public IndexedServerConfig(int serverId, LocatedServerConfig locatedConfig)
    {
        ServerId = serverId;
        LocatedConfig = locatedConfig;
    }

    public LocatedServerConfig LocatedConfig { get; init; }
    public int ServerId { get; init; }
    public string ServerPath => LocatedConfig.ServerPath;
    public string ServerName => LocatedConfig.ServerName;
    public ServerCoreType ServerType => LocatedConfig.ServerType;

    public CommonCoreConfigV1? CommonCoreInfo => LocatedConfig.CommonCoreInfo;
    public ForgeCoreConfigV1? ForgeCoreInfo => LocatedConfig.ForgeCoreInfo;
    public string JavaPath => LocatedConfig.JavaPath;
    public uint MinMemory => LocatedConfig.MinMemory;
    public uint MaxMemory => LocatedConfig.MaxMemory;
    public ImmutableList<string> ExtraJvmArgs => LocatedConfig.ExtraJvmArgs;
    public bool StatusMonitoring => LocatedConfig.StatusMonitoring;

    /// <summary>
    ///     Returns a server info which will be recognized as not added.
    /// </summary>
    public static IndexedServerConfig None =>
        new(-1, "", "未添加服务器", ServerCoreType.Unknown, null, null, "", 0, 0, [], false);
}

public partial class MutableIndexedServerConfig : INotifyPropertyChanged;
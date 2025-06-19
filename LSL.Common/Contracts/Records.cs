namespace LSL.Common.Contracts
{
    // 共享数据存储类
    #region Java信息

    public class JavaInfo
    {
        public string Path { get; init; }
        public string Version { get; init; }
        public string Vendor { get; init; }
        public string Architecture { get; init; }

        public JavaInfo(string path, string version, string vendor, string architecture)
        {
            Path = path;
            Version = version;
            Vendor = vendor;
            Architecture = architecture;
        }
    }

    #endregion

    #region 服务器配置记录

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
            : this(config.server_id, config.server_path, config.name, config.using_java, config.core_name,
                config.min_memory, config.max_memory, config.ext_jvm)
        {
        }

        public static ServerConfig None => new ServerConfig(-1, "", "未添加服务器", "", "", 0, 0, "");
    }

    #endregion

    #region 准服务器配置记录

    public class FormedServerConfig(
        string serverName,
        string corePath,
        string minMem,
        string maxMem,
        string javaPath,
        string extJVM)
    {
        public string ServerName => serverName;
        public string CorePath => corePath;
        public string MinMem => minMem;
        public string MaxMem => maxMem;
        public string JavaPath => javaPath;
        public string ExtJvm => extJVM;
    }

    #endregion
}
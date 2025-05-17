

namespace LSL
{
    public interface IStorageArgs;
    public record ColorOutputArgs(int ServerId, string Output, string ColorHex) : IStorageArgs;// 彩色终端输出事件
    public record ServerStatusArgs(int ServerId, bool IsRunning, bool IsOnline) : IStorageArgs;// 服务器状态更新事件
    public record PlayerUpdateArgs(int ServerId, string UUID, string PlayerName, bool Entering) : IStorageArgs;// 玩家列表更新事件
    public record PlayerMessageArgs(int ServerId, string Message) : IStorageArgs;// 服务器消息事件
    
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
    public class ServerConfig
    {
        public int server_id;
        public string server_path;
        public string name;
        public string using_java;
        public string core_name;
        public uint min_memory;
        public uint max_memory;
        public string ext_jvm;
        public ServerConfig(int ServerId, string ServerPath, string Name, string UsingJava, string CoreName, uint MinMemory, uint MaxMemory, string ExtJVM)
        {
            this.server_id = ServerId;
            this.server_path = ServerPath;
            this.name = Name;
            this.using_java = UsingJava;
            this.core_name = CoreName;
            this.min_memory = MinMemory;
            this.max_memory = MaxMemory;
            this.ext_jvm = ExtJVM;
        }

        public ServerConfig(ServerConfig config)// 深拷贝构造函数
        {
            this.server_id = config.server_id;
            this.server_path = config.server_path;
            this.name = config.name;
            this.using_java = config.using_java;
            this.core_name = config.core_name;
            this.min_memory = config.min_memory;
            this.max_memory = config.max_memory;
            this.ext_jvm = config.ext_jvm;
        }
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

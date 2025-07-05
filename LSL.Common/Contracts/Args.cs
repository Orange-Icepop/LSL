

namespace LSL.Common.Contracts
{
    /// <summary>Contracts for methods handling server outputs in LSL.</summary>
    public interface IStorageArgs;
    /// <summary>Contracts for methods handling server metrics information in LSL.</summary>
    public interface IMetricsArgs;
    /// <summary>Server output line with color properties. Sent by ServerOutputHandler class.</summary>
    /// <param name="ServerId">The registered ID of the server process from which the output line comes.</param>
    /// <param name="Output">The output content.</param>
    /// <param name="ColorHex">The line's color property.</param>
    public record ColorOutputArgs(int ServerId, string Output, string ColorHex) : IStorageArgs;
    
    /// <summary>An argument being sent when a ServerProcess' status changed.</summary>
    /// <param name="ServerId">The registered ID of the status changed server process.</param>
    /// <param name="IsRunning">Whether the server is running now.</param>
    /// <param name="IsOnline">Whether the server has been initialized and enables players login now.</param>
    public record ServerStatusArgs(int ServerId, bool IsRunning, bool IsOnline) : IStorageArgs;// 服务器状态更新事件
    /// <summary>An argument being sent when someone enters or leaves a server.</summary>
    /// <param name="ServerId">The registered ID of the server process which the player logs in/out.</param>
    /// <param name="UUID">The UUID of the login/logout player.</param>
    /// <param name="PlayerName">The login/logout player's name.</param>
    /// <param name="Entering">Whether the player is logging in or out.</param>
    public record PlayerUpdateArgs(int ServerId, string UUID, string PlayerName, bool Entering) : IStorageArgs;// 玩家列表更新事件
    /// <summary>An argument being sent when a player sends a message in the server.</summary>
    /// <param name="ServerId">The registered ID of the server process which the player sends the message.</param>
    /// <param name="Message">The message content.</param>
    public record PlayerMessageArgs(int ServerId, string Message) : IStorageArgs;// 服务器消息事件 TODO:增加用户名参数

    /// <summary>The report of metrics of a single server which will be sent once a second. Will be wrapped in an IEnumerable.</summary>
    /// <param name="ServerId">The registered ID of the monitored server process.</param>
    /// <param name="CpuUsage">The CPU multicore usage percent that has been kept integer.</param>
    /// <param name="MemUsage">The RAM usage percent that has been kept integer.</param>
    /// <param name="MemBytes">How many bytes of RAM has been used by this server.</param>
    public record MetricsReport(int ServerId, int CpuUsage, long MemBytes, int MemUsage);
    /// <summary>Message that contains the current second's metrics of all servers that are running now.</summary>
    /// <param name="Metrics">An IEnumerable of MetricsReport instances.</param>
    public record MetricsUpdateArgs(IEnumerable<MetricsReport> Metrics) : IMetricsArgs;
    /// <summary>Minutely metrics report of recent 30 mins.</summary>
    /// <param name="CpuHistory"></param>
    /// <param name="RamHistory"></param>
    public record GeneralMetricsArgs(IEnumerable<uint> CpuHistory, IEnumerable<uint> RamHistory) : IMetricsArgs;
}



namespace LSL.IPC
{
    public interface IStorageArgs;
    public record ColorOutputArgs(int ServerId, string Output, string ColorHex) : IStorageArgs;// 彩色终端输出事件
    public record ServerStatusArgs(int ServerId, bool IsRunning, bool IsOnline) : IStorageArgs;// 服务器状态更新事件
    public record PlayerUpdateArgs(int ServerId, string UUID, string PlayerName, bool Entering) : IStorageArgs;// 玩家列表更新事件
    public record PlayerMessageArgs(int ServerId, string Message) : IStorageArgs;// 服务器消息事件
}

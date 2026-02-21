using System.Threading.Tasks;
using FluentResults;

namespace LSL.Services.ServerServices;

public interface IServerHost
{
    Task<Result> RunServer(int serverId);
    void StopServer(int serverId);
    bool SendCommand(int serverId, string command);
    Task EndServer(int serverId);
    Task EndAllServers();
}

public enum OutputChannelType
{
    StdOut,
    StdErr,
    LSLInfo,
    LSLError
}

public record TerminalOutputArgs(int ServerId, string Output, OutputChannelType ChannelType); // 终端输出事件

public record ColorOutputLine(string Line, string ColorHex); // 着色输出行
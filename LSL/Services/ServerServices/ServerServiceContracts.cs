namespace LSL.Services.ServerServices;

public interface IServerHost
{
    bool RunServer(int serverId);
    void StopServer(int serverId);
    bool SendCommand(int serverId, string command);
    void EndServer(int serverId);
    void EndAllServers();
}

public record TerminalOutputArgs(int ServerId, string Output);// 终端输出事件
public record ColorOutputLine(string Line, string ColorHex);// 着色输出行
    

using ReactiveUI;
using ReactiveUI.SourceGenerators;


namespace LSL.Models;

public partial class ServerStatus : ReactiveObject
{
    public ServerStatus()
    {
        IsRunning = false;
        IsOnline = false;
    }

    public ServerStatus((bool, bool) param)
    {
        IsRunning = param.Item1;
        IsOnline = param.Item2;
    }

    public ServerStatus(bool isRunning, bool isOnline)
    {
        IsRunning = isRunning;
        IsOnline = isOnline;
    }

    [Reactive]
    public partial bool IsRunning { get; private set; }
    [Reactive]
    public partial bool IsOnline { get; private set; }

    public ServerStatus Update(bool isRunning, bool isOnline)
    {
        IsRunning = isRunning;
        IsOnline = isOnline;
        return this;
    }

    public ServerStatus Update((bool, bool) param)
    {
        IsRunning = param.Item1;
        IsOnline = param.Item2;
        return this;
    }
}
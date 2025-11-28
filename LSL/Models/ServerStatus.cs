using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.Models;

public class ServerStatus : ReactiveObject
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

    [Reactive] public bool IsRunning { get; private set; }
    [Reactive] public bool IsOnline { get; private set; }
}
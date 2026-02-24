using System.ComponentModel;

namespace LSL.Common.Models.Minecraft;

public partial class ServerStatus : INotifyPropertyChanged
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
    
    public bool IsRunning { get; private set; }
    public bool IsOnline { get; private set; }

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
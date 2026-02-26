using System.ComponentModel;
using LSL.Common.DTOs;

namespace LSL.Common.Models.Minecraft;

public record ServerStatusInfo : IServerMessage
{
    public ServerStatusInfo(int id, bool isRunning, bool isOnline)
    {
        Id = id;
        Info = new ServerStatus(isRunning, isOnline);
    }
    public int Id { get; init; }
    public ServerStatus Info { get; init; }
}

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
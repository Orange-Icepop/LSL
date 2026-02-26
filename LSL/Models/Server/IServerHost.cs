using System;
using System.Threading.Tasks;
using FluentResults;
using LSL.Common.DTOs;

namespace LSL.Models.Server;

public interface IServerHost
{
    Task<Result> RunServer(int serverId);
    void StopServer(int serverId);
    bool SendCommand(int serverId, string command);
    Task EndServer(int serverId);
    Task EndAllServers();
    IObservable<IServerMessage> ServerMessages { get; }
}
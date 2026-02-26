using System;
using LSL.Common.DTOs;
using LSL.Models.Server;
using Microsoft.Extensions.Logging;

namespace LSL.Services;

public class ClientConnector
{
    public ClientConnector(ILogger<ClientConnector> logger, IServerHost serverHost)
    {
        _logger = logger;
        _serverHost = serverHost;
        //_serverHost.ServerMessages.Subscribe(m => EventBus.Instance.Fire(m));
    }
    
    private readonly ILogger<ClientConnector> _logger;
    private readonly IServerHost _serverHost;
    
    public IObservable<IServerMessage> ServerMessages => _serverHost.ServerMessages;
}
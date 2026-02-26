using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentResults;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Models.Server;
using LSL.Services.ConfigServices;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ServerServices;

/// <summary>
///     The main daemon class for managing Minecraft server processes.
/// </summary>
public class ServerHost : IServerHost, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ServerHost> _logger;
    
    // 注意：接受ServerId作为参数的方法采用的都是注册服务器的顺序，必须先在ViewModel中将列表项解析为ServerId

    private readonly ConcurrentDictionary<int, ServerInstance> _runningServers = []; // 存储正在运行的服务器实例
    private readonly ServerConfigManager _serverConfigManager;

    public ServerHost(ServerConfigManager scm, ILogger<ServerHost> logger, ILoggerFactory loggerFactory)
    {
        _serverConfigManager = scm;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _logger.LogInformation("ServerHost Launched");
    }

    // 释放资源
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var server in _runningServers.Values)
        {
            Task.Run((() => server.Dispose()));
        }
    }

    #region 启动服务器RunServer(int serverId)

    public async Task<Result> RunServer(int serverId)
    {
        _logger.LogInformation("Starting server with id {id}...", serverId);
        if (GetServer(serverId) is not null)
        {
            _logger.LogError("Server with id {id} is already running. Not running another instance.", serverId);
            return Result.Ok().WithReason(new WarningReason($"Server with id {serverId} is already running."));
        }

        if (!_serverConfigManager.TryGetServerConfig(serverId, out var config))
        {
            _logger.LogError(
                "Server with id {id} not found in configuration. That's weird! It should have been checked!", serverId);
            return Result.Fail($"Server with id {serverId} not found in configuration.");
        }

        var processResult = await ServerInstance.Create(config, _loggerFactory.CreateLogger<ServerInstance>());
        if (processResult.IsFailed)
        {
            var messages = processResult.GetErrors().GetMessages();
            _logger.LogError("Server with id {id} failed to run: {error}.", serverId, messages);
            return processResult.Bind(_ => Result.Ok());
        }

        var process = processResult.Value;
        process.AllEvents.Subscribe(args => EventBus.Instance.Fire(args));

        LoadServer(serverId, process);
        _logger.LogInformation("Server with id {id} is mounted.", serverId);
        process.Status.Where(info => !info.IsRunning)
            .Subscribe(_ =>
            {
                // 移除进程的实例
                UnloadServer(serverId);
                process.Dispose();
            });
        _logger.LogInformation("Server with id {id} is started.", serverId);
        return Result.Ok();
    }

    #endregion

    #region 关闭服务器StopServer(int serverId)

    public void StopServer(int serverId)
    {
        var server = GetServer(serverId);
        if (server is null) return;
        _logger.LogInformation("Stopping server with id {id}...", serverId);
        server.Stop();
    }

    #endregion

    #region 发送命令SendCommand(int serverId, string command)

    public bool SendCommand(int serverId, string command)
    {
        var server = GetServer(serverId);
        if (server is null) return false;
        server.SendCommand(command);
        return true;
    }

    #endregion

    #region 强制结束服务器进程EndServer(int serverId)

    public Task EndServer(int serverId)
    {
        var server = GetServer(serverId);
        return Task.Run(() => server?.Dispose());
    }

    #endregion

    #region 终止所有服务器进程EndAllServers()

    public async Task EndAllServers()
    {
        await Parallel.ForEachAsync(_runningServers.Values, (i, t) =>
        {
            Task.Run(i.Dispose, t);
            return ValueTask.CompletedTask;
        });
        _runningServers.Clear();
        _logger.LogInformation("Ended all servers.");
    }

    #endregion

    #region 存储服务器进程实例

    private void LoadServer(int serverId, ServerInstance process)
    {
        _runningServers.AddOrUpdate(serverId, process, (_, _) => process);
    }

    #endregion

    #region 移除服务器进程实例UnloadServer(int serverId)

    private void UnloadServer(int serverId)
    {
        if (_runningServers.TryRemove(serverId, out _))
            _logger.LogInformation("Server with id {id} unloaded successfully.", serverId);
        else
            _logger.LogError("Server with id {id} not found.", serverId);
    }

    #endregion

    #region 获取服务器进程实例GetServer(int serverId)

    private ServerInstance? GetServer(int serverId)
    {
        return _runningServers.GetValueOrDefault(serverId);
    }

    #endregion

    #region 确保进程退出命令EnsureExited(int serverId)

    private void EnsureExited(int serverId)
    {
        var server = GetServer(serverId);
        server?.Dispose();
        UnloadServer(serverId);
    }

    #endregion
}
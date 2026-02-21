using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using LSL.Common.DTOs;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Services.ConfigServices;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ServerServices;

/// <summary>
///     The main daemon class for managing Minecraft server processes.
/// </summary>
public class ServerHost : IServerHost, IDisposable
{
    private readonly ILogger<ServerHost> _logger;

    private readonly ServerMetricsBuffer _metricsHandler;

    // 启动输出处理器
    private readonly ServerOutputHandler _outputHandler;

    private readonly ServerOutputStorage _outputStorage;
    // 注意：接受ServerId作为参数的方法采用的都是注册服务器的顺序，必须先在ViewModel中将列表项解析为ServerId

    private readonly ConcurrentDictionary<int, ServerProcess> _runningServers = []; // 存储正在运行的服务器实例
    private readonly ServerConfigManager _serverConfigManager;

    public ServerHost(ServerOutputHandler outputHandler, ServerOutputStorage outputStorage, ServerConfigManager scm,
        ServerMetricsBuffer smt, ILogger<ServerHost> logger)
    {
        _outputStorage = outputStorage;
        _outputHandler = outputHandler;
        _serverConfigManager = scm;
        _metricsHandler = smt;
        _logger = logger;
        _logger.LogInformation("ServerHost Launched");
    }

    // 释放资源
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region 启动服务器RunServer(int serverId)

    public async Task<Result> RunServer(int serverId)
    {
        _logger.LogInformation("Starting server with id {id}...", serverId);
        if (GetServer(serverId) is not null)
        {
            _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 警告]: 服务器已经在运行中。",
                OutputChannelType.LSLError));
            _logger.LogError("Server with id {id} is already running. Not running another instance.", serverId);
            return Result.Ok().WithReason(new WarningReason($"Server with id {serverId} is already running."));
        }

        if (!_serverConfigManager.TryGetServerConfig(serverId, out var config))
        {
            _logger.LogError(
                "Server with id {id} not found in configuration. That's weird! It should have been checked!", serverId);
            return Result.Fail($"Server with id {serverId} not found in configuration.");
        }

        var processResult = await ServerProcess.Create(config);
        if (processResult.IsFailed)
        {
            var messages = processResult.GetErrors().GetMessages();
            _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, $"[LSL 错误]: {messages}",
                OutputChannelType.LSLError));
            _logger.LogError("Server with id {id} failed to run: {error}.", serverId, messages);
            return processResult.Bind(_ => Result.Ok());
        }

        var process = processResult.Value;
        process.StatusEventHandler += (_, args) =>
            EventBus.Instance.Fire<IStorageArgs>(new ServerStatusArgs(serverId, args.IsRunning, args.IsOnline));
        // 启动服务器
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            process.Dispose();
            _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 错误]: 服务器启动失败，请检查配置文件。",
                OutputChannelType.LSLError));
            _logger.LogError("Server with id {id} failed to run.\n{ex}", serverId, ex);
            return Result.Fail(new ExceptionalError(ex));
        }

        LoadServer(serverId, process);
        _logger.LogInformation("Server with id {id} is mounted.", serverId);
        _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 服务器正在启动，请稍后......",
            OutputChannelType.LSLInfo));
        process.OutputReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, e.Data, OutputChannelType.StdOut));
        };

        process.ErrorReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, e.Data, OutputChannelType.StdErr));
        };
        process.BeginRead();
        process.Exited += (_, _) =>
        {
            // 移除进程的实例
            UnloadServer(serverId);
            var exitCode = "Unknown，因为服务端进程以异常的方式结束了";
            try
            {
                var processExitCode = process.ExitCode ?? -1;
                exitCode = processExitCode == -1 ? "Unknown，因为服务端进程以异常的方式结束了" : processExitCode.ToString();
            }
            finally
            {
                var status = $"已关闭，进程退出码为{exitCode}";
                _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 当前服务器" + status,
                    OutputChannelType.StdOut));
                _logger.LogInformation("Server with id {id} is stopped.", serverId);
                process.Dispose();
            }
        };
        process.StatusEventHandler += (_, e) =>
        {
            if (e.IsOnline)
            {
                _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 服务器启动成功!",
                    OutputChannelType.LSLInfo));
                _logger.LogInformation("Server with id {id} is online now.", serverId);
            }
        };
        process.MetricsReceived += (_, e) => { _metricsHandler.TryWrite(e); };
        _logger.LogInformation("Server with id {id} is started.", serverId);
        return Result.Ok();
    }

    #endregion

    #region 关闭服务器StopServer(int serverId)

    public void StopServer(int serverId)
    {
        var server = GetServer(serverId);
        if (server is not null && server.IsRunning)
        {
            _logger.LogInformation("Stopping server with id {id}...", serverId);
            server.Stop();
            _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 关闭服务器命令已发出，请等待......",
                OutputChannelType.LSLInfo));
        }
        else
        {
            _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 错误]: 服务器未启动，消息无法发送",
                OutputChannelType.LSLError));
        }
    }

    #endregion

    #region 发送命令SendCommand(int serverId, string command)

    public bool SendCommand(int serverId, string command)
    {
        var server = GetServer(serverId);
        if (server is not null && server.IsRunning)
        {
            if (command == "stop")
                _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 关闭服务器命令已发出，请等待......",
                    OutputChannelType.LSLInfo));
            server.SendCommand(command);
            return true;
        }

        _outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 错误]: 服务器未启动，消息无法发送",
            OutputChannelType.LSLError));
        return false;
    }

    #endregion

    #region 强制结束服务器进程EndServer(int serverId)

    public async Task EndServer(int serverId)
    {
        var server = GetServer(serverId);
        var t = server?.Kill();
        if (t is not null) await t;
    }

    #endregion

    #region 终止所有服务器进程EndAllServers()

    public async Task EndAllServers()
    {
        await Parallel.ForEachAsync(_runningServers.Values, (p, _) => new ValueTask(p.Kill()));
        _runningServers.Clear();
        _logger.LogInformation("Ended all servers.");
    }

    #endregion

    #region 存储服务器进程实例LoadServer(int serverId, Process process)

    private void LoadServer(int serverId, ServerProcess process)
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

    private ServerProcess? GetServer(int serverId)
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
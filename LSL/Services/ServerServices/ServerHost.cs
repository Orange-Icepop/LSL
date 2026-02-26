using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentResults;
using LSL.Common.Collections;
using LSL.Common.DTOs;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Common.Utilities;
using LSL.Models.Server;
using LSL.Services.ConfigServices;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ServerServices;

/// <summary>
///     The main daemon class for managing Minecraft server processes.
/// </summary>
public class ServerHost : IServerHost, IDisposable
{
    #region 依赖

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ServerHost> _logger;
    private readonly ServerConfigManager _serverConfigManager;

    #endregion

    // 注意：接受ServerId作为参数的方法采用的都是注册服务器的顺序，必须先在ViewModel中将列表项解析为ServerId

    private readonly ConcurrentDictionary<int, ServerInstance> _runningServers = []; // 存储正在运行的服务器实例
    private readonly ConcurrentDictionary<int, IDisposable> _serverSubscriptions = new();

    #region 全局统计相关

    private static readonly long s_systemMemory = MemoryInfo.CurrentSystemMemory; // 系统总内存（字节）
    private readonly ConcurrentDictionary<int, SecondlyMetricsReport> _latestSecondlyMetrics = new(); // 各服务器最新的秒级指标
    private readonly Subject<GlobalSecondlyMetricsReport> _globalSecondlySubject = new(); // 每秒发布的全局指标

    private readonly Subject<GlobalMinutelyMetricsReport> _globalMinutelySubject = new(); // 每分钟发布的聚合指标

    // 公开的全局流
    public IObservable<GlobalSecondlyMetricsReport> GlobalSecondly => _globalSecondlySubject.AsObservable();
    public IObservable<GlobalMinutelyMetricsReport> GlobalMinutely => _globalMinutelySubject.AsObservable();

    // 用于分钟级聚合的历史采样（最近一分钟的秒级报告）
    private readonly RangedObservableLinkedList<GlobalSecondlyMetricsReport> _lastMinuteSamples = new(60);

    // 定时器
    private readonly IDisposable _secondlyTimer;
    private readonly IDisposable _minutelyTimer;

    #endregion

    public ServerHost(ServerConfigManager scm, ILogger<ServerHost> logger, ILoggerFactory loggerFactory)
    {
        _serverConfigManager = scm;
        _logger = logger;
        _loggerFactory = loggerFactory;
        // 每秒触发一次全局统计
        _secondlyTimer = Observable.Interval(TimeSpan.FromSeconds(1))
            .Subscribe(_ => OnSecondlyTick());

        // 每分钟触发一次分钟级聚合
        _minutelyTimer = Observable.Interval(TimeSpan.FromMinutes(1))
            .Subscribe(_ => OnMinutelyTick());
        _logger.LogInformation("ServerHost initialized");
    }

    // 释放资源
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _secondlyTimer.Dispose();
        _minutelyTimer.Dispose();
        EndAllServers().Wait(TimeSpan.FromSeconds(5));
        _globalSecondlySubject.Dispose();
        _globalMinutelySubject.Dispose();
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

        var instanceResult = await ServerInstance.Create(config, _loggerFactory.CreateLogger<ServerInstance>());
        if (instanceResult.IsFailed)
        {
            var messages = instanceResult.GetErrors().GetMessages();
            _logger.LogError("Server with id {id} failed to run: {error}.", serverId, messages);
            return instanceResult.Bind(_ => Result.Ok());
        }

        var instance = instanceResult.Value;
        // 订阅秒级指标，更新最新数据
        var secondlySub = instance.SecondlyMetrics.Subscribe(metrics =>
        {
            _latestSecondlyMetrics[serverId] = metrics;
        });

        // 订阅状态流，监测进程退出以清理资源
        var statusSub = instance.Status
            .Where(status => !status.IsRunning)
            .Subscribe(_ => UnloadServer(serverId));

        // 保存订阅以便卸载时取消
        _serverSubscriptions[serverId] = new CompositeDisposable(secondlySub, statusSub);

        instance.AllEvents.Subscribe(args => EventBus.Instance.Fire(args));

        LoadServer(serverId, instance);
        _logger.LogInformation("Server with id {id} is mounted.", serverId);
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
        await Parallel.ForEachAsync(_runningServers.Values, (i, _) =>
        {
            i.Dispose();
            return ValueTask.CompletedTask;
        });
        await Parallel.ForEachAsync(_serverSubscriptions.Values, (i, _) =>
        {
            i.Dispose();
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
        if (_runningServers.TryRemove(serverId, out var process))
        {
            process.Dispose();
            if (_serverSubscriptions.TryRemove(serverId, out var subscription))
            {
                subscription.Dispose();
            }

            _logger.LogInformation("Server with id {id} unloaded successfully.", serverId);
        }
        else _logger.LogError("Server with id {id} not found.", serverId);
        _latestSecondlyMetrics.TryRemove(serverId, out _);
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

    #region 性能记录

    private void OnSecondlyTick()
    {
        // 获取所有服务器的最新秒级指标快照
        var snapshots = _latestSecondlyMetrics.Values.ToArray();
        if (snapshots.Length == 0) return;

        double totalCpu = 0;
        long totalMem = 0;
        foreach (var s in snapshots)
        {
            totalCpu += s.CpuUsage;
            totalMem += s.MemBytes;
        }

        double totalMemPercent = (double)totalMem / s_systemMemory * 100;

        var report = new GlobalSecondlyMetricsReport(
            DateTime.UtcNow,
            totalCpu,
            totalMem,
            totalMemPercent
        );

        _globalSecondlySubject.OnNext(report);

        // 添加到分钟采样列表
        _lastMinuteSamples.Add(report);
    }

    private void OnMinutelyTick()
    {
        if (_latestSecondlyMetrics.IsEmpty) return;
        var list = _lastMinuteSamples.ToArray();
        if (list.Length == 0) return;

        // 计算平均值和峰值
        var avgCpu = list.Average(r => r.CpuUsage);
        var avgMem = (long)list.Average(r => r.MemBytes);
        var avgMemPercent = list.Average(r => r.MemUsage);

        var minutelyReport = new GlobalMinutelyMetricsReport(
            DateTime.UtcNow,
            avgCpu,
            avgMem,
            avgMemPercent
        );

        _globalMinutelySubject.OnNext(minutelyReport);
    }

    #endregion
}
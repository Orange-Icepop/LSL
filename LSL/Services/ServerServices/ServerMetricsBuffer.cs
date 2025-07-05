using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LSL.Common.Contracts;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ServerServices;

/// <summary>
/// A class used to transfer ProcessMetricsEventArgs into continuous & low-cost args.
/// Also caches metrics information.
/// </summary>
public class ServerMetricsBuffer : IDisposable
{
    private readonly CancellationTokenSource CTS;
    private Task _processTask;
    private Task _reportTask;
    private readonly Channel<ProcessMetricsEventArgs> metricsChannel;
    private readonly RangedObservableLinkedList<uint> generalCpuHistory = new(30, 0, false);
    private readonly RangedObservableLinkedList<uint> generalRamHistory = new(30, 0, false);
    private readonly List<double> thisMinuteCpuUsage = [];
    private readonly List<double> thisMinuteRamUsage = [];
    private DateTime lastMinute;
        
    private ILogger<ServerMetricsBuffer> _logger { get; }

    public ServerMetricsBuffer(ILogger<ServerMetricsBuffer> logger)
    {
        _logger = logger;
        lastMinute = DateTime.Now;
        CTS = new CancellationTokenSource();
        metricsChannel = Channel.CreateUnbounded<ProcessMetricsEventArgs>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
        _processTask = Task.Run(() => ProcessMetrics(CTS.Token));
        _reportTask = Task.Run(() => ReportMetrics(CTS.Token));
        _logger.LogInformation("ServerMetricsBuffer Launched");
    }
    
    #region 处理业务逻辑
    public bool TryWrite(ProcessMetricsEventArgs args) => metricsChannel.Writer.TryWrite(args);
    private readonly ConcurrentDictionary<int,(double CpuUsage, long MemBytes, double MemUsage)> currentMetrics = new();
    private async Task ProcessMetrics(CancellationToken ct)
    {
        try
        {
            await foreach (var args in metricsChannel.Reader.ReadAllAsync(ct))
            {
                currentMetrics.AddOrUpdate(args.ServerId,
                    (args.CpuUsagePercent, args.MemoryUsageBytes, args.MemoryUsagePercent),
                    (id, value) => (args.CpuUsagePercent, args.MemoryUsageBytes, args.MemoryUsagePercent));
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Metrics processing is canceled.");
        }
    }

    private async Task ReportMetrics(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var met = currentMetrics
                .Select(kvp => new MetricsReport(kvp.Key, CastAndRecCpu(kvp.Value.CpuUsage), kvp.Value.MemBytes,
                    CastAndRecMem(kvp.Value.MemUsage)))
                .ToList();
            EventBus.Instance.PublishAsync(new MetricsUpdateArgs(met));
            // calc minutely metrics sum-up
            var now = DateTime.Now;
            if (now - lastMinute > TimeSpan.FromMinutes(1))
            {
                lastMinute = now;
                generalCpuHistory.Add((uint)thisMinuteCpuUsage.Average());
                generalRamHistory.Add((uint)thisMinuteRamUsage.Average());
                thisMinuteCpuUsage.Clear();
                thisMinuteRamUsage.Clear();
                EventBus.Instance.PublishAsync(new GeneralMetricsArgs(generalCpuHistory, generalRamHistory));
            }
            // wait
            await Task.Delay(1000, ct);
        }
        _logger.LogInformation("Metrics reporting stopped.");
    }
    #endregion

    #region 辅助方法
    private static int CastToInt(double val)
    {
        if (double.IsInfinity(val)) return 100;
        if (double.IsNaN(val)) return 0;
        return (int)Math.Round(val);
    }

    private int CastAndRecCpu(double val)
    {
        thisMinuteCpuUsage.Add(val);
        return CastToInt(val);
    }
    private int CastAndRecMem(double val)
    {
        thisMinuteRamUsage.Add(val);
        return CastToInt(val);
    }
    #endregion
    
    public void Dispose()
    {
        CTS.Cancel();
        metricsChannel.Writer.TryComplete();
        Task.WaitAll([_processTask, _reportTask], TimeSpan.FromSeconds(3));
        CTS.Dispose();
        GC.SuppressFinalize(this);
    }
}
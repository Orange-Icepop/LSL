using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LSL.Common.Collections;
using LSL.Common.DTOs;
using LSL.Common.Utilities;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ServerServices;

/// <summary>
///     A class used to transfer ProcessMetricsEventArgs into continuous & low-cost args.
///     Also caches metrics information.
/// </summary>
public class ServerMetricsBuffer : IDisposable
{
    private const int HistoryMinutes = 30;
    private const int ReportIntervalMs = 1000;
    private const int MinuteIntervalMs = 60000;
    private static readonly long s_systemMem = MemoryInfo.CurrentSystemMemory;

    // 历史记录队列
    private readonly RangedObservableLinkedList<double> _cpuHistory = new(HistoryMinutes, 0, false);

    private readonly CancellationTokenSource _cts;

    // 当前秒数据
    private readonly ConcurrentDictionary<int, (double Cpu, long RamBytes, double RamPercent)> _currentMetrics = new();
    private readonly ILogger<ServerMetricsBuffer> _logger;
    private readonly Channel<ProcessMetricsEventArgs> _metricsChannel;

    // 当前分钟数据
    private readonly List<double> _minuteCpuSamples = [];
    private readonly List<long> _minuteRamBytesSamples = [];

    private readonly Task _processingTask;
    private readonly RangedObservableLinkedList<long> _ramBytesAvgHistory = new(HistoryMinutes, 0, false);
    private readonly RangedObservableLinkedList<long> _ramBytesPeakHistory = new(HistoryMinutes, 0, false);
    private readonly RangedObservableLinkedList<double> _ramPercentHistory = new(HistoryMinutes, 0, false);
    private readonly Task _reportingTask;
    private DateTime _lastMinuteReportTime = DateTime.Now;

    public ServerMetricsBuffer(ILogger<ServerMetricsBuffer> logger)
    {
        _logger = logger;
        _cts = new CancellationTokenSource();
        _metricsChannel = Channel.CreateUnbounded<ProcessMetricsEventArgs>(
            new UnboundedChannelOptions { SingleReader = true });

        _processingTask = Task.Run(() => ProcessMetricsAsync(_cts.Token));
        _reportingTask = Task.Run(() => ReportMetricsAsync(_cts.Token));

        _logger.LogInformation("ServerMetricsBuffer initialized");
    }

    public void Dispose()
    {
        _cts.Cancel();
        _metricsChannel.Writer.Complete();

        Task.WaitAll([_processingTask, _reportingTask], TimeSpan.FromSeconds(3));
        _cts.Dispose();

        GC.SuppressFinalize(this);
        _logger.LogInformation("ServerMetricsBuffer disposed");
    }

    public bool TryWrite(ProcessMetricsEventArgs args)
    {
        return _metricsChannel.Writer.TryWrite(args);
    }

    private async Task ProcessMetricsAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var args in _metricsChannel.Reader.ReadAllAsync(ct))
            {
                var metrics = GetValidatedMetrics(args);
                _currentMetrics.AddOrUpdate(args.ServerId, _ => metrics, (_, _) => metrics);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Metrics processing stopped");
        }
    }

    private async Task ReportMetricsAsync(CancellationToken ct)
    {
        var secondTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(ReportIntervalMs));
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 每秒报告
                await secondTimer.WaitForNextTickAsync(ct);
                ReportPerSecondMetrics();

                // 每分钟报告
                if (DateTime.Now.Subtract(_lastMinuteReportTime).TotalMilliseconds > MinuteIntervalMs)
                {
                    ReportPerMinuteMetrics();
                    _lastMinuteReportTime = DateTime.Now;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Metrics reporting stopped");
        }
    }

    private void ReportPerSecondMetrics()
    {
        var reports = _currentMetrics.Select(kvp =>
                new MetricsReport(
                    kvp.Key,
                    SanitizeValue(kvp.Value.Cpu),
                    kvp.Value.RamBytes,
                    SanitizeValue(kvp.Value.RamPercent)))
            .ToList();

        EventBus.Instance.Fire<IMetricsArgs>(new MetricsUpdateArgs(reports));
    }

    private void ReportPerMinuteMetrics()
    {
        // 计算分钟级指标
        var cpuAvg = _minuteCpuSamples.Count > 0
            ? _minuteCpuSamples.Average()
            : 0;

        var ramBytesAvg = _minuteRamBytesSamples.Count > 0
            ? (long)_minuteRamBytesSamples.Average()
            : 0;

        var ramPercentAvg = (double)ramBytesAvg / s_systemMem * 100;

        var ramBytesPeak = _minuteRamBytesSamples.Count > 0
            ? _minuteRamBytesSamples.Max()
            : 0;

        // 更新历史记录
        _cpuHistory.Add(cpuAvg);
        _ramPercentHistory.Add(ramPercentAvg);
        _ramBytesAvgHistory.Add(ramBytesAvg);
        _ramBytesPeakHistory.Add(ramBytesPeak);

        // 发布历史报告
        EventBus.Instance.Fire<IMetricsArgs>(new GeneralMetricsArgs(
            _cpuHistory,
            _ramPercentHistory,
            _ramBytesAvgHistory,
            _ramBytesPeakHistory));

        // 重置分钟数据
        _minuteCpuSamples.Clear();
        _minuteRamBytesSamples.Clear();

        _lastMinuteReportTime = DateTime.Now;
    }

    private (double Cpu, long RamBytes, double RamPercent) GetValidatedMetrics(ProcessMetricsEventArgs args)
    {
        if (args.IsProcessExited || args.Error != null)
            return (0, 0, 0);

        // 收集分钟级样本
        _minuteCpuSamples.Add(args.CpuUsagePercent);
        _minuteRamBytesSamples.Add(args.MemoryUsageBytes);

        return (args.CpuUsagePercent, args.MemoryUsageBytes, args.MemoryUsagePercent);
    }

    private static double SanitizeValue(double value)
    {
        if (double.IsNaN(value)) return 0;
        if (double.IsInfinity(value)) return 100;
        return Math.Clamp(value, 0, 100);
    }
}
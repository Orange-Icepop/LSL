using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LSL.Common.Collections;
using LSL.Common.Contracts;
using LSL.Common.Utilities;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ServerServices;

/// <summary>
/// A class used to transfer ProcessMetricsEventArgs into continuous & low-cost args.
/// Also caches metrics information.
/// </summary>
public class ServerMetricsBuffer : IDisposable
{
    private const int HISTORY_MINUTES = 30;
    private const int REPORT_INTERVAL_MS = 1000;
    private const int MINUTE_INTERVAL_MS = 60000;
    private static readonly long _systemMem = MemoryInfo.GetTotalSystemMemory();
    
    private readonly CancellationTokenSource _cts;
    private readonly ILogger<ServerMetricsBuffer> _logger;
    private readonly Channel<ProcessMetricsEventArgs> _metricsChannel;
    
    // 历史记录队列
    private readonly RangedObservableLinkedList<double> _cpuHistory = new(HISTORY_MINUTES, 0, false);
    private readonly RangedObservableLinkedList<double> _ramPercentHistory = new(HISTORY_MINUTES, 0, false);
    private readonly RangedObservableLinkedList<long> _ramBytesAvgHistory = new(HISTORY_MINUTES, 0, false);
    private readonly RangedObservableLinkedList<long> _ramBytesPeakHistory = new(HISTORY_MINUTES, 0, false);
    
    // 当前分钟数据
    private readonly List<double> _minuteCpuSamples = [];
    private readonly List<long> _minuteRamBytesSamples = [];
    
    // 当前秒数据
    private readonly ConcurrentDictionary<int, (double Cpu, long RamBytes, double RamPercent)> _currentMetrics = new();
    private DateTime _lastMinuteReportTime = DateTime.Now;
    
    private Task _processingTask;
    private Task _reportingTask;

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

    public bool TryWrite(ProcessMetricsEventArgs args) => 
        _metricsChannel.Writer.TryWrite(args);

    private async Task ProcessMetricsAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var args in _metricsChannel.Reader.ReadAllAsync(ct))
            {
                var metrics = GetValidatedMetrics(args);
                _currentMetrics.AddOrUpdate(args.ServerId, k => metrics, (k, v) => metrics);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Metrics processing stopped");
        }
    }

    private async Task ReportMetricsAsync(CancellationToken ct)
    {
        var secondTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(REPORT_INTERVAL_MS));
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 每秒报告
                await secondTimer.WaitForNextTickAsync(ct);
                ReportPerSecondMetrics();
                
                // 每分钟报告
                if (DateTime.Now.Subtract(_lastMinuteReportTime).TotalMilliseconds > MINUTE_INTERVAL_MS)
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
        double cpuAvg = _minuteCpuSamples.Count > 0 
            ? _minuteCpuSamples.Average()
            : 0;

        long ramBytesAvg = _minuteRamBytesSamples.Count > 0
            ? (long)_minuteRamBytesSamples.Average()
            : 0;

        double ramPercentAvg = (double)ramBytesAvg / _systemMem * 100;
        
        long ramBytesPeak = _minuteRamBytesSamples.Count > 0 
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

    public void Dispose()
    {
        _cts.Cancel();
        _metricsChannel.Writer.Complete();
        
        Task.WaitAll([_processingTask, _reportingTask], TimeSpan.FromSeconds(3));
        _cts.Dispose();
        
        GC.SuppressFinalize(this);
        _logger.LogInformation("ServerMetricsBuffer disposed");
    }
}
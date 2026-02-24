using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LSL.Common.Collections;
using LSL.Common.DTOs;
using LSL.Common.Utilities;
using LSL.Models.Server;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ServerServices;

/// <summary>
///     Aggregates process metrics and exposes reactive streams.
/// </summary>
public class ServerMetricsBuffer : IDisposable
{
    private const int HistoryMinutes = 30;
    private const int ReportIntervalMs = 1000;
    private const int MinuteIntervalMs = 60000;
    private static readonly long s_systemMem = MemoryInfo.CurrentSystemMemory;

    private readonly ILogger<ServerMetricsBuffer> _logger;
    private readonly CancellationTokenSource _cts;
    private readonly Channel<ProcessMetrics> _metricsChannel;
    private readonly Task _processingTask;
    private readonly Task _reportingTask;

    // 当前秒级数据
    private readonly ConcurrentDictionary<int, (double Cpu, long RamBytes, double RamPercent)> _currentMetrics = new();

    // 分钟采样
    private readonly List<double> _minuteCpuSamples = [];
    private readonly List<long> _minuteRamBytesSamples = [];

    // 历史记录
    private readonly RangedObservableLinkedList<double> _cpuHistory = new(HistoryMinutes, 0, false);
    private readonly RangedObservableLinkedList<long> _ramBytesAvgHistory = new(HistoryMinutes, 0, false);
    private readonly RangedObservableLinkedList<long> _ramBytesPeakHistory = new(HistoryMinutes, 0, false);
    private readonly RangedObservableLinkedList<double> _ramPercentHistory = new(HistoryMinutes, 0, false);

    private DateTime _lastMinuteReportTime = DateTime.Now;

    // 发布源，缓存最近1分钟
    private readonly ReplaySubject<MetricsUpdateArgs> _metricsUpdateSubject = new(TimeSpan.FromMinutes(1));
    private readonly ReplaySubject<GeneralMetricsArgs> _generalMetricsSubject = new(1);

    public ServerMetricsBuffer(ILogger<ServerMetricsBuffer> logger)
    {
        _logger = logger;
        _cts = new CancellationTokenSource();
        _metricsChannel = Channel.CreateUnbounded<ProcessMetrics>(
            new UnboundedChannelOptions { SingleReader = true });

        _processingTask = Task.Run(() => ProcessMetricsAsync(_cts.Token));
        _reportingTask = Task.Run(() => ReportMetricsAsync(_cts.Token));

        _logger.LogInformation("ServerMetricsBuffer initialized");
    }

    // 公共IObservable
    public IObservable<MetricsUpdateArgs> MetricsUpdates => _metricsUpdateSubject.AsObservable();
    public IObservable<GeneralMetricsArgs> GeneralMetrics => _generalMetricsSubject.AsObservable();

    public bool TryWrite(ProcessMetrics args) => _metricsChannel.Writer.TryWrite(args);

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

        _metricsUpdateSubject.OnNext(new MetricsUpdateArgs(reports));
    }

    private void ReportPerMinuteMetrics()
    {
        var cpuAvg = _minuteCpuSamples.Count > 0 ? _minuteCpuSamples.Average() : 0;
        var ramBytesAvg = _minuteRamBytesSamples.Count > 0 ? (long)_minuteRamBytesSamples.Average() : 0;
        var ramPercentAvg = (double)ramBytesAvg / s_systemMem * 100;
        var ramBytesPeak = _minuteRamBytesSamples.Count > 0 ? _minuteRamBytesSamples.Max() : 0;

        _cpuHistory.Add(cpuAvg);
        _ramPercentHistory.Add(ramPercentAvg);
        _ramBytesAvgHistory.Add(ramBytesAvg);
        _ramBytesPeakHistory.Add(ramBytesPeak);

        _generalMetricsSubject.OnNext(new GeneralMetricsArgs(
            _cpuHistory,
            _ramPercentHistory,
            _ramBytesAvgHistory,
            _ramBytesPeakHistory));

        // 重置分钟数据
        _minuteCpuSamples.Clear();
        _minuteRamBytesSamples.Clear();
    }

    private (double Cpu, long RamBytes, double RamPercent) GetValidatedMetrics(ProcessMetrics args)
    {
        if (args.IsProcessExited || args.Error != null)
            return (0, 0, 0);

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
        GC.SuppressFinalize(this);
        _cts.Cancel();
        _metricsChannel.Writer.TryComplete();
        try { Task.WaitAll([_processingTask, _reportingTask], TimeSpan.FromSeconds(2)); } catch { /*ignore*/ }

        _metricsUpdateSubject.Dispose();
        _generalMetricsSubject.Dispose();
        _cts.Dispose();

        _logger.LogInformation("ServerMetricsBuffer disposed");
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using LSL.Common.Helpers;

namespace LSL.Services;

public class ProcessMetricsMonitor : IDisposable
{
    public event EventHandler<ProcessMetricsEventArgs>? MetricsUpdated;
    private readonly Timer _timer;
    private readonly Process _process;
    private TimeSpan _prevCpuTime;
    private DateTime _prevTime;
    private static readonly ulong _totalMemoryBytes = MemoryInfo.GetTotalSystemMemory();
    private readonly object _lock = new();
    private bool _disposed;

    public ProcessMetricsMonitor(Process process, int interval = 1000)
    {
        _process = process;
        _prevCpuTime = process.TotalProcessorTime;
        _prevTime = DateTime.UtcNow;
        
        // 创建定时器（首次触发在1秒后，之后每秒触发）
        _timer = new Timer(OnTimerCallback, null, interval, interval);
    }

    private void OnTimerCallback(object? state)
    {
        lock (_lock)
        {
            if (_disposed) return;

            double cpuUsage = 0;
            long processMemory = 0;
            bool isExited = false;

            try
            {
                // 检查进程是否已退出
                isExited = _process.HasExited;
                
                if (!isExited)
                {
                    // 计算CPU使用率
                    var currentTime = DateTime.UtcNow;
                    var currentCpuTime = _process.TotalProcessorTime;
                    
                    var cpuElapsed = (currentCpuTime - _prevCpuTime).TotalSeconds;
                    var timeElapsed = (currentTime - _prevTime).TotalSeconds;
                    
                    _prevTime = currentTime;
                    _prevCpuTime = currentCpuTime;

                    if (timeElapsed > 0 && cpuElapsed > 0)
                    {
                        cpuUsage = (cpuElapsed / timeElapsed) * 100;
                        cpuUsage /= Environment.ProcessorCount; // 多核百分比
                    }

                    // 计算内存使用率
                    processMemory = _process.WorkingSet64;
                }
            }
            catch (InvalidOperationException)
            {
                // 进程已退出或无法访问
                isExited = true;
            }
            catch (Exception ex)
            {
                // 触发包含错误信息的事件
                MetricsUpdated?.Invoke(this, new ProcessMetricsEventArgs(0, 0, true, ex.Message));
                return;
            }

            // 触发事件（即使进程已退出也通知）
            MetricsUpdated?.Invoke(this, new ProcessMetricsEventArgs(
                cpuUsage, 
                processMemory, 
                isExited
            ));
        }
    }
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _timer?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

// 事件参数类
public class ProcessMetricsEventArgs : EventArgs
{
    /// <summary>多核CPU使用百分比</summary>
    public double CpuUsagePercent { get; }
    
    /// <summary>内存使用百分比</summary>
    public long MemoryUsageBytes { get; }
    
    /// <summary>进程是否已退出</summary>
    public bool IsProcessExited { get; }
    
    /// <summary>错误信息（如果有）</summary>
    public string Error { get; }

    public ProcessMetricsEventArgs(
        double cpuUsagePercent, 
        long memoryUsageBytes, 
        bool isProcessExited,
        string error = null)
    {
        CpuUsagePercent = cpuUsagePercent;
        MemoryUsageBytes = memoryUsageBytes;
        IsProcessExited = isProcessExited;
        Error = error;
    }
}
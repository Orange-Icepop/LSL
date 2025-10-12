using System;
using System.Diagnostics;
using System.Threading;

namespace LSL.Services.ServerServices;

/// <summary>
/// A metrics monitor attached to a Process instance.
/// </summary>
public class ProcessMetricsMonitor : IDisposable
{
    private readonly int _id;
    public event EventHandler<ProcessMetricsEventArgs>? MetricsUpdated;
    private readonly Timer _timer;
    private readonly Process _process;
    private readonly long _allocatedMemoryBytes;
    private TimeSpan _prevCpuTime;
    private DateTime _prevTime;
    private readonly object _lock = new();
    private bool _disposed;

    public ProcessMetricsMonitor(Process process, int id, long allocatedMemoryBytes, int interval = 1000)
    {
        _process = process;
        _id = id;
        _allocatedMemoryBytes = allocatedMemoryBytes;
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
            bool isExited;

            try
            {
                // 检查进程是否已退出
                isExited = _process.HasExited;
                
                if (!isExited)
                {
                    _process.Refresh();
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

                    // 计算内存使用量
                    processMemory = _process.PrivateMemorySize64;
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
                MetricsUpdated?.Invoke(this, new ProcessMetricsEventArgs(_id, 0, 0, 0, true, ex.Message));
                return;
            }

            // 触发事件（即使进程已退出也通知）
            MetricsUpdated?.Invoke(this, new ProcessMetricsEventArgs(
                _id,
                cpuUsage, 
                processMemory, 
                _allocatedMemoryBytes,
                isExited
            ));
        }
    }
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            MetricsUpdated?.Invoke(this, new ProcessMetricsEventArgs(
                _id,
                0, 
                0, 
                _allocatedMemoryBytes,
                true
            ));
            _timer.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
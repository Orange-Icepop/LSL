using System;

namespace LSL.Services.ServerServices;
/// <summary>
/// Contains information about the CPU time and RAM usage of a Process.
/// </summary>
/// <param name="cpuUsagePercent"></param>
/// <param name="memoryUsageBytes"></param>
/// <param name="allocatedMemoryBytes"></param>
/// <param name="isProcessExited"></param>
/// <param name="error"></param>
public class ProcessMetricsEventArgs(
    int serverId,
    double cpuUsagePercent,
    long memoryUsageBytes,
    long allocatedMemoryBytes,
    bool isProcessExited,
    string? error = null)
    : EventArgs
{
    /// <summary>The registered server's ID of this message.</summary>
    public int ServerId { get; } = serverId;
    
    /// <summary>CPU usage of all cores.</summary>
    public double CpuUsagePercent { get; } = cpuUsagePercent;

    /// <summary>RAM usage amount in bytes.</summary>
    public long MemoryUsageBytes { get; } = memoryUsageBytes;

    /// <summary>RAM usage percent.</summary>
    public double MemoryUsagePercent { get; } = (double)memoryUsageBytes / allocatedMemoryBytes * 100;
    
    /// <summary>Whether the server process has exited or not.</summary>
    public bool IsProcessExited { get; } = isProcessExited;

    /// <summary>Optional error information.</summary>
    public string? Error { get; } = error;
}
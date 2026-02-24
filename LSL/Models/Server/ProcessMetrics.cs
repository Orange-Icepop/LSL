namespace LSL.Models.Server;

/// <summary>
///     Contains information about the CPU time and RAM usage of a Process.
/// </summary>
public record ProcessMetrics
{
    public ProcessMetrics(int serverId,
        double cpuUsagePercent,
        long memoryUsageBytes,
        long allocatedMemoryBytes,
        bool isProcessExited,
        string? error = null)
    {
        ServerId = serverId;
        CpuUsagePercent = cpuUsagePercent;
        MemoryUsageBytes = memoryUsageBytes;
        MemoryUsagePercent = (double)memoryUsageBytes / allocatedMemoryBytes * 100;
        IsProcessExited = isProcessExited;
        Error = error;
    }

    /// <summary>The registered server's ID of this message.</summary>
    public int ServerId { get; init; }

    /// <summary>CPU usage of all cores.</summary>
    public double CpuUsagePercent { get; init; }

    /// <summary>RAM usage amount in bytes.</summary>
    public long MemoryUsageBytes { get; init; }

    /// <summary>RAM usage percent.</summary>
    public double MemoryUsagePercent { get; init; }

    /// <summary>Whether the server process has exited or not.</summary>
    public bool IsProcessExited { get; init; }

    /// <summary>Optional error information.</summary>
    public string? Error { get; init; }
}
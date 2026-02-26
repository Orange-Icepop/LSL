namespace LSL.Common.DTOs;

/// <summary>Contracts for methods handling server metrics information in LSL.</summary>
public interface IMetricsArgs : IStorageArgs;

/// <summary>
///     The report of metrics of a single server which will be sent once a second.
/// </summary>
/// <param name="ServerId">The registered ID of the monitored server process.</param>
/// <param name="CpuUsage">The CPU multicore usage percent that has been kept integer.</param>
/// <param name="MemUsage">The RAM usage percent that has been kept integer.</param>
/// <param name="MemBytes">How many bytes of RAM has been used by this server.</param>
public record SecondlyMetricsReport(int ServerId, double CpuUsage, long MemBytes, double MemUsage) : IMetricsArgs;
public record MinutelyMetricsReport(int ServerId, double CpuUsage, long MemBytes, double MemUsage) : IMetricsArgs;
public record GlobalSecondlyMetricsReport(DateTime Timestamp, double CpuUsage, long MemBytes, double MemUsage) : IMetricsArgs;
public record GlobalMinutelyMetricsReport(DateTime Timestamp, double CpuUsage, long MemBytes, double MemUsage) : IMetricsArgs;
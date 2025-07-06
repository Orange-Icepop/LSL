namespace LSL.Common.Contracts;

/// <summary>Contracts for methods handling server metrics information in LSL.</summary>
public interface IMetricsArgs;

/// <summary>The report of metrics of a single server which will be sent once a second. Will be wrapped in an IEnumerable. Not a legal IMetricsArgs.</summary>
/// <param name="ServerId">The registered ID of the monitored server process.</param>
/// <param name="CpuUsage">The CPU multicore usage percent that has been kept integer.</param>
/// <param name="MemUsage">The RAM usage percent that has been kept integer.</param>
/// <param name="MemBytes">How many bytes of RAM has been used by this server.</param>
public record MetricsReport(int ServerId, int CpuUsage, long MemBytes, int MemUsage);


/// <summary>Message that contains the current second's metrics of all servers that are running now.</summary>
/// <param name="Metrics">An IEnumerable of MetricsReport instances.</param>
public record MetricsUpdateArgs(IEnumerable<MetricsReport> Metrics) : IMetricsArgs;


/// <summary>Minutely metrics report of recent 30 mins.</summary>
/// <param name="CpuHistory"></param>
/// <param name="RamHistory"></param>
public record GeneralMetricsArgs(IEnumerable<uint> CpuHistory, IEnumerable<uint> RamHistory) : IMetricsArgs;
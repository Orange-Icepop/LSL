using LSL.Common.Collections;
using LSL.Common.DTOs;

namespace LSL.Common.Models.Minecraft;

/// <summary>
///     A wrapper of a server's historic metrics record.
/// </summary>
public class MetricsStorage
{
    public MetricsStorage(bool notifiable = true)
    {
        CpuPct = new RangedObservableLinkedList<double>(30, 0, notifiable);
        MemCnt = new RangedObservableLinkedList<long>(30, 0, notifiable);
        MemPct = new RangedObservableLinkedList<double>(30, 0, notifiable);
    }

    public MetricsStorage(SecondlyMetricsReport report, bool notifiable = true) : this(notifiable)
    {
        Add(report);
    }

    public RangedObservableLinkedList<double> CpuPct { get; }
    public RangedObservableLinkedList<long> MemCnt { get; }
    public RangedObservableLinkedList<double> MemPct { get; }

    public MetricsStorage Add(SecondlyMetricsReport report)
    {
        CpuPct.Add(report.CpuUsage);
        MemCnt.Add(report.MemBytes);
        MemPct.Add(report.MemUsage);
        return this;
    }
}
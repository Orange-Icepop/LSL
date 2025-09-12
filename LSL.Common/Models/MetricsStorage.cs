using LSL.Common.Collections;
using LSL.Common.DTOs;

namespace LSL.Common.Models;

/// <summary>
/// A wrapper of a server's historic metrics record.
/// </summary>
public class MetricsStorage
{
    public RangedObservableLinkedList<double> CpuPct { get; }
    public RangedObservableLinkedList<long> MemCnt { get; }
    public RangedObservableLinkedList<double> MemPct { get; }

    public MetricsStorage(bool notifiable = false)
    {
        CpuPct = new RangedObservableLinkedList<double>(30, 0, notifiable);
        MemCnt = new RangedObservableLinkedList<long>(30, 0, notifiable);
        MemPct = new RangedObservableLinkedList<double>(30, 0, notifiable);
    }

    public MetricsStorage(MetricsReport report, bool notifiable = false) : this(notifiable)
    {
        this.Add(report);
    }

    public MetricsStorage Add(MetricsReport report)
    {
        CpuPct.Add(report.CpuUsage);
        MemCnt.Add(report.MemBytes);
        MemPct.Add(report.MemUsage);
        return this;
    }
}
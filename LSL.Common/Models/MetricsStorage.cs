using LSL.Common.Collections;
using LSL.Common.Contracts;

namespace LSL.Common.Models;

/// <summary>
/// A wrapper of a server's historic metrics record.
/// </summary>
public class MetricsStorage
{
    public RangedObservableLinkedList<uint> CpuPct { get; }
    public RangedObservableLinkedList<long> MemCnt { get; }
    public RangedObservableLinkedList<uint> MemPct { get; }

    public MetricsStorage(bool notifiable = false)
    {
        CpuPct = new RangedObservableLinkedList<uint>(30, 0, notifiable);
        MemCnt = new RangedObservableLinkedList<long>(30, 0, notifiable);
        MemPct = new RangedObservableLinkedList<uint>(30, 0, notifiable);
    }

    public MetricsStorage(MetricsReport report, bool notifiable = false) : this(notifiable)
    {
        this.Add(report);
    }

    public void Add(MetricsReport report)
    {
        CpuPct.Add((uint)report.CpuUsage);
        MemCnt.Add(report.MemBytes);
        MemPct.Add((uint)report.MemUsage);
    }
}
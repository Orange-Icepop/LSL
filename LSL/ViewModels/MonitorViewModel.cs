using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using LSL.Common.Contracts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class MonitorViewModel : RegionalVMBase
{
    [Reactive] public RangedObservableLinkedList<uint> CurrentCpuMetrics { get; set; } = new(30, 0);
    [Reactive] public RangedObservableLinkedList<uint> CurrentRamMetrics { get; set; } = new(30, 0);

    public Dictionary<int, MetricsStorage> MetricsStorage { get; } = new();
    public MonitorViewModel(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
    {
        
    }
}

public class MetricsStorage
{
    public RangedObservableLinkedList<uint> CpuPct { get; } = new(30, 0);
    public RangedObservableLinkedList<long> MemCnt { get; } = new(30, 0);
    public RangedObservableLinkedList<uint> MemPct { get; } = new(30, 0);

    public void Add(MetricsReport report)
    {
        CpuPct.Add((uint)report.CpuUsage);
        MemCnt.Add(report.MemBytes);
        MemPct.Add((uint)report.MemUsage);
    }
}
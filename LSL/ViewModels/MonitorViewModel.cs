using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using LSL.Common.Collections;
using LSL.Common.Contracts;
using LSL.Common.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class MonitorViewModel : RegionalVMBase
{
    [Reactive] public RangedObservableLinkedList<uint> CurrentCpuMetrics { get; set; } = new(30, 0);
    [Reactive] public RangedObservableLinkedList<uint> CurrentRamMetrics { get; set; } = new(30, 0);

    public MonitorViewModel(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
    {
        AppState.ServerIdChanged.Select(id => AppState.MetricsDict.GetOrAdd(id, i => new MetricsStorage(true)))
            .Subscribe(ms =>
            {
                CurrentCpuMetrics = ms.CpuPct;
                CurrentRamMetrics = ms.MemPct;
            });
    }
}
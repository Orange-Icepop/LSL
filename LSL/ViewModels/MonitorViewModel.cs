using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
    [Reactive] public RangedObservableLinkedList<long> CurrentRamValueMetrics { get; set; } = new(30, 0);
    [Reactive] public uint CurrentCpuUsage { get; private set; }
    [Reactive] public uint CurrentRamUsage { get; private set; }
    [Reactive] public long CurrentRamValue { get; private set; }
    [Reactive] public long CurrentRamMax { get; private set; }

    public MonitorViewModel(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
    {
        AppState.ServerIdChanged.Select(id => AppState.MetricsDict.GetOrAdd(id, i => new MetricsStorage(true)))
            .Subscribe(ms =>
            {
                CurrentCpuMetrics.CollectionChanged -= OnCpuMetricsChanged;
                CurrentRamMetrics.CollectionChanged -= OnMemMetricsChanged;
                CurrentRamValueMetrics.CollectionChanged -= OnMemValueMetricsChanged;
                CurrentCpuMetrics = ms.CpuPct;
                CurrentRamMetrics = ms.MemPct;
                CurrentRamValueMetrics = ms.MemCnt;
                CurrentCpuMetrics.CollectionChanged += OnCpuMetricsChanged;
                CurrentRamMetrics.CollectionChanged += OnMemMetricsChanged;
                CurrentRamValueMetrics.CollectionChanged += OnMemValueMetricsChanged;
            });
        AppState.ServerIdChanged.Select(id => AppState.CurrentServerConfigs.GetOrAdd(id, k => ServerConfig.None))
            .Subscribe(sc =>
            {
                CurrentRamMax = (long)sc.max_memory * 1024 * 1024;
            });
    }

    private void OnCpuMetricsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CurrentCpuUsage = CurrentCpuMetrics.LastItem;
    }
    private void OnMemMetricsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CurrentRamUsage = CurrentRamMetrics.LastItem;
    }
    private void OnMemValueMetricsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CurrentRamValue = CurrentRamValueMetrics.LastItem;
    }
}
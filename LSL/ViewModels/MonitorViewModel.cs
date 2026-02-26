using System;
using System.Collections.Specialized;
using System.Reactive.Linq;
using LSL.Common.Collections;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;
using LSL.Common.Utilities;
using ReactiveUI.SourceGenerators;

namespace LSL.ViewModels;

public partial class MonitorViewModel : RegionalViewModelBase<MonitorViewModel>
{
    public MonitorViewModel(AppStateLayer appState, ServiceConnector connector, DialogCoordinator coordinator,
        PublicCommand commands) : base(appState, connector, coordinator, commands)
    {
        AppState.ServerIdChanged.Select(id => AppState.MetricsDict.GetOrAdd(id, _ => new MetricsStorage()))
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
        AppState.ServerIdChanged.Select(id =>
                AppState.CurrentServerConfigs.TryGetValue(id, out var value) ? value : IndexedServerConfig.None)
            .Subscribe(sc => { CurrentRamMax = (long)sc.MaxMemory * 1024 * 1024; });
        AppState.ServerConfigChanged.Select(dict =>
                dict.TryGetValue(AppState.SelectedServerId, out var value) ? value : IndexedServerConfig.None)
            .Subscribe(sc => { CurrentRamMax = (long)sc.MaxMemory * 1024 * 1024; });
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => AppState.GeneralCpuMetrics.CollectionChanged += handler,
                handler => AppState.GeneralCpuMetrics.CollectionChanged -= handler)
            .Subscribe(_ => CurrentGeneralCpuUsage = AppState.GeneralCpuMetrics.LastItem);
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => AppState.GeneralRamMetrics.CollectionChanged += handler,
                handler => AppState.GeneralRamMetrics.CollectionChanged -= handler)
            .Subscribe(_ => CurrentGeneralRamUsage = AppState.GeneralRamMetrics.LastItem);
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => AppState.GeneralRamCount.CollectionChanged += handler,
                handler => AppState.GeneralRamCount.CollectionChanged -= handler)
            .Subscribe(_ => CurrentGeneralRamValue = AppState.GeneralRamCount.LastItem);
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

    #region 当前性能占用绑定器

    [Reactive] public partial RangedObservableLinkedList<double> CurrentCpuMetrics { get; set; } = new(30, 0);
    [Reactive] public partial RangedObservableLinkedList<double> CurrentRamMetrics { get; set; } = new(30, 0);
    [Reactive] public partial RangedObservableLinkedList<long> CurrentRamValueMetrics { get; set; } = new(30, 0);
    [Reactive] public partial double CurrentCpuUsage { get; private set; }
    [Reactive] public partial double CurrentRamUsage { get; private set; }
    [Reactive] public partial long CurrentRamValue { get; private set; }
    [Reactive] public partial long CurrentRamMax { get; private set; }

    #endregion

    #region 全局性能占用绑定器

    [Reactive] public partial double CurrentGeneralCpuUsage { get; private set; }
    [Reactive] public partial double CurrentGeneralRamUsage { get; private set; }
    [Reactive] public partial long CurrentGeneralRamValue { get; private set; }
    public static long SystemRamMax => MemoryInfo.CurrentSystemMemory;

    #endregion
}
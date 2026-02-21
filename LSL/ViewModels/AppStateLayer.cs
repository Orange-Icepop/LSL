using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using LSL.Common.Collections;
using LSL.Common.Models.AppConfig;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;
using LSL.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LSL.ViewModels;

public partial class AppStateLayer : ReactiveObject
{
    public AppStateLayer(ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        Logger = LoggerFactory.CreateLogger<AppStateLayer>();
        CurrentBarState = BarState.Common;
        CurrentGeneralPage = GeneralPageState.Home;
        CurrentRightPage = RightPageState.HomeRight;
        // 自获取属性初始化
        _serverIDs = [];
        _serverNames = [];
        _selectedServerId = -1;
        _totalServerCount = 0;
        RunningServerCount = 0;
        // end
        MessageBus.Current.Listen<NavigateArgs>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(Navigate);
        MessageBus.Current.Listen<NavigateCommand>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(arg => arg.CommandType)
            .Subscribe(NavigateCommandHandler);
        // 配置公共监听属性
        ServerConfigChanged = this.WhenAnyValue(stateLayer => stateLayer._currentServerConfigs)
            .ObserveOn(RxApp.MainThreadScheduler);
        ServerIndexChanged = this.WhenAnyValue(stateLayer => stateLayer.SelectedServerIndex)
            .ObserveOn(RxApp.MainThreadScheduler);
        ServerIdChanged = this.WhenAnyValue(stateLayer => stateLayer.SelectedServerId)
            .ObserveOn(RxApp.MainThreadScheduler);

        #region 监听

        // 配置文件更新的连带更新
        _serverIDsHelper = ServerConfigChanged.Select(s => new ObservableCollection<int>(s.Keys))
            .ToProperty(this, x=>x.ServerIDs);
        _serverNamesHelper = ServerConfigChanged.Select(s => new ObservableCollection<string>(s.Values.Select(v => v.ServerName)))
            .ToProperty(this, x=>x.ServerNames);
        ServerConfigChanged.Subscribe(configs =>
        {
            if (configs.Count == 0) return;
            SelectedServerIndex = 0;
            Logger.LogInformation("Selected server index reset to 0");
        });
        _notTemplateServerHelper = ServerConfigChanged.Select(configs => !configs.TryGetValue(-1, out _))
            .ToProperty(this, x => x.NotTemplateServer);
        _totalServerCountHelper = ServerConfigChanged.Select(configs =>
            {
                if (configs.Count == 0) return 0;
                return configs.TryGetValue(-1, out _) ? 0 : configs.Count;
            })
            .ToProperty(this, x => x.TotalServerCount);
        // 在索引更新时刷新右视图
        ServerIndexChanged.Subscribe(_ =>
        {
            if (!NavigationCollection.ServerRightPages.Contains(CurrentRightPage)) return;
            MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.Refresh));
        });
        // 更新服务器ID
        _selectedServerIdHelper = this.WhenAnyValue(x => x.SelectedServerIndex, x => x.ServerIDs)
            .Select(tup =>
            {
                if (tup.Item1 < 0) return -1;
                if (tup.Item2.Count > tup.Item1) return tup.Item2[tup.Item1];
                return -1;
            })
            .ToProperty(this, x => x.SelectedServerId);

        #endregion
    }

    public ILoggerFactory LoggerFactory { get; }
    private ILogger<AppStateLayer> Logger { get; }
    public IObservable<ImmutableDictionary<int, IndexedServerConfig>> ServerConfigChanged { get; }
    public IObservable<int> ServerIndexChanged { get; }
    public IObservable<int> ServerIdChanged { get; private set; }

    #region 导航相关

    [Reactive] public partial BarState CurrentBarState { get; private set; }
    [Reactive] public partial GeneralPageState CurrentGeneralPage { get; private set; }
    [Reactive] public partial RightPageState CurrentRightPage { get; private set; }

    private (BarState, GeneralPageState, RightPageState) _lastPage = (BarState.Common, GeneralPageState.Undefined,
        RightPageState.Undefined);

    private void Navigate(BarState bar, GeneralPageState gen, RightPageState right)
    {
        Navigate(new NavigateArgs { BarTarget = bar, LeftTarget = gen, RightTarget = right });
    }

    private void Navigate(NavigateArgs args) // ASL不负责查重操作
    {
        (BarState, GeneralPageState, RightPageState) lastPage = (BarState.Common, CurrentGeneralPage,
            CurrentRightPage);
        if (args.LeftTarget != GeneralPageState.Undefined) CurrentGeneralPage = args.LeftTarget;

        if (args.RightTarget != RightPageState.Undefined) CurrentRightPage = args.RightTarget;

        if (args.BarTarget != BarState.Undefined)
        {
            if (args.BarTarget == BarState.FullScreen && CurrentBarState == BarState.Common)
            {
                CurrentBarState = BarState.FullScreen;
            }
            else if (args.BarTarget == BarState.Common && CurrentBarState == BarState.FullScreen)
            {
                CurrentBarState = BarState.Common;
                if (_lastPage != (BarState.Common, GeneralPageState.Undefined, RightPageState.Undefined))
                {
                    CurrentBarState = _lastPage.Item1;
                    CurrentGeneralPage = _lastPage.Item2;
                    CurrentRightPage = _lastPage.Item3;
                }
                else
                {
                    CurrentGeneralPage = GeneralPageState.Home;
                    CurrentRightPage = RightPageState.HomeRight;
                }

                lastPage = (BarState.Common, GeneralPageState.Undefined, RightPageState.Undefined);
            }
            else
            {
                CurrentBarState = args.BarTarget;
            }
        }

        _lastPage = lastPage;
    }

    private void NavigateCommandHandler(NavigateCommandType command)
    {
        switch (command)
        {
            case NavigateCommandType.FullScreenToCommon:
                Navigate(_lastPage.Item1, _lastPage.Item2, _lastPage.Item3);
                break;
            case NavigateCommandType.Refresh:
            {
                /*
                var last = CurrentRightPage;
                Navigate(BarState.Undefined, GeneralPageState.Undefined, RightPageState.Empty);
                Navigate(BarState.Undefined, GeneralPageState.Undefined, last);*/
                break;
            }
        }
    }

    #endregion

    #region 配置相关

    [Reactive] private DaemonConfig _daemonConfigs = new();
    [Reactive] private WebConfig _webConfigs = new();
    [Reactive] private DesktopConfig _desktopConfigs = new();

    [Reactive]
    private ImmutableDictionary<int, IndexedServerConfig> _currentServerConfigs =
        ImmutableDictionary<int, IndexedServerConfig>.Empty;

    [Reactive]
    private ImmutableDictionary<int, JavaInfo> _currentJavaDict = ImmutableDictionary<int, JavaInfo>.Empty;

    #endregion

    #region 选项相关

    public int SelectedServerIndex
    {
        get;
        set
        {
            int fin;
            if (_currentServerConfigs.Count == 0)
                fin = -1;
            else if (value >= _currentServerConfigs.Count)
                fin = 0;
            else fin = value;

            this.RaiseAndSetIfChanged(ref field, fin);
        }
    } = -1;

    [ObservableAsProperty] private int _selectedServerId;
    [ObservableAsProperty] private ObservableCollection<int> _serverIDs;
    [ObservableAsProperty] private ObservableCollection<string> _serverNames;

    #endregion

    #region 服务器相关

    [Reactive] private ConcurrentDictionary<int, ObservableCollection<ColoredLine>> _terminalTexts = new();

    [Reactive] private ConcurrentDictionary<int, ServerStatus> _serverStatuses = new();
    [Reactive] private ConcurrentDictionary<int, ObservableCollection<PlayerInfo>> _userDict = new();

    [Reactive]
    private ConcurrentDictionary<int, ObservableCollection<UserMessageLine>> _messageDict = new();

    [ObservableAsProperty] private int _totalServerCount;
    [Reactive] private int _runningServerCount;
    [ObservableAsProperty] private bool _notTemplateServer;

    #endregion

    #region 性能监控相关

    [Reactive] private ConcurrentDictionary<int, MetricsStorage> _metricsDict = new();
    [Reactive] private RangedObservableLinkedList<double> _generalCpuMetrics = new(30, 0);
    [Reactive] private RangedObservableLinkedList<double> _generalRamMetrics = new(30, 0);
    public event EventHandler<GeneralMetricsEventArgs>? GeneralMetricsEventHandler;

    public void OnGeneralMetricsUpdated(double cpu, double ram, long memVal)
    {
        GeneralMetricsEventHandler?.Invoke(this, new GeneralMetricsEventArgs(cpu, ram, memVal));
    }

    #endregion
}
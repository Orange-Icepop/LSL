using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Threading;
using LSL.Common.Models;
using LSL.Common.Validation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class ConfigViewModel : RegionalViewModelBase<ConfigViewModel>
{
    public ConfigViewModel(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
    {
        JavaVersions = null!;
        SelectedServerConfig = ServerConfig.None;
        SelectedServerName = string.Empty;
        SelectedServerPath = string.Empty;
        AppState.WhenAnyValue(stateLayer => stateLayer.CurrentJavaDict)
            .Select(s => new FlatTreeDataGridSource<JavaInfo>(s.Values)
            {
                Columns =
                {
                    new TextColumn<JavaInfo, string>("版本", x => x.Version),
                    new TextColumn<JavaInfo, string>("路径", x => x.Path),
                    new TextColumn<JavaInfo, string>("提供商", x => x.Vendor),
                    new TextColumn<JavaInfo, string>("架构", x => x.Architecture),
                },
            })
            .ToPropertyEx(this, x => x.JavaVersions);
        AppState.WhenAnyValue(x => x.CurrentServerConfigs, x => x.SelectedServerId)
            .Subscribe(scc => RaiseServerConfigChanged(scc.Item2, scc.Item1));
        DeleteServerCmd = ReactiveCommand.CreateFromTask(async () => await DeleteServer());
    }

    public async Task Init()
    {
        await GetConfigAsync(true); // cached config需要手动同步，不能依赖自动更新
        await Connector.ReadServerConfig(true); // 服务器配置由于较为复杂，统一为手动控制
        await Connector.ReadJavaConfig(true);
    }

    private ConcurrentDictionary<string, object> _cachedConfig = [];

    #region 核心配置数据

    public bool AutoEula
    {
        get => (bool)_cachedConfig["auto_eula"];
        set => CacheConfig("auto_eula", value);
    }

    public int AppPriority
    {
        get => (int)_cachedConfig["app_priority"];
        set => CacheConfig("app_priority", value);
    }

    public bool EndServerWhenClose
    {
        get => (bool)_cachedConfig["end_server_when_close"];
        set => CacheConfig("end_server_when_close", value);
    }

    public bool Daemon
    {
        get => (bool)_cachedConfig["daemon"];
        set => CacheConfig("daemon", value);
    }

    public bool ColoringTerminal
    {
        get => (bool)_cachedConfig["coloring_terminal"];
        set => CacheConfig("coloring_terminal", value);
    }

    public int DownloadSource
    {
        get => (int)_cachedConfig["download_source"];
        set => CacheConfig("download_source", value);
    }

    public int DownloadThreads
    {
        get => (int)_cachedConfig["download_threads"];
        set => CacheConfig("download_threads", value);
    }

    [DownloadLimitValidator]
    public string? DownloadLimit
    {
        get => _cachedConfig["download_limit"].ToString();
        set => CacheConfig("download_limit", value);
    }

    public bool PanelEnable
    {
        get => (bool)_cachedConfig["panel_enable"];
        set => CacheConfig("panel_enable", value);
    }

    [PanelPortValidator]
    public string? PanelPort
    {
        get => _cachedConfig["panel_port"].ToString();
        set => CacheConfig("panel_port", value);
    }

    public bool PanelMonitor
    {
        get => (bool)_cachedConfig["panel_monitor"];
        set => CacheConfig("panel_monitor", value);
    }

    public bool PanelTerminal
    {
        get => (bool)_cachedConfig["panel_terminal"];
        set => CacheConfig("panel_terminal", value);
    }

    public bool AutoUpdate
    {
        get => (bool)_cachedConfig["auto_update"];
        set => CacheConfig("auto_update", value);
    }

    public bool BetaUpdate
    {
        get => (bool)_cachedConfig["beta_update"];
        set => CacheConfig("beta_update", value);
    }

    #endregion

    #region 主配置操作

    public async Task GetConfigAsync(bool rf = false)
    {
        await Connector.ReadMainConfig(rf);
        await Dispatcher.UIThread.InvokeAsync(() => _cachedConfig = new ConcurrentDictionary<string, object>(AppState.CurrentConfigs));
    }

    private void CacheConfig(string key, object? value) // 向缓存字典中写入新配置
    {
        if (value == null) return;
        _cachedConfig.AddOrUpdate(key, _ => value, (_, _) => value);
    }

    public async Task ConfirmConfigAsync()
    {
        AppState.CurrentConfigs = _cachedConfig.ToFrozenDictionary();
        await Connector.SaveConfig();
    }

    #endregion

    #region 服务器配置操作
    public ICommand DeleteServerCmd { get; }
    public async Task DeleteServer()
    {
        // 检查是否可以删除
        int serverId = AppState.SelectedServerId;
        if (serverId < 0)
        {
            await AppState.InteractionUnits.ThrowError("选定的服务器不存在", "你没有添加过服务器。该服务器是LSL提供的占位符，不支持删除。");
            return;
        }
        if (!AppState.ServerStatuses.TryGetValue(serverId, out var status) || !AppState.CurrentServerConfigs.TryGetValue(serverId, out var config))
        {
            await AppState.InteractionUnits.ThrowError("无法删除服务器", "指定的服务器不存在。");
            return;
        }
        else if (status.IsRunning)
        {
            await AppState.InteractionUnits.ThrowError("无法删除服务器", $"指定的服务器{config.Name}正在运行，请先关闭服务器再删除。");
            return;
        }
            
        var result1 = await AppState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningYesNo,
            $"确认删除服务器{config.Name}吗？",
            "注意！此操作不可逆！" + Environment.NewLine + "服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result1 == PopupResult.No) return;
        var result2 = await AppState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningYesNo,
            $"第二次确认，删除服务器{config.Name}吗？",
            "注意！此操作不可逆！" + Environment.NewLine + "服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result2 == PopupResult.No) return;
        var result3 = await AppState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningYesNo,
            $"最后一次确认，你确定要删除服务器{config.Name}吗？",
            "这是最后一次警告！此操作不可逆！" + Environment.NewLine + "服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result3 == PopupResult.No) return;
        var deleteResult = await Connector.DeleteServer(serverId);
        if (deleteResult)
        {
            AppState.InteractionUnits.Notify(1, null, $"服务器{config.Name}删除成功");
        }
    }

    #endregion

    #region Java配置

    public FlatTreeDataGridSource<JavaInfo> JavaVersions { [ObservableAsProperty] get; }

    #endregion

    #region 服务器当前配置访问器

    [Reactive] public ServerConfig SelectedServerConfig { get; private set; }
    [Reactive] public string SelectedServerName { get; private set; }
    [Reactive] public string SelectedServerPath { get; private set; }

    private void RaiseServerConfigChanged(int serverId, FrozenDictionary<int,ServerConfig> serverConfig)
    {
        if (serverConfig.TryGetValue(serverId, out var config))
        {
            SelectedServerConfig = config;
            SelectedServerName = config.Name;
            SelectedServerPath = config.ServerPath;
        }
        else
        {
            var cache = ServerConfig.None;
            SelectedServerConfig = cache;
            SelectedServerName = cache.Name;
            SelectedServerPath = cache.ServerPath;
        }
    }
    #endregion
}
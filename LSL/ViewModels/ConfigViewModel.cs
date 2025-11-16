using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Threading;
using LSL.Common.Models;
using LSL.Common.Validation;
using Microsoft.Extensions.Logging;
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

    public async Task<bool> Init()
    {
        try
        {
            var res1 = await Connector.ReadMainConfig(true);
            if (res1.IsError) throw res1.Error;
            var res2 = await Connector.ReadServerConfig(true);
            if (res2.IsError) throw res2.Error;
            else await AppState.InteractionUnits.SubmitServiceError(res2);
            var res3 = await Connector.ReadJavaConfig(true);
            if (res3.IsError) throw res3.Error;
            else await AppState.InteractionUnits.SubmitServiceError(res3);
            return true;
        }
        catch (Exception e)
        {
            Logger.LogCritical(e, "A fatal error occured when initializing LSL.");
            MessageBus.Current.SendMessage(new ViewModelFatalError(e, "A fatal error occured when initializing LSL.", "初始化LSL时发生了致命错误。"));
            return false;
        }
    }

    private ConcurrentDictionary<string, object> _cachedConfig = [];

    #region 核心配置数据

    public bool AutoEula
    {
        get => (bool)_cachedConfig["auto_eula"];
        set => SaveConfigToCache("auto_eula", value);
    }

    public int AppPriority
    {
        get => (int)_cachedConfig["app_priority"];
        set => SaveConfigToCache("app_priority", value);
    }

    public bool EndServerWhenClose
    {
        get => (bool)_cachedConfig["end_server_when_close"];
        set => SaveConfigToCache("end_server_when_close", value);
    }

    public bool Daemon
    {
        get => (bool)_cachedConfig["daemon"];
        set => SaveConfigToCache("daemon", value);
    }

    public bool ColoringTerminal
    {
        get => (bool)_cachedConfig["coloring_terminal"];
        set => SaveConfigToCache("coloring_terminal", value);
    }

    public int DownloadSource
    {
        get => (int)_cachedConfig["download_source"];
        set => SaveConfigToCache("download_source", value);
    }

    public int DownloadThreads
    {
        get => (int)_cachedConfig["download_threads"];
        set => SaveConfigToCache("download_threads", value);
    }

    [DownloadLimitValidator]
    public string? DownloadLimit
    {
        get => _cachedConfig["download_limit"].ToString();
        set => SaveConfigToCache("download_limit", value);
    }

    public bool PanelEnable
    {
        get => (bool)_cachedConfig["panel_enable"];
        set => SaveConfigToCache("panel_enable", value);
    }

    [PanelPortValidator]
    public string? PanelPort
    {
        get => _cachedConfig["panel_port"].ToString();
        set => SaveConfigToCache("panel_port", value);
    }

    public bool PanelMonitor
    {
        get => (bool)_cachedConfig["panel_monitor"];
        set => SaveConfigToCache("panel_monitor", value);
    }

    public bool PanelTerminal
    {
        get => (bool)_cachedConfig["panel_terminal"];
        set => SaveConfigToCache("panel_terminal", value);
    }

    public bool AutoUpdate
    {
        get => (bool)_cachedConfig["auto_update"];
        set => SaveConfigToCache("auto_update", value);
    }

    public bool BetaUpdate
    {
        get => (bool)_cachedConfig["beta_update"];
        set => SaveConfigToCache("beta_update", value);
    }

    #endregion

    #region 主配置操作

    public async Task<bool> TryCacheConfigFromFileAsync(bool rf = false)
    {
        var success = await Connector.ReadMainConfig(rf);
        if (success.IsSuccess)
        {
            Dispatcher.UIThread.Invoke(() =>
                _cachedConfig = new ConcurrentDictionary<string, object>(AppState.CurrentConfigs));
            return true;
        }
        else
        {
            await AppState.InteractionUnits.SubmitServiceError(success);
            return false;
        }
    }

    private void SaveConfigToCache(string key, object? value) // 向缓存字典中写入新配置
    {
        if (value == null) return;
        _cachedConfig.AddOrUpdate(key, _ => value, (_, _) => value);
    }

    public async Task ConfirmConfigAsync()
    {
        var res = await Connector.SaveConfig();
        await AppState.InteractionUnits.SubmitServiceError(res);
        AppState.CurrentConfigs = _cachedConfig.ToFrozenDictionary();
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

        if (!AppState.ServerStatuses.TryGetValue(serverId, out var status) ||
            !AppState.CurrentServerConfigs.TryGetValue(serverId, out var config))
        {
            await AppState.InteractionUnits.ThrowError("无法删除服务器", "指定的服务器不存在。");
            return;
        }
        else if (status.IsRunning)
        {
            await AppState.InteractionUnits.ThrowError("无法删除服务器", $"指定的服务器{config.Name}正在运行，请先关闭服务器再删除。");
            return;
        }

        var result1 = await AppState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo,
            $"确认删除服务器{config.Name}吗？",
            "注意！此操作不可逆！\n服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result1 == PopupResult.No) return;
        var result2 = await AppState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo,
            $"第二次确认，删除服务器{config.Name}吗？",
            "注意！此操作不可逆！\n服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result2 == PopupResult.No) return;
        var result3 = await AppState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo,
            $"最后一次确认，你确定要删除服务器{config.Name}吗？",
            "这是最后一次警告！此操作不可逆！\n服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result3 == PopupResult.No) return;
        var deleteResult = AppState.InteractionUnits.SubmitServiceError(await Connector.DeleteServer(serverId));
        if (deleteResult.IsSuccess)
        {
            AppState.InteractionUnits.Notify(1, null, $"服务器{config.Name}删除成功");
        }
        else await deleteResult;
    }

    #endregion

    #region Java配置

    public FlatTreeDataGridSource<JavaInfo> JavaVersions { [ObservableAsProperty] get; }

    #endregion

    #region 服务器当前配置访问器

    [Reactive] public ServerConfig SelectedServerConfig { get; private set; }
    [Reactive] public string SelectedServerName { get; private set; }
    [Reactive] public string SelectedServerPath { get; private set; }

    private void RaiseServerConfigChanged(int serverId, FrozenDictionary<int, ServerConfig> serverConfig)
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
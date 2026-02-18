using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Threading;
using FluentResults;
using FluentResults.Extensions;
using LSL.Common.Models.AppConfig;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;
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
        SelectedServerConfig = IndexedServerConfig.None;
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
                    new TextColumn<JavaInfo, string>("架构", x => x.Architecture)
                }
            })
            .ToPropertyEx(this, x => x.JavaVersions);
        AppState.WhenAnyValue(x => x.CurrentServerConfigs, x => x.SelectedServerId)
            .Subscribe(scc => RaiseServerConfigChanged(scc.Item2, scc.Item1));
        DeleteServerCmd = ReactiveCommand.CreateFromTask(async () => await DeleteServer());
    }

    #region Java配置

    public FlatTreeDataGridSource<JavaInfo> JavaVersions { [ObservableAsProperty] get; }

    #endregion

    public async Task<bool> Init()
    {
        try
        {
            var res = await Connector.ReadDaemonConfig(true)
                .Bind(_ => Connector.ReadWebConfig(true))
                .Bind(_ => Connector.ReadDesktopConfig(true))
                .Bind(_ => Connector.ReadServerConfig(true))
                .Bind(_ => Connector.ReadJavaConfig(true));
            await AppState.Coordinator.SubmitServiceError(res);
            return res.IsSuccess;
        }
        catch (Exception e)
        {
            Logger.LogCritical(e, "A fatal error occured when initializing LSL.");
            MessageBus.Current.SendMessage(new ViewModelFatalError(e, "A fatal error occured when initializing LSL.",
                "初始化LSL时发生了致命错误。"));
            return false;
        }
    }

    #region 核心配置数据

    [Reactive] public MutableDaemonConfig DaemonConfigs { get; private set; } = new DaemonConfig().CreateDraft();
    [Reactive] public MutableWebConfig WebConfigs { get; private set; } = new WebConfig().CreateDraft();
    [Reactive] public MutableDesktopConfig DesktopConfigs { get; private set; } = new DesktopConfig().CreateDraft();

    #endregion

    #region 主配置操作

    public async Task<bool> TryCacheConfigFromFileAsync(bool rf = false)
    {
        var success = await Connector.ReadDaemonConfig(rf);
        if (success.IsSuccess)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DaemonConfigs = AppState.DaemonConfigs.CreateDraft();
                WebConfigs = AppState.WebConfigs.CreateDraft();
                DesktopConfigs = AppState.DesktopConfigs.CreateDraft();
            });
            return true;
        }

        await AppState.Coordinator.SubmitServiceError(success);
        return false;
    }

    public async Task SaveConfigAsync()
    {
        var res = await Connector.SaveDaemonConfig(DaemonConfigs.FinishDraft())
            .Bind(_ => Connector.SaveWebConfig(WebConfigs.FinishDraft()))
            .Bind(_ => Connector.SaveDesktopConfig(DesktopConfigs.FinishDraft()))
            .Bind(_ => Result.Ok());
        await AppState.Coordinator.SubmitServiceError(res, "保存配置时出现错误", true);
    }

    #endregion

    #region 服务器配置操作

    public ICommand DeleteServerCmd { get; }

    public async Task DeleteServer()
    {
        // 检查是否可以删除
        var serverId = AppState.SelectedServerId;
        if (serverId < 0)
        {
            await AppState.Coordinator.ThrowError("选定的服务器不存在", "你没有添加过服务器。该服务器是LSL提供的占位符，不支持删除。");
            return;
        }

        if (!AppState.ServerStatuses.TryGetValue(serverId, out var status) ||
            !AppState.CurrentServerConfigs.TryGetValue(serverId, out var config))
        {
            await AppState.Coordinator.ThrowError("无法删除服务器", "指定的服务器不存在。");
            return;
        }

        if (status.IsRunning)
        {
            await AppState.Coordinator.ThrowError("无法删除服务器", $"指定的服务器{config.ServerName}正在运行，请先关闭服务器再删除。");
            return;
        }

        var result1 = await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo,
            $"确认删除服务器{config.ServerName}吗？",
            "注意！此操作不可逆！\n服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result1 == PopupResult.No) return;
        var result2 = await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo,
            $"第二次确认，删除服务器{config.ServerName}吗？",
            "注意！此操作不可逆！\n服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result2 == PopupResult.No) return;
        var result3 = await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo,
            $"最后一次确认，你确定要删除服务器{config.ServerName}吗？",
            "这是最后一次警告！此操作不可逆！\n服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result3 == PopupResult.No) return;
        var deleteResult = await AppState.Coordinator.SubmitServiceError(await Connector.DeleteServer(serverId));
        if (deleteResult.IsSuccess)
            AppState.Coordinator.Notify(NotifyType.Success, null, $"服务器{config.ServerName}删除成功");
    }

    #endregion

    #region 服务器当前配置访问器

    [Reactive] public IndexedServerConfig SelectedServerConfig { get; private set; }
    [Reactive] public string SelectedServerName { get; private set; }
    [Reactive] public string SelectedServerPath { get; private set; }

    private void RaiseServerConfigChanged(int serverId, ImmutableDictionary<int, IndexedServerConfig> serverConfig)
    {
        if (serverConfig.TryGetValue(serverId, out var config))
        {
            SelectedServerConfig = config;
            SelectedServerName = config.ServerName;
            SelectedServerPath = config.ServerPath;
        }
        else
        {
            var cache = IndexedServerConfig.None;
            SelectedServerConfig = cache;
            SelectedServerName = cache.ServerName;
            SelectedServerPath = cache.ServerPath;
        }
    }

    #endregion
}
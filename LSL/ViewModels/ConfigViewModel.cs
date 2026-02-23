using System;
using System.Collections.Immutable;
using System.Linq;
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
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LSL.ViewModels;

public partial class ConfigViewModel : RegionalViewModelBase<ConfigViewModel>
{
    public ConfigViewModel(AppStateLayer appState, ServiceConnector connector, DialogCoordinator coordinator, PublicCommand commands) : base(appState, connector, coordinator, commands)
    {
        _javaVersions = null!;
        SelectedServerConfig = IndexedServerConfig.None;
        SelectedServerName = string.Empty;
        SelectedServerPath = string.Empty;
        DaemonConfigs = AppState.DaemonConfigs.CreateDraft();
        WebConfigs = AppState.WebConfigs.CreateDraft();
        DesktopConfigs = AppState.DesktopConfigs.CreateDraft();
        _javaVersionsHelper = AppState.WhenAnyValue(stateLayer => stateLayer.CurrentJavaDict)
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
            .ToProperty(this, x => x.JavaVersions);
        AppState.WhenAnyValue(x => x.CurrentServerConfigs, x => x.SelectedServerId)
            .Subscribe(scc => RaiseServerConfigChanged(scc.Item2, scc.Item1));
        DeleteServerCmd = ReactiveCommand.CreateFromTask(DeleteServer);
        SearchJava = ReactiveCommand.CreateFromTask(async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                Coordinator.Notify(NotifyType.Info, "正在搜索Java", "请耐心等待......"));
            var success = await Coordinator.SubmitServiceError(await Connector.FindJava());
            if (success.IsSuccess)
                await Dispatcher.UIThread.InvokeAsync(() =>
                    Coordinator.Notify(NotifyType.Success, "Java搜索完成！", $"搜索到了{success.Value}个Java"));
        }); // 搜索Java命令-实现
        
        CheckUpdateCmd = ReactiveCommand.CreateFromTask(Connector.CheckForUpdates);
    }

    #region Java配置

    [ObservableAsProperty] private FlatTreeDataGridSource<JavaInfo> _javaVersions;

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
            await Coordinator.SubmitServiceError(res);
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

    [Reactive] public partial MutableDaemonConfig DaemonConfigs { get; private set; }
    [Reactive] public partial MutableWebConfig WebConfigs { get; private set; }
    [Reactive] public partial MutableDesktopConfig DesktopConfigs { get; private set; }
    [Reactive] private string _universalJvmPrefix = string.Empty;

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
                UniversalJvmPrefix = string.Join('\n', DaemonConfigs.UniversalJvmPrefix);
            });
            return true;
        }

        await Coordinator.SubmitServiceError(success);
        return false;
    }

    public async Task SaveConfigAsync()
    {
        DaemonConfigs.UniversalJvmPrefix = UniversalJvmPrefix.Split(
            ["\r\n", "\r", "\n"],
            StringSplitOptions.RemoveEmptyEntries
        ).Select(line => line.Trim()).ToList();
        var res = await Connector.SaveDaemonConfig(DaemonConfigs.FinishDraft())
            .Bind(_ => Connector.SaveWebConfig(WebConfigs.FinishDraft()))
            .Bind(_ => Connector.SaveDesktopConfig(DesktopConfigs.FinishDraft()))
            .Bind(_ => Result.Ok());
        await Coordinator.SubmitServiceError(res, "保存配置时出现错误", true);
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
            await Coordinator.ThrowError("选定的服务器不存在", "你没有添加过服务器。该服务器是LSL提供的占位符，不支持删除。");
            return;
        }

        if (!AppState.ServerStatuses.TryGetValue(serverId, out var status) ||
            !AppState.CurrentServerConfigs.TryGetValue(serverId, out var config))
        {
            await Coordinator.ThrowError("无法删除服务器", "指定的服务器不存在。");
            return;
        }

        if (status.IsRunning)
        {
            await Coordinator.ThrowError("无法删除服务器", $"指定的服务器{config.ServerName}正在运行，请先关闭服务器再删除。");
            return;
        }

        var result1 = await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo,
            $"确认删除服务器{config.ServerName}吗？",
            "注意！此操作不可逆！\n服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result1 == PopupResult.No) return;
        var result2 = await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo,
            $"第二次确认，删除服务器{config.ServerName}吗？",
            "注意！此操作不可逆！\n服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result2 == PopupResult.No) return;
        var result3 = await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo,
            $"最后一次确认，你确定要删除服务器{config.ServerName}吗？",
            "这是最后一次警告！此操作不可逆！\n服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
        if (result3 == PopupResult.No) return;
        var deleteResult = await Coordinator.SubmitServiceError(await Connector.DeleteServer(serverId));
        if (deleteResult.IsSuccess)
            Coordinator.Notify(NotifyType.Success, null, $"服务器{config.ServerName}删除成功");
    }

    #endregion

    #region 服务器当前配置访问器

    [Reactive] public partial IndexedServerConfig SelectedServerConfig { get; private set; }
    [Reactive] public partial string SelectedServerName { get; private set; }
    [Reactive] public partial string SelectedServerPath { get; private set; }

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

    #region 其它命令

    public ICommand SearchJava { get; }
    public ICommand CheckUpdateCmd { get; }

    #endregion
}
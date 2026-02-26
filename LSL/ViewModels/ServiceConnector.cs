using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Threading;
using FluentResults;
using FluentResults.Extensions;
using LSL.Common.Collections;
using LSL.Common.DTOs;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Common.Models.AppConfig;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;
using LSL.Common.Utilities;
using LSL.Common.Utilities.Minecraft;
using LSL.Models;
using LSL.Models.Server;
using LSL.Services;
using LSL.Services.ConfigServices;
using LSL.Services.ServerServices;
using Microsoft.Extensions.Logging;

namespace LSL.ViewModels;

/* 用于连接视图模型与服务层
 主要成员为void，用于调用服务层方法
 编写原则：
 永不调用Notify与Popup，而是交由上游进行报告，有特殊需求的除外
 */
public class ServiceConnector
{
    private readonly AppStateLayer _appState;
    private readonly DialogCoordinator _coordinator;
    private readonly PublicCommand _commands;
    private readonly ConfigManager _configManager;
    private readonly IServerHost _daemonHost;
    private readonly ILogger<ServiceConnector> _logger;
    private readonly NetService _webHost;
    private readonly CompositeDisposable _disposables;

    public ServiceConnector(AppStateLayer appState, DialogCoordinator coordinator, PublicCommand commands, ConfigManager cfm, IServerHost daemon, NetService netService)
    {
        _appState = appState;
        _coordinator = coordinator;
        _commands = commands;
        _configManager = cfm;
        _daemonHost = daemon;
        _webHost = netService;
        _logger = _appState.LoggerFactory.CreateLogger<ServiceConnector>();
        _disposables = new CompositeDisposable(_outputCts,
            daemon.ServerMessages.Subscribe(args => _serverOutputChannel.Writer.TryWrite(args)));
        Task.Run(() => HandleOutput(_outputCts.Token));
        _logger.LogInformation("Got total RAM:{ram}", MemoryInfo.CurrentSystemMemory);
    }

    #region 启动前校验配置文件

    public Task<Result> VerifyServerConfigBeforeStart(int serverId)
    {
        if (!_configManager.TryGetServerConfig(serverId, out var config))
            return Task.FromResult(Result.Fail("LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。"));
        return config.LocatedConfig.Validate();
    }

    #endregion

    #region 网络项

    public async Task<Result> Download(string url, string dir, IProgress<double>? progress, CancellationToken? token)
    {
        return token is not null
            ? await _webHost.GetFileAsync(url, dir, progress, (CancellationToken)token)
            : await _webHost.GetFileAsync(url, dir, progress);
    }

    #endregion

    #region 配置与自启项

    public async Task CheckForUpdates()
    {
        var result = await UpdateHelper.QueryLatest(_webHost.Factory);
        if (!result.IsSuccess)
        {
            await _coordinator.ThrowError("检查更新时出错", $"检查更新时出现以下错误：\n{result.GetErrors().FlattenToString()}");
            return;
        }

        var isGreater = result.Value.IsNewerVersion(DesktopConstant.Version);
        if (!isGreater.IsSuccess)
        {
            await _coordinator.ThrowError("检查更新时出错", "无法比较当前软件版本与远程软件版本的大小差异。这是一个开发错误，请向作者反馈。");
            return;
        }

        if (isGreater.Value)
        {
            var confirm = await _coordinator.PopupInteraction.Handle(new InvokePopupArgs(
                PopupType.InfoYesNo,
                "LSL 版本更新", $"LSL 有新版本 {result.Value.TagName} 已经发布。\n{result.Value.FormatBody().Body}\n是否前往更新？"));
            if (confirm == PopupResult.Yes) await _commands.OpenWebPage(result.Value.HtmlUrl);
        }
        else
        {
            _coordinator.Notify(NotifyType.Success, "版本检查完成",
                $"当前版本已经为最新：v{DesktopConstant.Version}");
        }
    }

    #endregion

    #region 配置部分

    public async Task<Result<DaemonConfig>> ReadDaemonConfig(bool readFile = false)
    {
        return await (readFile ? await _configManager.ReadDaemonConfig() : Result.Ok(_configManager.DaemonConfigs))
            .Bind(async Task<Result<DaemonConfig>> (conf) =>
            {
                await Dispatcher.UIThread.InvokeAsync(() => _appState.DaemonConfigs = conf);
                return Result.Ok(conf);
            });
    }
    public async Task<Result<WebConfig>> ReadWebConfig(bool readFile = false)
    {
        return await (readFile ? await _configManager.ReadWebConfig() : Result.Ok(_configManager.WebConfigs))
            .Bind(async Task<Result<WebConfig>> (conf) =>
            {
                await Dispatcher.UIThread.InvokeAsync(() => _appState.WebConfigs = conf);
                return Result.Ok(conf);
            });
    }
    public async Task<Result<DesktopConfig>> ReadDesktopConfig(bool readFile = false)
    {
        return await (readFile ? await _configManager.ReadDesktopConfig() : Result.Ok(_configManager.DesktopConfigs))
            .Bind(async Task<Result<DesktopConfig>> (conf) =>
            {
                await Dispatcher.UIThread.InvokeAsync(() => _appState.DesktopConfigs = conf);
                return Result.Ok(conf);
            });
    }
    

    public async Task<Result<int>> ReadJavaConfig(bool readFile = false)
    {
        if (readFile)
        {
            var res = await _configManager.ReadJavaConfig();
            if (res.IsSuccess)
            {
                await Dispatcher.UIThread.InvokeAsync(() => _appState.CurrentJavaDict = res.Value);
                var rt = Result.Ok(res.Value.Count);
                if (res.Reasons.Count != 0)
                {
                    _logger.LogWarning(
                        "Some nonfatal error occured when reading java, please refer to the service logfile for more details.");
                    var error = new StringBuilder("在读取已保存的Java列表时出现了一些非致命错误:");
                    error.AppendLine();
                    error.AppendJoin('\n', res.Reasons.OfType<IWarning>().Select(w => w.Message));
                    error.AppendLine("这些配置没有被读取。你可以通过重新搜索Java来解决这个问题。");
                    rt.WithReason(new WarningReason(error.ToString()));
                }

                return rt;
            }
            else
            {
                return Result.Fail<int>(res.Errors);
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() => _appState.CurrentJavaDict = _configManager.JavaConfigs);
        return Result.Ok(_configManager.JavaConfigs.Count);
    }

    public async Task<Result<int>> ReadServerConfig(bool readFile = false)
    {
        if (readFile)
        {
            var res = await _configManager.ReadServerConfig();
            if (res.IsFailed) return Result.Fail<int>(res.Errors);
            var rt = Result.Ok(res.Value.Count);
            if (res.Reasons.Count != 0)
            {
                var error = new StringBuilder("在读取服务器配置时出现了一些问题:");
                error.AppendLine();
                error.AppendJoin('\n', res.Reasons.OfType<IWarning>().Select(w => w.Message));
                rt.WithReason(new WarningReason(error.ToString()));
            }

            return rt;
        }

        var cache = _configManager.CloneServerConfigs();
        var count = cache.Count;
        if (count == 0)
        {
            cache.Add(-1, IndexedServerConfig.None);
        }

        await Dispatcher.UIThread.InvokeAsync(() => _appState.CurrentServerConfigs = cache.ToImmutableDictionary());
        return Result.Ok(count);
    }

    public Task<Result<DaemonConfig>> SaveDaemonConfig(DaemonConfig? config)
    {
        return _configManager.SetDaemonConfig(config).Bind(_ => ReadDaemonConfig());
    }
    public Task<Result<WebConfig>> SaveWebConfig(WebConfig? config)
    {
        return _configManager.SetWebConfig(config).Bind(_ => ReadWebConfig());
    }
    public Task<Result<DesktopConfig>> SaveDesktopConfig(DesktopConfig? config)
    {
        return _configManager.SetDesktopConfig(config).Bind(_ => ReadDesktopConfig());
    }

    public Task<Result<int>> FindJava()
    {
        return _configManager.DetectJavaAsync().Bind(() => ReadJavaConfig(true));
    }

    #endregion

    #region 服务器命令

    public Task<Result> StartServer(int serverId)
    {
        return VerifyServerConfigBeforeStart(serverId).Bind(() =>
        {
            _appState.TerminalTexts.TryAdd(serverId, []);
            return Task.FromResult(Result.Ok());
        }).Bind(() => _daemonHost.RunServer(serverId));
    }

    public async Task StopServer(int serverId)
    {
        var confirm = await _coordinator.PopupInteraction
            .Handle(new InvokePopupArgs(PopupType.WarningYesNo, "确定要关闭该服务器吗？", "将会立刻踢出服务器内所有玩家，服务器上的最新更改会被保存。"));
        if (confirm == PopupResult.Yes)
        {
            _daemonHost.StopServer(serverId);
            _coordinator.Notify(NotifyType.Info, "正在关闭服务器", "请稍作等待");
        }
    }

    public void SaveServer(int serverId)
    {
        _daemonHost.SendCommand(serverId, "save-all");
    }

    public async Task EndServer(int serverId)
    {
        var confirm = await _coordinator.PopupInteraction.Handle(new InvokePopupArgs(
            PopupType.WarningYesNo, "确定要终止该服务端进程吗？",
            "如果强制退出，将会立刻踢出服务器内所有玩家，并且可能会导致服务端最新更改不被保存！"));
        if (confirm == PopupResult.Yes) await _daemonHost.EndServer(serverId);
    }

    public Task EndAllServers() => _daemonHost.EndAllServers();

    public void SendCommandToServer(int serverId, string command)
    {
        if (string.IsNullOrEmpty(command)) return;
        _daemonHost.SendCommand(serverId, command);
    }

    #endregion

    #region 服务器添加、修改与删除

    public async Task<Result> AddServerUsingCore(LocatedServerConfig config, string corePath)
    {
        var registerResult = await _configManager.AddServerUsingCore(config, corePath); // TODO:安插等待Forge安装方法
        return await registerResult.Bind(async Task<Result> (dict) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => _appState.CurrentServerConfigs = dict.ToImmutableDictionary());
            return Result.Ok();
        });
    }

    public async Task<Result> EditServer(int id, LocatedServerConfig config)
    {
        var result = await _configManager.EditServer(new IndexedServerConfig(id, config));
        return await result.Bind(async Task<Result> (dict) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => _appState.CurrentServerConfigs = dict.ToImmutableDictionary());
            return Result.Ok();
        });
    }

    public async Task<Result> DeleteServer(int serverId)
    {
        var result = await _configManager.DeleteServer(serverId);
        return await result.Bind(async Task<Result> (dict) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => _appState.CurrentServerConfigs = dict.ToImmutableDictionary());
            return Result.Ok();
        });
    }

    public async Task<Result> AddServerFolder(LocatedServerConfig config, IProgress<string>? progress = null)
    {
        var result = await _configManager.AddServerFolder(config, progress); // TODO:进度的后台运行与报告
        return await result.Bind(async Task<Result> (dict) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => _appState.CurrentServerConfigs = dict.ToImmutableDictionary());
            return Result.Ok();
        });
    }

    #endregion

    #region 读取服务器输出

    private readonly Channel<IServerMessage> _serverOutputChannel = Channel.CreateUnbounded<IServerMessage>();
    private readonly CancellationTokenSource _outputCts = new();

    private async Task HandleOutput(CancellationToken token)
    {
        try
        {
            await foreach (var args in _serverOutputChannel.Reader.ReadAllAsync(token))
                try
                {
                    await ArgsProcessor(args);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured while processing server output.");
                }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Server output handling queue cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured in server output handling task.");
        }
    }

    private async Task ArgsProcessor(IServerMessage args)
    {
        switch (args)
        {
            case ColorOutputArgs coa:
                await Dispatcher.UIThread.InvokeAsync(() => _appState.TerminalTexts.AddOrUpdate(coa.ServerId,
                    [new ColoredLine(coa.Output, coa.ColorHex)], (_, value) =>
                    {
                        value.Add(new ColoredLine(coa.Output, coa.ColorHex));
                        return value;
                    }));
                break;
            case GlobalMinutelyMetricsReport gmmr:
                await Dispatcher.UIThread.InvokeAsync(() => ProcessMinutelyMetrics(gmmr));
                break;
            case SecondlyMetricsReport smr:
                await Dispatcher.UIThread.InvokeAsync(() => _appState.MetricsDict.AddOrUpdate(smr.ServerId,
                    _ => new MetricsStorage(smr),
                    (_, storage) => storage.Add(smr)));
                break;
            case ServerStatusArgs ssa:
                await Dispatcher.UIThread.InvokeAsync(() => UpdateStatus(ssa));
                break;
            case PlayerUpdateArgs pua:
                await Dispatcher.UIThread.InvokeAsync(() => UpdateUser(pua));
                break;
            case PlayerMessageArgs pma:
                await Dispatcher.UIThread.InvokeAsync(() => _appState.MessageDict.AddOrUpdate(pma.ServerId,
                    [new UserMessageLine(pma.Message)], (_, value) =>
                    {
                        value.Add(new UserMessageLine(pma.Message));
                        return value;
                    }));
                break;
        }
    }

    private void UpdateUser(PlayerUpdateArgs args)
    {
        if (args.Entering)
        {
            _appState.UserDict.AddOrUpdate(args.ServerId, [new PlayerInfo(args.UUID, args.PlayerName)],
                (_, oldValue) =>
                {
                    oldValue.Add(new PlayerInfo(args.UUID, args.PlayerName));
                    return oldValue;
                });
        }
        else
        {
            if (_appState.UserDict.TryGetValue(args.ServerId, out var uc))
                for (var i = uc.Count - 1; i >= 0; i--)
                    if (uc[i].PlayerName == args.PlayerName) //TODO:将uc转换为字典，提高搜索效率
                    {
                        uc.RemoveAt(i);
                        break;
                    }
        }
    }

    private void UpdateStatus(ServerStatusArgs args)
    {
        _appState.ServerStatuses.AddOrUpdate(args.ServerId,
            new ServerStatus(args.IsRunning, args.IsOnline),
            (_, value) => value.Update(args.IsRunning, args.IsOnline));
        UpdateRunningServer();
    }

    private void UpdateRunningServer()
    {
        _appState.RunningServerCount = 0;
        foreach (var item in _appState.ServerStatuses)
            if (item.Value.IsRunning)
                _appState.RunningServerCount++;
    }

    #endregion
    
    #region 接收服务器资源占用信息

    private void ProcessMinutelyMetrics(GlobalMinutelyMetricsReport args)
    {
        _appState.GeneralCpuMetrics.Add(args.CpuUsage);
        _appState.GeneralRamMetrics.Add(args.MemUsage);
        _appState.GeneralRamCount.Add(args.MemBytes);
    }

    #endregion
}
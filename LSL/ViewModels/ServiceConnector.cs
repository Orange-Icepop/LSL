﻿using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Threading;
using LSL.Common.Collections;
using LSL.Common.DTOs;
using LSL.Common.Models;
using LSL.Common.Utilities;
using LSL.Common.Validation;
using LSL.Services;
using LSL.Services.ConfigServices;
using LSL.Services.ServerServices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using MemoryInfo = LSL.Common.Utilities.MemoryInfo;

namespace LSL.ViewModels
{
    /* 用于连接视图模型与服务层
     主要成员为void，用于调用服务层方法
     编写原则：
     永不调用Notify（因为没有可执行保证）
     */
    public class ServiceConnector
    {
        private readonly AppStateLayer _appState;
        private readonly ConfigManager _configManager;
        private readonly ServerHost _daemonHost;
        private readonly ServerOutputStorage _outputStorage;
        private readonly NetService _webHost;
        private readonly ILogger<ServiceConnector> _logger;
        public ServiceConnector(AppStateLayer appState, ConfigManager cfm, ServerHost daemon, ServerOutputStorage optStorage, NetService netService)
        {
            _appState = appState;
            _configManager = cfm;
            _daemonHost = daemon;
            _outputStorage = optStorage;
            _webHost = netService;
            _logger = _appState.LoggerFactory.CreateLogger<ServiceConnector>();
            EventBus.Instance.Subscribe<IStorageArgs>(args => _serverOutputChannel.Writer.TryWrite(args));
            EventBus.Instance.Subscribe<IMetricsArgs>(ReceiveMetrics);
            _handleOutputTask = Task.Run(() => HandleOutput(_outputCts.Token));
            _initializationTask = CopyServerOutput();
            _logger.LogInformation("Got total RAM:{ram}", MemoryInfo.CurrentSystemMemory);
        }

        #region 配置部分

        public async Task ReadMainConfig(bool readFile = false)
        {
            if (readFile)
            {
                var res = _configManager.ReadMainConfig();
                var notCritical = await _appState.InteractionUnits.SubmitServiceError(res);
                if (!notCritical)
                {
                    //var err = res.Error?.ToString() ?? string.Empty;
                    Environment.Exit(1);
                }
            }
            _appState.CurrentConfigs = _configManager.MainConfigs;
        }

        public async Task ReadJavaConfig(bool readFile = false)
        {
            if (readFile)
            {
                var res = _configManager.ReadJavaConfig();
                if (res.ResultType is ServiceResultType.Error)
                {
                    // var err = res.Error?.ToString() ?? string.Empty;
                    Environment.Exit(1);
                    //TODO:别这么搞，把退出任务交给上游
                }
                if (res.Result is not null)
                {
                    if (res.Result.NotFound.Any() || res.Result.NotJava.Any())
                    {
                        var error = new StringBuilder("在读取已保存的Java列表时出现了一些非致命错误:");
                        error.AppendLine();
                        if (res.Result.NotFound.Any())
                        {
                            error.AppendLine("以下Java不存在或无法被访问:");
                            error.AppendJoin(Environment.NewLine, res.Result.NotFound);
                        }

                        if (res.Result.NotJava.Any())
                        {
                            error.AppendLine("以下文件不是Java:");
                            error.AppendJoin(Environment.NewLine, res.Result.NotJava);
                        }
                        error.AppendLine();
                        error.AppendLine("这些配置没有被读取。你可以通过重新搜索Java来解决这个问题。");
                        _logger.LogWarning("Some nonfatal error occured when reading java, please refer to the service logfile for more details.");
                        await _appState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningConfirm, "读取Java配置时出错", error.ToString()));
                    }
                }

            }
            await Dispatcher.UIThread.InvokeAsync(() => _appState.CurrentJavaDict = _configManager.JavaConfigs);
        }

        public async Task ReadServerConfig(bool readFile = false)
        {
            if (readFile)
            {
                var res = _configManager.ReadServerConfig();
                if (res.ResultType is ServiceResultType.Error)
                {
                    // 如果是致命错误，直接退出
                    if (res.Error is not null) await _appState.InteractionUnits.SubmitServiceError(res);
                    _logger.LogCritical(res.Error, "A critical error occured while reading server config. Exiting......");
                    Environment.Exit(1);
                    return;
                }
                else if (res.ResultType is ServiceResultType.FinishWithWarning)
                {
                    var error = new StringBuilder("在读取服务器配置时出现了一些非致命错误:");
                    if (res.NotFoundServers.Count > 0)
                    {
                        error.AppendLine()
                            .AppendLine($"有{res.NotFoundServers.Count}个已注册的服务器不存在：")
                            .AppendJoin(Environment.NewLine, res.NotFoundServers);
                    }

                    if (res.ConfigErrorServers.Count > 0)
                    {
                        error.AppendLine()
                            .AppendLine($"有{res.ConfigErrorServers.Count}个服务器的配置文件不存在或格式不正确：")
                            .AppendJoin(Environment.NewLine, res.ConfigErrorServers);
                    }
                    error.AppendLine("这些服务器将不会被读取。你可以通过重新添加服务器来解决这个问题。");
                    await _appState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningConfirm, "读取服务器配置时发生错误", error.ToString()));
                }
            }

            var cache = _configManager.ServerConfigs.Clone2Dict();
            if (cache.Count == 0)
            {
                cache.Add(-1, ServerConfig.None);
            }

            await Dispatcher.UIThread.InvokeAsync(() => _appState.CurrentServerConfigs = cache.ToFrozenDictionary());
        }

        public async Task<bool> UpdateConfig(IDictionary<int, object> currentConfigs)
        {
            var saveSuccess = await SaveConfig();
            if (!saveSuccess) return false;
            await ReadMainConfig(true);
            return true;
        }
        public async Task<bool> SaveConfig()
        {
            var result = _configManager.ConfirmMainConfig(_appState.CurrentConfigs);
            await _appState.InteractionUnits.SubmitServiceError(result);
            return result.IsFullSuccess;
        }

        public async Task<bool> FindJava()
        {
            var result = await _configManager.DetectJava();
            await _appState.InteractionUnits.SubmitServiceError(result);
            bool success = result.IsFullSuccess;
            await ReadJavaConfig(true);
            return success;
        }

        #endregion

        #region 服务器命令

        public bool StartServer(int serverId)
        {
            var result = VerifyServerConfigBeforeStart(serverId);
            if (result != null)
            {
                _appState.InteractionUnits.ThrowError("服务器配置校验发生错误", result);
                return false;
            }

            _appState.TerminalTexts.TryAdd(serverId, []);
            _daemonHost.RunServer(serverId);
            return true;
        }

        public async Task StopServer(int serverId)
        {
            var confirm = await _appState.InteractionUnits.PopupInteraction
                .Handle(new InvokePopupArgs(PopupType.WarningYesNo, "确定要关闭该服务器吗？", "将会立刻踢出服务器内所有玩家，服务器上的最新更改会被保存。"));
            if (confirm == PopupResult.Yes)
            {
                _daemonHost.StopServer(serverId);
                _appState.InteractionUnits.Notify(0, "正在关闭服务器", "请稍作等待");
            }
        }

        public void SaveServer(int serverId)
        {
            _daemonHost.SendCommand(serverId, "save-all");
        }

        public async Task EndServer(int serverId)
        {
            var confirm = await _appState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningYesNo, "确定要终止该服务端进程吗？",
                "如果强制退出，将会立刻踢出服务器内所有玩家，并且可能会导致服务端最新更改不被保存！"));
            if (confirm == PopupResult.Yes) _daemonHost.EndServer(serverId);
        }

        public void EndAllServers() => _daemonHost.EndAllServers();

        public void SendCommandToServer(int serverId, string command)
        {
            if (string.IsNullOrEmpty(command)) return;
            _daemonHost.SendCommand(serverId, command);
        }

        #endregion

        #region 启动前校验配置文件

        public string? VerifyServerConfigBeforeStart(int serverId)
        {
            if (!_configManager.ServerConfigs.TryGetValue(serverId, out var config))
                return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
            else if (!File.Exists(config.UsingJava)) return "LSL无法启动选定的服务器，因为配置文件中指定的Java路径不存在。";
            else
            {
                string configPath = Path.Combine(config.ServerPath, config.CoreName);
                if (!File.Exists(configPath)) return "LSL无法启动选定的服务器，因为配置文件中指定的核心文件不存在。";
            }

            return null;
        }

        #endregion

        #region 读取服务器输出

        private readonly Channel<IStorageArgs> _serverOutputChannel = Channel.CreateUnbounded<IStorageArgs>();
        private CancellationTokenSource _outputCts = new();
        private Task _handleOutputTask;

        private async Task HandleOutput(CancellationToken token)
        {
            try
            {
                await foreach (var args in _serverOutputChannel.Reader.ReadAllAsync(token))
                {
                    try
                    {
                        await ArgsProcessor(args);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occured while processing server output.");
                    }
                }
            }
            catch(OperationCanceledException)
            {
                _logger.LogInformation("Server output handling queue cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured in server output handling task.");
            }
        }

        private async Task ArgsProcessor(IStorageArgs args)
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
                {
                    for (int i = uc.Count - 1; i >= 0; i--)
                    {
                        if (uc[i].PlayerName == args.PlayerName)
                        {
                            uc.RemoveAt(i);
                            break;
                        }
                    }
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
            {
                if (item.Value.IsRunning) _appState.RunningServerCount++;
            }
        }
        #endregion
        
        #region 接收服务器资源占用信息
        private void ReceiveMetrics(IMetricsArgs args)
        {
            switch (args)
            {
                case MetricsUpdateArgs mua: ProcessSecondlyMetrics(mua); break;
                case GeneralMetricsArgs gma: ProcessMinutelyMetrics(gma); break;
            }
        }

        private void ProcessSecondlyMetrics(MetricsUpdateArgs args)
        {
            foreach (var item in args.Metrics)
            {
                _appState.MetricsDict.AddOrUpdate(item.ServerId, _ => new MetricsStorage(item),
                    (_, storage) => storage.Add(item));
            }
        }

        private void ProcessMinutelyMetrics(GeneralMetricsArgs args)
        {
            RangedObservableLinkedList<double> cpu = new(30);
            RangedObservableLinkedList<double> ram = new(30);
            foreach (var c in args.CpuHistory)
            {
                cpu.Add(c);
            }

            foreach (var r in args.RamPctHistory)
            {
                ram.Add(r);
            }

            _appState.GeneralCpuMetrics = cpu;
            _appState.GeneralRamMetrics = ram;
            _appState.OnGeneralMetricsUpdated(args.CpuHistory.LastItem, args.RamPctHistory.LastItem, args.RamBytesAvgHistory.LastItem);
        }
        #endregion

        #region 服务器添加、修改与删除

        public static (int, string?) ValidateNewServerConfig(FormedServerConfig config, bool skipCorePathCheck = false)
        {
            var checkResult = CheckService.VerifyFormedServerConfig(config, skipCorePathCheck);
            string errorInfo = "";
            foreach (var item in checkResult)
            {
                if (!item.Passed)
                {
                    errorInfo += $"{item.Reason}\r";
                }
            }

            if (!string.IsNullOrEmpty(errorInfo))
            {
                return (0, errorInfo);
            }
            if (skipCorePathCheck) return (1, null);// 不检查核心，直接返回
            var coreResult = CoreValidationService.Validate(config.CorePath, out var problem);
            return coreResult switch
            {
                CoreValidationService.CoreType.Error => (0, "验证核心文件时发生错误。\r" + problem),
                CoreValidationService.CoreType.ForgeInstaller => (0, "您选择的文件是一个Forge安装器，而不是一个Minecraft服务端核心文件。LSL暂不支持Forge服务器的添加与启动。"),
                CoreValidationService.CoreType.FabricInstaller => (0, "您选择的文件是一个Fabric安装器，而不是一个Minecraft服务端核心文件。请下载Fabric官方服务器jar文件，而不是安装器。"),
                CoreValidationService.CoreType.Unknown => (-1,
                    "LSL无法确认您选择的文件是否为Minecraft服务端核心文件。\r这可能是由于LSL没有收集足够的关于服务器核心的辨识信息造成的。如果这是确实一个Minecraft服务端核心并且具有一定的知名度，请您前往LSL的仓库（https://github.com/Orange-Icepop/LSL）提交相关Issue。\r您可以直接点击确认绕过校验，但是LSL及其开发团队不为因此造成的后果作担保。"),
                CoreValidationService.CoreType.Client => (0, "您选择的文件是一个Minecraft客户端核心文件，而不是一个服务端核心文件。"),
                _ => (1, null)
            };
        }

        public static string GetCoreType(string? corePath)
        {
            return CoreValidationService.Validate(corePath, out _).ToString();
        }

        public async Task<bool> AddServer(FormedServerConfig config)
        { 
            var result = _configManager.RegisterServer(config);
            await _appState.InteractionUnits.SubmitServiceError(result);
            await ReadServerConfig(true);
            return result.IsFullSuccess;
        }

        public async Task<bool> EditServer(int id, FormedServerConfig config)
        {
            var result = _configManager.EditServer(id, config);
            await _appState.InteractionUnits.SubmitServiceError(result);
            await ReadServerConfig(true);
            return result.IsFullSuccess;
        }

        public async Task<bool> DeleteServer(int serverId)
        {
            var result = _configManager.DeleteServer(serverId); 
            await _appState.InteractionUnits.SubmitServiceError(result);
            await ReadServerConfig(true);
            return result.IsFullSuccess;
        }

        public async Task<bool> AddExistedServer(FormedServerConfig config)
        {
            var result = await _configManager.AddExistedServer(config);
            await _appState.InteractionUnits.SubmitServiceError(result);
            await ReadServerConfig(true);
            return result.IsFullSuccess;
        }

        #endregion

        #region 从服务线程拷贝服务器输出字典
        private readonly Task _initializationTask;
        private readonly SemaphoreSlim _copyLock = new(1, 1);
        private async Task CopyServerOutput()
        {
            // 保险起见用锁Throttle一下，别瞎玩搞竞态了
            await _copyLock.WaitAsync();
            try
            {
                // 暂停输出处理
                _logger.LogInformation("Copy server output. Stopping output handler...");
                await _outputCts.CancelAsync();
                await _handleOutputTask;
                // 拷贝输出字典
                _appState.TerminalTexts = new ConcurrentDictionary<int, ObservableCollection<ColoredLine>>(
                    _outputStorage.OutputDict.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new ObservableCollection<ColoredLine>(
                            kvp.Value.Select(line => new ColoredLine(line.Line, line.ColorHex))
                        )
                    )
                );

                _appState.ServerStatuses = new ConcurrentDictionary<int, ServerStatus>(
                    _outputStorage.StatusDict.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new ServerStatus(kvp.Value)
                    )
                );
                await Dispatcher.UIThread.InvokeAsync(UpdateRunningServer);

                _appState.UserDict = new ConcurrentDictionary<int, ObservableCollection<PlayerInfo>>(
                    _outputStorage.PlayerDict
                        .GroupBy(kvp => kvp.Key.ServerId)
                        .ToDictionary(
                            g => g.Key,
                            g => new ObservableCollection<PlayerInfo>(
                                g.Select(kvp => new PlayerInfo(kvp.Value, kvp.Key.PlayerName))
                            )
                        )
                );

                _appState.MessageDict = new ConcurrentDictionary<int, ObservableCollection<UserMessageLine>>(
                    _outputStorage.MessageDict.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new ObservableCollection<UserMessageLine>(
                            kvp.Value.Select(line => new UserMessageLine(line))
                        )
                    )
                );
            }
            finally
            {
                // 恢复输出处理
                _outputCts = new CancellationTokenSource();
                _handleOutputTask = Task.Run(() => HandleOutput(_outputCts.Token));
                _logger.LogInformation("Output handler restarted.");
                // 释放锁
                _copyLock.Release();
            }
        }
        #endregion
        
        #region 网络项

        public async Task<bool> Download(string url, string dir, IProgress<double>? progress, CancellationToken? token)
        {
            var result = token is not null ? await _webHost.GetFileAsync(url, dir, progress, (CancellationToken)token) : await _webHost.GetFileAsync(url, dir, progress);
            var success = !await _appState.InteractionUnits.SubmitServiceError(result);
            return success;
        }
        #endregion
        
        #region 配置与自启项
        public async Task CheckForUpdates()
        {
            try
            {
                if (!_appState.CurrentConfigs.TryGetValue("beta_update", out var betaUpdateObj))
                {
                    throw new KeyNotFoundException("Config key beta_update not found.");
                }

                if (!bool.TryParse(betaUpdateObj.ToString(), out var betaUpdate))
                {
                    throw new FormatException("Config beta_update are not valid boolean.");
                }

                Dispatcher.UIThread.Post(() => _appState.InteractionUnits.Notify(0, "更新检查", "开始检查LSL更新......"));
                string url = betaUpdate ? "https://api.orllow.cn/lsl/latest/prerelease" : "https://api.orllow.cn/lsl/latest/stable";
                var result = await _webHost.ApiGet(url);
                switch (result.StatusCode)
                {
                    case 0:
                        throw new HttpRequestException("Cannot connect to the server.");
                    case < 200 or >= 300:
                        throw new HttpRequestException($"Error getting update API(code {result.StatusCode}){Environment.NewLine}{result.Message}");
                }
                var jobj = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Message) ??
                           throw new FormatException("Update API Response can't be serialized as dictionary.");
                var remoteVerString = jobj["tag_name"].ToString() ??
                                      throw new NullReferenceException(
                                          "API Response doesn't contain required key tag_name.");
                var remoteVer = remoteVerString.TrimStart('v');
                var needUpdate = AlgoServices.IsGreaterVersion(DesktopConstant.Version, remoteVer);
                _logger.LogInformation("Got remote version update. Local:{LC}, remote:{RM}.", DesktopConstant.Version, remoteVer);
                if (needUpdate)
                {
                    var updateMessage = jobj["body"].ToString() ??
                                        throw new NullReferenceException(
                                            "API Response doesn't contain required key body.");
                    var message =
                        $"LSL已经推出了新版本：{remoteVerString}。可前往https://github.com/Orange-Icepop/LSL/releases下载。{Environment.NewLine}{updateMessage}";
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _appState.InteractionUnits.PopupInteraction
                            .Handle(new InvokePopupArgs(PopupType.InfoConfirm, "LSL更新提示", message))
                            .Subscribe();
                    });
                }
                else
                    Dispatcher.UIThread.Post(() =>
                        _appState.InteractionUnits.Notify(1, "更新检查完毕", $"当前LSL版本已为最新：{DesktopConstant.Version}"));
                _logger.LogInformation("Check for updates completed.");
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _appState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(PopupType.ErrorConfirm, "更新检查出错",
                            "LSL在检查更新时出现了问题。" + Environment.NewLine + ex.Message))
                        .Subscribe();
                });
                _logger.LogInformation("Error checking for updates:{ex}", ex.Message);
            }
        }
        #endregion
    }

    #region 服务器状态类

    public class ServerStatus : ReactiveObject
    {
        public ServerStatus()
        {
            IsRunning = false;
            IsOnline = false;
        }

        public ServerStatus((bool, bool) param)
        {
            IsRunning = param.Item1;
            IsOnline = param.Item2;
        }

        public ServerStatus(bool isRunning, bool isOnline)
        {
            IsRunning = isRunning;
            IsOnline = isOnline;
        }

        public ServerStatus Update(bool isRunning, bool isOnline)
        {
            IsRunning = isRunning;
            IsOnline = isOnline;
            return this;
        }

        public ServerStatus Update((bool, bool) param)
        {
            IsRunning = param.Item1;
            IsOnline = param.Item2;
            return this;
        }

        [Reactive] public bool IsRunning { get; private set; }
        [Reactive] public bool IsOnline { get; private set; }
    }

    #endregion

    #region 用户记录类

    public class PlayerInfo
    {
        public PlayerInfo(string uuid, string playerName)
        {
            this.UUID = uuid;
            this.PlayerName = playerName;
        }

        public string UUID { get; set; }
        public string PlayerName { get; set; }
    }

    #endregion
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Threading;
using LSL.Common.Contracts;
using LSL.Common.Helpers;
using LSL.Common.Helpers.Validators;
using LSL.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels
{
    /* 用于连接视图模型与服务层
     主要成员为void，用于调用服务层方法
     编写原则：
     永不调用Notify（因为没有可执行保证）
     */
    public class ServiceConnector
    {
        private AppStateLayer AppState { get; }
        private ConfigManager configManager { get; }
        private ServerHost daemonHost { get; }
        private ServerOutputStorage outputStorage { get; }
        private NetService WebHost { get; }
        private ILogger<ServiceConnector> _logger { get; }
        public ServiceConnector(AppStateLayer appState, ConfigManager cfm, ServerHost daemon, ServerOutputStorage optStorage, NetService netService)
        {
            AppState = appState;
            configManager = cfm;
            daemonHost = daemon;
            outputStorage = optStorage;
            WebHost = netService;
            _logger = AppState.LoggerFactory.CreateLogger<ServiceConnector>();
            EventBus.Instance.Subscribe<IStorageArgs>(args => ServerOutputChannel.Writer.TryWrite(args));
            _handleOutputTask = Task.Run(() => HandleOutput(OutputCts.Token));
            CopyServerOutput();
            _logger.LogInformation("Total RAM:{ram}", MemoryInfo.GetTotalSystemMemory());
        }

        #region 配置部分

        public async Task GetConfig(bool readFile = false)
        {
            _logger.LogInformation("start loading main config");
            if (readFile)
            {
                var res = configManager.ReadMainConfig();
                var notCritical = await AppState.ITAUnits.SubmitServiceError(res);
                if (!notCritical)
                {
                    var err = res.Error?.ToString() ?? string.Empty;
                    _logger.LogCritical("Fatal error when loading LSL main config.{nl}{err} ",Environment.NewLine, err);
                    Environment.Exit(1);
                }
            }
            AppState.CurrentConfigs = configManager.MainConfigs;
            _logger.LogInformation("loading main config completed");
        }

        public async Task<ServiceResult> ReadJavaConfig(bool readFile = false)
        {
            _logger.LogInformation("start loading java config");
            if (readFile)
            {
                var res = configManager.ReadJavaConfig();
                var notCritical = await AppState.ITAUnits.SubmitServiceError(res);
                if (!notCritical)
                {
                    var err = res.Error?.ToString() ?? string.Empty;
                    _logger.LogCritical("Fatal error when loading java config.{nl}{err} ",Environment.NewLine, err);
                    Environment.Exit(1);
                    return res;
                }
            }
            AppState.CurrentJavaDict = configManager.JavaConfigs;
            _logger.LogInformation("loading java config completed");
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ReadServerConfig(bool readFile = false)
        {
            _logger.LogInformation("start loading server config");
            if (readFile)
            {
                var res = configManager.ReadServerConfig();
                var notCritical = await AppState.ITAUnits.SubmitServiceError(res);
                if (!notCritical)
                {
                    var err = res.Error?.ToString() ?? string.Empty;
                    _logger.LogCritical("Fatal error when loading LSL server config.{nl}{err} ",Environment.NewLine, err);
                    Environment.Exit(1);
                    return res;
                }
            }

            var cache = configManager.ServerConfigs.ToDictionary(
                item => item.Key,
                item => new ServerConfig(item.Value));
            if (cache.Count == 0)
            {
                cache.Add(-1, ServerConfig.None);
            }
            AppState.CurrentServerConfigs = cache;
            _logger.LogInformation("loading server config completed");
            return ServiceResult.Success();
        }
        public async Task<bool> SaveConfig()
        {
            var result = configManager.ConfirmMainConfig(AppState.CurrentConfigs);
            await AppState.ITAUnits.SubmitServiceError(result);
            bool success = result.IsFullSuccess;
            if (success) _logger.LogInformation("Main config saved");
            return success;
        }

        public async Task<bool> FindJava()
        {
            _logger.LogInformation("start finding java");
            var result = await configManager.DetectJava();
            await AppState.ITAUnits.SubmitServiceError(result);
            bool success = result.IsFullSuccess;
            if (success) _logger.LogInformation("java detection completed");
            await Dispatcher.UIThread.InvokeAsync(() => ReadJavaConfig());
            return success;
        }

        #endregion

        #region 服务器命令

        public void StartServer(int serverId)
        {
            var result = VerifyServerConfigBeforeStart(serverId);
            if (result != null)
            {
                AppState.ITAUnits.ThrowError("服务器配置校验发生错误", result);
                return;
            }

            AppState.TerminalTexts.TryAdd(serverId, new ObservableCollection<ColoredLines>());
            daemonHost.RunServer(serverId);
        }

        public async Task StopServer(int serverId)
        {
            var confirm = await AppState.ITAUnits.PopupITA
                .Handle(new InvokePopupArgs(PopupType.Warning_YesNo, "确定要关闭该服务器吗？", "将会立刻踢出服务器内所有玩家，服务器上的最新更改会被保存。"));
            if (confirm == PopupResult.Yes)
            {
                daemonHost.StopServer(serverId);
                AppState.ITAUnits.Notify(0, "正在关闭服务器", "请稍作等待");
            }
        }

        public void SaveServer(int serverId)
        {
            daemonHost.SendCommand(serverId, "save-all");
        }

        public async Task EndServer(int serverId)
        {
            var confirm = await AppState.ITAUnits.PopupITA.Handle(new(PopupType.Warning_YesNo, "确定要终止该服务端进程吗？",
                "如果强制退出，将会立刻踢出服务器内所有玩家，并且可能会导致服务端最新更改不被保存！"));
            if (confirm == PopupResult.Yes) daemonHost.EndServer(serverId);
        }

        public void EndAllServers() => daemonHost.EndAllServers();

        public void SendCommandToServer(int serverId, string command)
        {
            if (string.IsNullOrEmpty(command)) return;
            daemonHost.SendCommand(serverId, command);
        }

        #endregion

        #region 启动前校验配置文件

        public string? VerifyServerConfigBeforeStart(int serverId)
        {
            if (!configManager.ServerConfigs.TryGetValue(serverId, out var config))
                return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
            else if (config is null) return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
            else if (!File.Exists(config.using_java)) return "LSL无法启动选定的服务器，因为配置文件中指定的Java路径不存在。";
            else
            {
                string configPath = Path.Combine(config.server_path, config.core_name);
                if (!File.Exists(configPath)) return "LSL无法启动选定的服务器，因为配置文件中指定的核心文件不存在。";
            }

            return null;
        }

        #endregion

        #region 读取服务器输出

        private readonly Channel<IStorageArgs> ServerOutputChannel = Channel.CreateUnbounded<IStorageArgs>();
        private CancellationTokenSource OutputCts = new();
        private Task _handleOutputTask;

        private async Task HandleOutput(CancellationToken token)
        {
            await foreach (var args in ServerOutputChannel.Reader.ReadAllAsync(token))
            {
                try
                {
                    await ArgsProcessor(args);
                }
                catch
                {
                }
            }
        }

        private async Task ArgsProcessor(IStorageArgs args)
        {
            switch (args)
            {
                case ColorOutputArgs COA:
                    await Dispatcher.UIThread.InvokeAsync(() => AppState.TerminalTexts.AddOrUpdate(COA.ServerId,
                        [new ColoredLines(COA.Output, COA.ColorHex)], (key, value) =>
                        {
                            value.Add(new ColoredLines(COA.Output, COA.ColorHex));
                            return value;
                        }));
                    break;
                case ServerStatusArgs SSA:
                    await Dispatcher.UIThread.InvokeAsync(() => UpdateStatus(SSA));
                    break;
                case PlayerUpdateArgs PUA:
                    await Dispatcher.UIThread.InvokeAsync(() => UpdateUser(PUA));
                    break;
                case PlayerMessageArgs PMA:
                    await Dispatcher.UIThread.InvokeAsync(() => AppState.MessageDict.AddOrUpdate(PMA.ServerId,
                        [new UserMessageLine(PMA.Message)], (key, value) =>
                        {
                            value.Add(new UserMessageLine(PMA.Message));
                            return value;
                        }));
                    break;
            }
        }

        private void UpdateUser(PlayerUpdateArgs args)
        {
            if (args.Entering)
            {
                AppState.UserDict.AddOrUpdate(args.ServerId, [new UUID_User(args.UUID, args.PlayerName)],
                    (key, oldValue) =>
                    {
                        oldValue.Add(new UUID_User(args.UUID, args.PlayerName));
                        return oldValue;
                    });
            }
            else
            {
                if (AppState.UserDict.TryGetValue(args.ServerId, out var uc))
                {
                    for (int i = uc.Count - 1; i >= 0; i--)
                    {
                        if (uc[i].User == args.PlayerName)
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
            AppState.ServerStatuses.AddOrUpdate(args.ServerId,
                new ServerStatus(args.IsRunning, args.IsOnline),
                (key, value) => value.Update(args.IsRunning, args.IsOnline));
            UpdateRunningServer();
        }

        private void UpdateRunningServer()
        {
            AppState.RunningServerCount = 0;
            foreach (var item in AppState.ServerStatuses)
            {
                if (item.Value.IsRunning) AppState.RunningServerCount++;
            }
        }
        #endregion

        #region 服务器添加、修改与删除

        public (int, string?) ValidateNewServerConfig(FormedServerConfig config, bool skipCP = false)
        {
            var checkResult = CheckService.VerifyFormedServerConfig(config, skipCP);
            string ErrorInfo = "";
            foreach (var item in checkResult)
            {
                if (item.Passed == false)
                {
                    ErrorInfo += $"{item.Reason}\r";
                }
            }

            if (!string.IsNullOrEmpty(ErrorInfo))
            {
                return (0, ErrorInfo);
            }
            if (skipCP) return (1, null);// 不检查核心，直接返回
            var coreResult = CoreValidationService.Validate(config.CorePath, out var Problem);
            return coreResult switch
            {
                CoreValidationService.CoreType.Error => (0, "验证核心文件时发生错误。\r" + Problem),
                CoreValidationService.CoreType.ForgeInstaller => (0, "您选择的文件是一个Forge安装器，而不是一个Minecraft服务端核心文件。"),
                CoreValidationService.CoreType.FabricInstaller => (0, "您选择的文件是一个Fabric安装器，而不是一个Minecraft服务端核心文件。"),
                CoreValidationService.CoreType.Unknown => (-1,
                    "LSL无法确认您选择的文件是否为Minecraft服务端核心文件。\r这可能是由于LSL没有收集足够的关于服务器核心的辨识信息造成的。如果这是确实一个Minecraft服务端核心并且具有一定的知名度，请您前往LSL的仓库（https://github.com/Orange-Icepop/LSL）提交相关Issue。\r您可以直接点击确认绕过校验，但是LSL及其开发团队不为因此造成的后果负任何义务或责任。"),
                CoreValidationService.CoreType.Client => (0, "您选择的文件是一个Minecraft客户端核心文件，而不是一个服务端核心文件。"),
                _ => (1, null)
            };
        }

        public string GetCoreType(string? corePath)
        {
            return CoreValidationService.Validate(corePath, out var Problem).ToString();
        }

        public async Task<bool> AddServer(FormedServerConfig config)
        { 
            var result = configManager.RegisterServer(config);
            await AppState.ITAUnits.SubmitServiceError(result);
            await ReadServerConfig(true);
            return result.IsFullSuccess;
        }

        public async Task<bool> EditServer(int id, FormedServerConfig config)
        {
            var result = configManager.EditServer(id, config);
            await AppState.ITAUnits.SubmitServiceError(result);
            await ReadServerConfig(true);
            return result.IsFullSuccess;
        }

        public async Task<bool> DeleteServer(int serverId)
        {
            var result = configManager.DeleteServer(serverId); 
            await AppState.ITAUnits.SubmitServiceError(result);
            await ReadServerConfig(true);
            return result.IsFullSuccess;
        }

        #endregion

        #region 从服务线程拷贝服务器输出字典
        private async Task CopyServerOutput()
        {
            // 暂停输出处理
            _logger.LogInformation("Copy server output. Stopping output handler...");
            OutputCts.Cancel();
            try
            {
                await _handleOutputTask;
            }
            catch (OperationCanceledException) { }
            _logger.LogInformation("Output handler cancelled.");
            try
            {
                AppState.TerminalTexts = new ConcurrentDictionary<int, ObservableCollection<ColoredLines>>(
                    outputStorage.OutputDict.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new ObservableCollection<ColoredLines>(
                            kvp.Value.Select(line => new ColoredLines(line.Line, line.ColorHex))
                        )
                    )
                );

                AppState.ServerStatuses = new ConcurrentDictionary<int, ServerStatus>(
                    outputStorage.StatusDict.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new ServerStatus(kvp.Value)
                    )
                );
                await Dispatcher.UIThread.InvokeAsync(UpdateRunningServer);

                AppState.UserDict = new ConcurrentDictionary<int, ObservableCollection<UUID_User>>(
                    outputStorage.PlayerDict
                        .GroupBy(kvp => kvp.Key.ServerId)
                        .ToDictionary(
                            g => g.Key,
                            g => new ObservableCollection<UUID_User>(
                                g.Select(kvp => new UUID_User(kvp.Value, kvp.Key.PlayerName))
                            )
                        )
                );

                AppState.MessageDict = new ConcurrentDictionary<int, ObservableCollection<UserMessageLine>>(
                    outputStorage.MessageDict.ToDictionary(
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
                OutputCts = new CancellationTokenSource();
                _handleOutputTask = Task.Run(() => HandleOutput(OutputCts.Token));
                _logger.LogInformation("Output handler restarted.");
            }
        }
        #endregion
        
        #region 网络项

        public async Task<bool> Download(string url, string dir, IProgress<double>? progress, CancellationToken? token)
        {
            var result = token is not null ? await WebHost.GetFileAsync(url, dir, progress, (CancellationToken)token) : await WebHost.GetFileAsync(url, dir, progress);
            var success = !await AppState.ITAUnits.SubmitServiceError(result);
            return success;
        }
        #endregion
        
        #region 配置与自启项
        public async Task CheckForUpdates()
        {
            try
            {
                if (!AppState.CurrentConfigs.TryGetValue("beta_update", out var betaUpdateObj))
                {
                    throw new KeyNotFoundException("Config key beta_update not found.");
                }

                if (!bool.TryParse(betaUpdateObj.ToString(), out var betaUpdate))
                {
                    throw new FormatException("Config beta_update are not valid boolean.");
                }

                Dispatcher.UIThread.Post(() => AppState.ITAUnits.Notify(0, "更新检查", "开始检查LSL更新......"));
                string url = betaUpdate ? "https://api.orllow.cn/lsl/latest/prerelease" : "https://api.orllow.cn/lsl/latest/stable";
                var result = await WebHost.ApiGet(url);
                var jobj = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Result) ??
                           throw new FormatException("Update API Response can't be serialized as dictionary.");
                var remoteVerString = jobj["tag_name"].ToString() ??
                                      throw new NullReferenceException(
                                          "API Response doesn't contain required key tag_name.");
                var remoteVer = remoteVerString.TrimStart('v');
                var needUpdate = AlgoServices.IsGreaterVersion(Constant.Version, remoteVer);
                _logger.LogInformation("Got remote version update. Local:{LC}, remote:{RM}.", Constant.Version, remoteVer);
                if (needUpdate)
                {
                    var updateMessage = jobj["body"].ToString() ??
                                        throw new NullReferenceException(
                                            "API Response doesn't contain required key body.");
                    var message =
                        $"LSL已经推出了新版本：{remoteVerString}。可前往https://github.com/Orange-Icepop/LSL/releases下载。{Environment.NewLine}{updateMessage}";
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        AppState.ITAUnits.PopupITA
                            .Handle(new InvokePopupArgs(PopupType.Info_Confirm, "LSL更新提示", message))
                            .Subscribe();
                    });
                }
                else
                    Dispatcher.UIThread.Post(() =>
                        AppState.ITAUnits.Notify(1, "更新检查完毕", $"当前LSL版本已为最新：{Constant.Version}"));
                _logger.LogInformation("Check for updates completed.");
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, "更新检查出错",
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

    public class UUID_User
    {
        public UUID_User(string uuid, string user)
        {
            this.UUID = uuid;
            this.User = user;
        }

        public string UUID { get; set; }
        public string User { get; set; }
    }

    #endregion
}
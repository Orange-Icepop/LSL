using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Threading;
using LSL.IPC;
using LSL.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels
{
    // 用于连接视图模型与服务层
    // 主要成员为void，用于调用服务层方法
    public class ServiceConnector
    {
        private AppStateLayer AppState { get; }
        private ServerHost daemonHost { get; }
        private ServerOutputStorage outputStorage { get; }
        private ILogger<ServiceConnector> _logger { get; }
        public ServiceConnector(AppStateLayer appState, ServerHost daemon, ServerOutputStorage optStorage)
        {
            AppState = appState;
            daemonHost = daemon;
            outputStorage = optStorage;
            _logger = AppState.LoggerFactory.CreateLogger<ServiceConnector>();
            EventBus.Instance.Subscribe<IStorageArgs>(args => ServerOutputChannel.Writer.TryWrite(args));
            _handleOutputTask = Task.Run(() => HandleOutput(OutputCts.Token));
            CopyServerOutput();
        }

        #region 配置部分

        public async Task<ServiceError> GetConfig(bool readFile = false)
        {
            _logger.LogInformation("start loading main config");
            if (readFile)
            {
                var res = ConfigManager.LoadConfig();
                var shouldShut = await AppState.ITAUnits.SubmitServiceError(res);
                if (shouldShut)
                {
                    var err = res.Error?.ToString() ?? res.Message ?? string.Empty;
                    _logger.LogCritical("Fatal error when loading LSL main config.{nl}{err} ",Environment.NewLine, err);
                    Environment.Exit(1);
                    return res;
                }
            }

            AppState.CurrentConfigs = ConfigManager.CurrentConfigs;
            _logger.LogInformation("loading main config completed");
            return ServiceError.Success;
        }

        public async Task<ServiceError> ReadJavaConfig(bool readFile = false)
        {
            _logger.LogInformation("start loading java config");
            if (readFile)
            {
                var res = JavaManager.ReadJavaConfig();
                var shouldShut = await AppState.ITAUnits.SubmitServiceError(res);
                if (shouldShut)
                {
                    var err = res.Error?.ToString() ?? res.Message ?? string.Empty;
                    _logger.LogCritical("Fatal error when loading java config.{nl}{err} ",Environment.NewLine, err);
                    Environment.Exit(1);
                    return res;
                }
            }
            AppState.CurrentJavaDict = JavaManager.JavaDict;
            _logger.LogInformation("loading java config completed");
            return ServiceError.Success;
        }

        public async Task<ServiceError> ReadServerConfig(bool readFile = false)
        {
            _logger.LogInformation("start loading server config");
            if (readFile)
            {
                var res = ServerConfigManager.LoadServerConfigs();
                var shouldShut = await AppState.ITAUnits.SubmitServiceError(res);
                if (shouldShut)
                {
                    var err = res.Error?.ToString() ?? res.Message ?? string.Empty;
                    _logger.LogCritical("Fatal error when loading LSL server config.{nl}{err} ",Environment.NewLine, err);
                    Environment.Exit(1);
                    return res;
                }
            }
            var cache = ServerConfigManager.ServerConfigs.ToDictionary(item => item.Key, item => new ServerConfig(item.Value));
            if (cache.Count == 0)
            {
                cache.Add(-1, ServerConfig.None);
            }
            AppState.CurrentServerConfigs = cache;
            _logger.LogInformation("loading server config completed");
            return ServiceError.Success;
        }

        public void SaveConfig()
        {
            ConfigManager.ConfirmConfig(AppState.CurrentConfigs);
            _logger.LogInformation("New config saved");
        }

        public async Task FindJava()
        {
            _logger.LogInformation("start finding java");
            await JavaManager.DetectJava();
            _logger.LogInformation("java detection completed");
            await Dispatcher.UIThread.InvokeAsync(() => ReadJavaConfig());
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

        public void SendCommandToServer(int serverId, string command)
        {
            if (string.IsNullOrEmpty(command)) return;
            daemonHost.SendCommand(serverId, command);
        }

        #endregion

        #region 启动前校验配置文件

        public string? VerifyServerConfigBeforeStart(int serverId)
        {
            if (!ServerConfigManager.ServerConfigs.TryGetValue(serverId, out var config))
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
            var checkResult = CheckService.VerifyServerConfig(config, skipCP);
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

        public bool AddServer(FormedServerConfig config)
        {
            try
            {
                ServerConfigManager.RegisterServer(config.ServerName, config.JavaPath, config.CorePath,
                    uint.Parse(config.MaxMem),
                    uint.Parse(config.MinMem), config.ExtJvm);
                ReadServerConfig(true);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool EditServer(int id, FormedServerConfig config)
        {
            try
            {
                ServerConfigManager.EditServer(id, config.ServerName, config.JavaPath, 
                    uint.Parse(config.MinMem),
                    uint.Parse(config.MaxMem), config.ExtJvm);
                ReadServerConfig(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        public string? DeleteServer(int serverId)
        {
            try
            {
                ServerConfigManager.DeleteServer(serverId);
                ReadServerConfig(true);
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
            return null;
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
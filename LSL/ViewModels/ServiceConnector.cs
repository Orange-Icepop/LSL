using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using LSL.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels
{
    // 用于连接视图模型与服务层
    // 主要成员为void，用于调用服务层方法
    public class ServiceConnector
    {
        public AppStateLayer AppState { get; set; }
        public ServiceConnector(AppStateLayer appState)
        {
            AppState = appState;
            EventBus.Instance.Subscribe<ColorOutputArgs>(args => ServerOutputChannel.Writer.TryWrite(args));
            EventBus.Instance.Subscribe<ServerStatusArgs>(args => ServerOutputChannel.Writer.TryWrite(args));
            EventBus.Instance.Subscribe<PlayerUpdateArgs>(args => ServerOutputChannel.Writer.TryWrite(args));
            EventBus.Instance.Subscribe<PlayerMessageArgs>(args => ServerOutputChannel.Writer.TryWrite(args));
            Task.Run(() => HandleOutput(OutputCts.Token));
        }

        #region 配置部分
        public void GetConfig(bool readFile = false)
        {
            if (readFile) ConfigManager.LoadConfig();
            AppState.CurrentConfigs = ConfigManager.CurrentConfigs;
        }

        public void ReadJavaConfig(bool readFile = false)
        {
            if (readFile) JavaManager.ReadJavaConfig();
            AppState.CurrentJavaDict = JavaManager.JavaDict;
        }

        public void ReadServerConfig(bool readFile = false)
        {
            if (readFile)
            {
                var result = ServerConfigManager.LoadServerConfigs();
                AppState.ITAUnits.ShowServiceError(result);
            }
            AppState.CurrentServerConfigs = ServerConfigManager.ServerConfigs;
        }

        public void SaveConfig()
        {
            ConfigManager.ConfirmConfig(AppState.CurrentConfigs);
        }

        public async Task FindJava()
        {
            await JavaManager.DetectJava();
            await Dispatcher.UIThread.InvokeAsync(() => ReadJavaConfig());
        }

        #endregion

        #region 服务器命令
        public void StartSelectedServer()
        {
            int serverId = AppState.SelectedServerId;
            var result = VerifyServerConfigBeforeStart(serverId);
            if (result != null)
            {
                AppState.ITAUnits.ThrowError(result);
                return;
            }
            AppState.TerminalTexts.TryAdd(AppState.SelectedServerId, new());
            ServerHost.Instance.RunServer(serverId);
        }
        public async Task StopSelectedServer()
        {
            var confirm = await AppState.ITAUnits.PopupITA.Handle(new(PopupType.Warning_YesNo, "确定要关闭该服务器吗？", "将会立刻踢出服务器内所有玩家，服务器上的最新更改会被保存。")).ToTask();
            if (confirm == PopupResult.Yes) ServerHost.Instance.StopServer(AppState.SelectedServerId);
        }
        public void SaveSelectedServer()
        {
            ServerHost.Instance.SendCommand(AppState.SelectedServerId, "save-all");
        }
        public async Task EndSelectedServer()
        {
            var confirm = await AppState.ITAUnits.PopupITA.Handle(new(PopupType.Warning_YesNo, "确定要终止该服务端进程吗？", "如果强制退出，将会立刻踢出服务器内所有玩家，并且可能会导致服务端最新更改不被保存！")).ToTask();
            if (confirm == PopupResult.Yes) ServerHost.Instance.EndServer(AppState.SelectedServerId);
        }
        public void SendCommandToServer(string command)
        {
            if (string.IsNullOrEmpty(command)) return;
            ServerHost.Instance.SendCommand(AppState.SelectedServerId, command);
        }
        #endregion

        #region 启动前校验配置文件
        public string? VerifyServerConfigBeforeStart(int serverId)
        {
            if (!ServerConfigManager.ServerConfigs.TryGetValue(serverId, out var config)) return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
            else if (config == null) return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
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
        private readonly CancellationTokenSource OutputCts = new();
        private async Task HandleOutput(CancellationToken token)
        {
            await foreach (var args in ServerOutputChannel.Reader.ReadAllAsync(token))
            {
                try { await ArgsProcessor(args); }
                catch { }
            }
        }
        private async Task ArgsProcessor(IStorageArgs args)
        {
            switch (args)
            {
                case ColorOutputArgs COA:
                    await Dispatcher.UIThread.InvokeAsync(() => AppState.TerminalTexts.AddOrUpdate(COA.ServerId, [new ColoredLines(COA.Output, COA.ColorHex)], (key, value) =>
                    {
                        value.Add(new ColoredLines(COA.Output, COA.ColorHex));
                        return value;
                    }));
                    break;
                case ServerStatusArgs SSA:
                    await Dispatcher.UIThread.InvokeAsync(() => AppState.ServerStatuses.AddOrUpdate(SSA.ServerId, new ServerStatus(SSA.IsRunning, SSA.IsOnline), (key, value) => value.Update(SSA.IsRunning, SSA.IsOnline)));
                    break;
                case PlayerUpdateArgs PUA:
                    await Dispatcher.UIThread.InvokeAsync(() => UpdateUser(PUA));
                    break;
                case PlayerMessageArgs PMA:
                    await Dispatcher.UIThread.InvokeAsync(() => AppState.MessageDict.AddOrUpdate(PMA.ServerId, [new(PMA.Message)], (key, value) =>
                    {
                        value.Add(new(PMA.Message));
                        return value;
                    }));
                    break;
                default:
                    break;
            }
        }
        private void UpdateUser(PlayerUpdateArgs args)
        {
            if (args.Entering)
            {
                AppState.UserDict.AddOrUpdate(args.ServerId, [new UUID_User(args.UUID, args.PlayerName)], (key, oldValue) =>
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
                        if (uc[i].UUID == args.UUID)
                        {
                            uc.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        private void CopyServerOutput()
        {

        }

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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LSL.Services;

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
            if (readFile) ServerConfigManager.LoadServerConfigs();
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

        #region 服务器部分
        public void StartSelectedServer()
        {
            int serverId = AppState.SelectedServerId;
            var result = VerifyServerConfigBeforeStart(serverId);
            if (result != null)
            {
                QuickHandler.ThrowError(result);
                return;
            }
            AppState.TerminalTexts.TryAdd(AppState.SelectedServerId, new());
            ServerHost.Instance.RunServer(serverId);
        }
        public void StopSelectedServer()
        {
            ServerHost.Instance.SendCommand(AppState.SelectedServerId, "stop");
        }
        public void SaveSelectedServer()
        {
            ServerHost.Instance.SendCommand(AppState.SelectedServerId, "save-all");
        }
        public void EndSelectedServer()
        {
            ServerHost.Instance.EndServer(AppState.SelectedServerId);
        }
        public void SendCommandToServer(string command)
        {
            if (string.IsNullOrEmpty(command)) return;
            ServerHost.Instance.SendCommand(AppState.SelectedServerId, command);
        }

        #region 启动前校验配置文件
        public string? VerifyServerConfigBeforeStart(int serverId)
        {
            if (ServerConfigManager.ServerConfigs.TryGetValue(serverId, out var config)) return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
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
        private Channel<ColorOutputArgs> ServerOutputChannel = Channel.CreateUnbounded<ColorOutputArgs>();
        private CancellationTokenSource OutputCts = new();
        private async Task HandleOutput(CancellationToken token)
        {
            await foreach (var args in ServerOutputChannel.Reader.ReadAllAsync(token))
            {
                await Dispatcher.UIThread.InvokeAsync(() => AppState.TerminalTexts[AppState.SelectedServerId].Add(new ColoredLines(args.Output, args.Color)));
            }
        }
        #endregion

        private void CopyServerOutput()
        {

        }

        #endregion
    }
}

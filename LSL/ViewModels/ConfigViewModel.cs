using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LSL.IPC;

namespace LSL.ViewModels
{
    public class ConfigViewModel : RegionalVMBase
    {
        public ConfigViewModel(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
            AppState.WhenAnyValue(AS => AS.CurrentJavaDict)
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
            AppState.ServerIdChanged.Subscribe(Id => RaiseServerConfigChanged(Id, null));
            AppState.ServerConfigChanged.Select(SC => !SC.TryGetValue(-1, out _))
                .ToPropertyEx(this, x => x.EnableConfig);
            AppState.ServerConfigChanged.Subscribe(SC => RaiseServerConfigChanged(null, SC));
            GetConfig(true); // cached config需要手动同步，不能依赖自动更新
            ReadServerConfig(true); // 服务器配置由于较为复杂，统一为手动控制
            Connector.ReadJavaConfig(true);
            DeleteServerCmd = ReactiveCommand.Create(async () => await DeleteServer());
        }

        private Dictionary<string, object> cachedConfig = [];

        #region 核心配置数据

        public bool AutoEula
        {
            get => (bool)cachedConfig["auto_eula"];
            set { CacheConfig("auto_eula", value); }
        }

        public int AppPriority
        {
            get => (int)cachedConfig["app_priority"];
            set { CacheConfig("app_priority", value); }
        }

        public bool EndServerWhenClose
        {
            get => (bool)cachedConfig["end_server_when_close"];
            set { CacheConfig("end_server_when_close", value); }
        }

        public bool Daemon
        {
            get => (bool)cachedConfig["daemon"];
            set { CacheConfig("daemon", value); }
        }

        public bool ColoringTerminal
        {
            get => (bool)cachedConfig["coloring_terminal"];
            set { CacheConfig("coloring_terminal", value); }
        }

        public int DownloadSource
        {
            get => (int)cachedConfig["download_source"];
            set { CacheConfig("download_source", value); }
        }

        public int DownloadThreads
        {
            get => (int)cachedConfig["download_threads"];
            set { CacheConfig("download_threads", value); }
        }

        [DownloadLimitValidator]
        public string? DownloadLimit
        {
            get => cachedConfig["download_limit"].ToString();
            set { CacheConfig("download_limit", value); }
        }

        public bool PanelEnable
        {
            get => (bool)cachedConfig["panel_enable"];
            set { CacheConfig("panel_enable", value); }
        }

        [PanelPortValidator]
        public string? PanelPort
        {
            get => cachedConfig["panel_port"].ToString();
            set { CacheConfig("panel_port", value); }
        }

        public bool PanelMonitor
        {
            get => (bool)cachedConfig["panel_monitor"];
            set { CacheConfig("panel_monitor", value); }
        }

        public bool PanelTerminal
        {
            get => (bool)cachedConfig["panel_terminal"];
            set { CacheConfig("panel_terminal", value); }
        }

        public bool AutoUpdate
        {
            get => (bool)cachedConfig["auto_update"];
            set { CacheConfig("auto_update", value); }
        }

        public bool BetaUpdate
        {
            get => (bool)cachedConfig["beta_update"];
            set { CacheConfig("auto_eula", value); }
        }

        #endregion

        #region 主配置操作

        public void GetConfig(bool rf = false)
        {
            Connector.GetConfig(rf);
            cachedConfig = AppState.CurrentConfigs;
        }

        private void CacheConfig(string key, object? value) // 向缓存字典中写入新配置
        {
            if (value == null) return;
            if (!cachedConfig.TryAdd(key, value))
            {
                cachedConfig[key] = value;
            }
        }

        public void ConfirmConfig()
        {
            AppState.CurrentConfigs = cachedConfig;
            Connector.SaveConfig();
        }

        #endregion

        #region 服务器配置操作

        public void ReadServerConfig(bool rf = false)
        {
            Connector.ReadServerConfig(rf);
        }
        public ICommand DeleteServerCmd { get; }
        public bool EnableConfig { [ObservableAsProperty] get; }
        public async Task DeleteServer()
        {
            int serverId = AppState.SelectedServerId;
            var result1 = await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Warning_YesNo,
                "确认删除该服务器吗？",
                "注意！此操作不可逆！" + Environment.NewLine + "服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
            if (result1 == PopupResult.No) return;
            var result2 = await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Warning_YesNo,
                "第二次确认，删除该服务器吗？",
                "注意！此操作不可逆！" + Environment.NewLine + "服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
            if (result2 == PopupResult.No) return;
            var result3 = await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Warning_YesNo,
                "最后一次确认，你确定要删除该服务器吗？",
                "这是最后一次警告！此操作不可逆！" + Environment.NewLine + "服务器的所有文件（包括存档、模组、核心文件）都会被完全删除，不会放入回收站！"));
            if (result3 == PopupResult.No) return;
            var deleteResult = Connector.DeleteServer(serverId);
            if (string.IsNullOrEmpty(deleteResult))
            {
                AppState.ITAUnits.Notify(1, null, $"服务器{serverId}删除成功");
            }
            else
            {
                AppState.ITAUnits.ThrowError("删除服务器出错", deleteResult);
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

        private void RaiseServerConfigChanged(int? serverId, Dictionary<int,ServerConfig>? serverConfig)
        {
            var SI = serverId ?? AppState.SelectedServerId;
            var SCS = serverConfig ?? AppState.CurrentServerConfigs;
            if (SCS.TryGetValue(SI, out var SC))
            {
                SelectedServerConfig = SC;
                SelectedServerName = SC.name;
                SelectedServerPath = SC.server_path;
            }
            else
            {
                SelectedServerConfig = ServerConfig.None;
                SelectedServerName = ServerConfig.None.name;
                SelectedServerPath = ServerConfig.None.server_path;
            }
        }
        #endregion
    }
}
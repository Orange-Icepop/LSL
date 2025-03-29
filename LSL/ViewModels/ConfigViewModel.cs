using LSL.Services;
using LSL.Services.Validators;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    public class ConfigViewModel : RegionalVMBase
    {
        public ConfigViewModel(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
            AppState.WhenAnyValue(AS => AS.CurrentJavaDict)
                .Select(s => new ObservableCollection<JavaInfo>(s.Values))
                .ToPropertyEx(this, x => x.JavaVersions);
            GetConfig(true);// cached config需要手动同步，不能依赖自动更新
            ReadServerConfig(true);// 服务器配置由于较为复杂，统一为手动控制
            Connector.ReadJavaConfig(true);
        }

        private Dictionary<string, object> cachedConfig = [];

        #region 核心配置数据
        public bool AutoEula { get => (bool)cachedConfig["auto_eula"]; set { CacheConfig("auto_eula", value); } }
        public int AppPriority { get => (int)cachedConfig["app_priority"]; set { CacheConfig("app_priority", value); } }
        public bool EndServerWhenClose { get => (bool)cachedConfig["end_server_when_close"]; set { CacheConfig("end_server_when_close", value); } }
        public bool Daemon { get => (bool)cachedConfig["daemon"]; set { CacheConfig("daemon", value); } }
        public bool ColoringTerminal { get => (bool)cachedConfig["coloring_terminal"]; set { CacheConfig("coloring_terminal", value); } }
        public int DownloadSource { get => (int)cachedConfig["download_source"]; set { CacheConfig("download_source", value); } }
        public int DownloadThreads { get => (int)cachedConfig["download_threads"]; set { CacheConfig("download_threads", value); } }
        [DownloadLimitValidator]
        public string? DownloadLimit { get => cachedConfig["download_limit"].ToString(); set { CacheConfig("download_limit", value); } }
        public bool PanelEnable { get => (bool)cachedConfig["panel_enable"]; set { CacheConfig("panel_enable", value); } }
        [PanelPortValidator]
        public string? PanelPort { get => cachedConfig["panel_port"].ToString(); set { CacheConfig("panel_port", value); } }
        public bool PanelMonitor { get => (bool)cachedConfig["panel_monitor"]; set { CacheConfig("panel_monitor", value); } }
        public bool PanelTerminal { get => (bool)cachedConfig["panel_terminal"]; set { CacheConfig("panel_terminal", value); } }
        public bool AutoUpdate { get => (bool)cachedConfig["auto_update"]; set { CacheConfig("auto_update", value); } }
        public bool BetaUpdate { get => (bool)cachedConfig["beta_update"]; set { CacheConfig("auto_eula", value); } }
        #endregion

        #region 主配置操作
        public void GetConfig(bool rf = false)
        {
            Connector.GetConfig(rf);
            cachedConfig = AppState.CurrentConfigs;
        }

        private void CacheConfig(string key, object? value)// 向缓存字典中写入新配置
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
        #endregion

        #region Java配置
        public ObservableCollection<JavaInfo> JavaVersions { [ObservableAsProperty] get; }
        #endregion
    }
}

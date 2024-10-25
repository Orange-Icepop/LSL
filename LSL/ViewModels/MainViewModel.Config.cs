using System;
using System.IO;
using System.Collections.Generic;
using LSL.ViewModels;
using LSL.Services;
using ReactiveUI;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        #region 配置变量
        //用ReactiveUI的办法真的是太烦了
        //但是又不能用ObservableProperty，因为要自定义更改后的操作
        public bool AutoEula { get => (bool)ViewConfigs["auto_eula"]; set { CacheConfig("auto_eula", value); } }
        public int AppPriority { get => (int)ViewConfigs["app_priority"]; set { CacheConfig("app_priority", value); } }
        public bool EndServerWhenClose { get => (bool)ViewConfigs["end_server_when_close"]; set { CacheConfig("end_server_when_close", value); } }
        public bool Daemon { get => (bool)ViewConfigs["daemon"]; set { CacheConfig("daemon", value); } }
        public int JavaSelection { get => (int)ViewConfigs["java_selection"]; set { CacheConfig("java_selection", value); } }
        public bool AutoFindJava { get => (bool)ViewConfigs["auto_find_java"]; set { CacheConfig("auto_find_java", value); } }
        public int OutputEncode { get => (int)ViewConfigs["output_encode"]; set { CacheConfig("output_encode", value); } }
        public int InputEncode { get => (int)ViewConfigs["input_encode"]; set { CacheConfig("input_encode", value); } }
        public bool ColoringTerminal { get => (bool)ViewConfigs["coloring_terminal"]; set { CacheConfig("coloring_terminal", value); } }
        public int DownloadSource { get => (int)ViewConfigs["download_source"]; set { CacheConfig("download_source", value); } }
        public int DownloadThreads { get => (int)ViewConfigs["download_threads"]; set { CacheConfig("download_threads", value); } }
        public int DownloadLimit { get => (int)ViewConfigs["download_limit"]; set { CacheConfig("download_limit", value); } }
        public bool PanelEnable { get => (bool)ViewConfigs["panel_enable"]; set { CacheConfig("panel_enable", value); } }
        public int PanelPort { get => (int)ViewConfigs["panel_port"]; set { CacheConfig("panel_port", value); } }
        public bool PanelMonitor { get => (bool)ViewConfigs["panel_monitor"]; set { CacheConfig("panel_monitor", value); } }
        public bool PanelTerminal { get => (bool)ViewConfigs["panel_terminal"]; set { CacheConfig("panel_terminal", value); } }
        public bool AutoUpdate { get => (bool)ViewConfigs["auto_update"]; set { CacheConfig("auto_update", value); } }
        public bool BetaUpdate { get => (bool)ViewConfigs["beta_update"]; set { CacheConfig("auto_eula", value); } }
        #endregion

        #region 读写配置文件
        public Dictionary<string, object> ViewConfigs;// 视图层所用的缓存字典

        public void GetConfig()
        {
            ConfigManager.LoadConfig();
            ViewConfigs = new Dictionary<string, object>( ConfigManager.CurrentConfigs );
            /*
            autoEula = (bool)ViewConfigs["auto_eula"];
            appPriorityCache = (int)ViewConfigs["app_priority"];
            endServerWhenClose = (bool)ViewConfigs["end_server_when_close"];
            daemon = (bool)ViewConfigs["daemon"];
            javaSelectionCache = (int)ViewConfigs["java_selection"];
            autoFindJava = (bool)ViewConfigs["auto_find_java"];
            outputEncodeCache = (int)ViewConfigs["output_encode"];
            inputEncodeCache = (int)ViewConfigs["input_encode"];
            coloringTerminal = (bool)ViewConfigs["coloring_terminal"];
            downloadSourceCache = (int)ViewConfigs["download_source"];
            downloadThreadsCache = (int)ViewConfigs["download_threads"];
            downloadLimitCache = (int)ViewConfigs["download_limit"];
            panelEnable = (bool)ViewConfigs["panel_enable"];
            panelPortCache = (int)ViewConfigs["panel_port"];
            panelMonitor = (bool)ViewConfigs["panel_monitor"];
            panelTerminal = (bool)ViewConfigs["panel_terminal"];
            autoUpdate = (bool)ViewConfigs["auto_update"];
            betaUpdate = (bool)ViewConfigs["beta_update"];
            */
        }

        public void CacheConfig(string key, object value)// 向缓存字典中写入新配置
        {
            if (!ViewConfigs.TryAdd(key, value))
            {
                ViewConfigs[key] = value;
            }
        }

        
        #endregion

        #region 存储文件路径方法
        public void SaveFilePath(string path, string targetValue)
        {
            switch (targetValue)
            {
                case "CorePath":
                    CorePath = path;
                    break;
            }
        }
        #endregion

        #region 新增服务器数据
        private string _corePath = string.Empty;// 核心文件路径
        public string CorePath { get => _corePath; set => this.RaiseAndSetIfChanged(ref _corePath, value); }

        private string _newServerName = string.Empty;// 新增服务器名称
        public string NewServerName { get => _newServerName; set => this.RaiseAndSetIfChanged(ref _newServerName, value); }

        private int _minMemory = 0;// 新增服务器最小内存
        public int MinMemory { get => _minMemory; set => this.RaiseAndSetIfChanged(ref _minMemory, value); }

        private int _maxMemory = 0;// 新增服务器最大内存
        public int MaxMemory { get => _maxMemory; set => this.RaiseAndSetIfChanged(ref _maxMemory, value); }

        private int _javaId = 0;//Java编号
        public int JavaId { get => _javaId; set => this.RaiseAndSetIfChanged(ref _javaId, value); }

        private string _extJvm = string.Empty;
        public string ExtJvm { get => _extJvm; set => this.RaiseAndSetIfChanged(ref _extJvm, value); }
        #endregion

        public ICommand SearchJava { get; }// 搜索Java命令
        public ICommand ConfirmAddServer { get; }// 确认新增服务器命令
        public ICommand DeleteServer { get; }// 删除服务器命令

        #region 全局获取服务器列表ReadServerList => ServerNames
        //持久化服务器映射列表
        private ObservableCollection<string> _serverIDs = [];
        private ObservableCollection<string> _servernames = [];
        public ObservableCollection<string> ServerIDs => _serverIDs;
        public ObservableCollection<string> ServerNames => _servernames;
        // 服务器列表读取（从配置文件读取）
        public void ReadServerList()
        {
            string jsonContent = File.ReadAllText(ConfigManager.ServerConfigPath);
            JObject jsonObj = JObject.Parse(jsonContent);
            //遍历配置文件中的所有服务器ID
            ServerIDs.Clear();
            foreach (var item in jsonObj.Properties())
            {
                ServerIDs.Add(item.Name);
            }
            //根据服务器ID读取每个服务器的配置文件
            ServerNames.Clear();
            foreach (var ServerID in ServerIDs)
            {
                string TargetedServerPath = (string)JsonHelper.ReadJson(ConfigManager.ServerConfigPath, $"$.{ServerID}");
                string TargetedConfigPath = Path.Combine(TargetedServerPath, "lslconfig.json");
                string KeyPath = "$.name";
                ServerNames.Add((string)JsonHelper.ReadJson(TargetedConfigPath, KeyPath));
            }
            if (SelectedServerIndex > ServerNames.Count)
            {
                SelectedServerIndex = 0;
            }
        }

        #endregion

        #region 全局获取Java列表ReadJavaList => JavaVersions
        //持久化Java映射列表
        private ObservableCollection<string> _javaVersions = [];
        public ObservableCollection<string> JavaVersions => _javaVersions;
        // Java列表读取（从配置文件读取）
        public void ReadJavaList()
        {
            string jsonContent = File.ReadAllText(ConfigManager.JavaListPath);
            JObject jsonObj = JObject.Parse(jsonContent);
            //遍历配置文件中的所有Java
            foreach (var item in jsonObj.Properties())
            {
                JToken versionObject = item.Value["version"];
                if (versionObject != null && versionObject.Type == JTokenType.String)
                {
                    JavaVersions.Add(versionObject.ToString());
                }
            }
        }

        #endregion

    }
}
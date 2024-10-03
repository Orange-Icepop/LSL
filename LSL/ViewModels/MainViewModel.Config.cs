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
        private bool autoEula;
        private int appPriority;
        private bool endServerWhenClose;
        private bool daemon;
        private int javaSelection;
        private bool autoFindJava;
        private int outputEncode;
        private int inputEncode;
        private bool coloringTerminal;
        private int downloadSource;
        private int downloadThreads;
        private int downloadLimit;
        private bool panelEnable;
        private int panelPort;
        private bool panelMonitor;
        private bool panelTerminal;
        private bool autoUpdate;
        private bool betaUpdate;
        //可观测对象，最烦的一堆史
        public bool AutoEula { get => autoEula; set { this.RaiseAndSetIfChanged(ref autoEula, value); ConfigManager.ModifyConfig("auto_eula", value); } }
        public int AppPriority { get => appPriority; set { this.RaiseAndSetIfChanged(ref appPriority, value); ConfigManager.ModifyConfig("app_priority", value); } }
        public bool EndServerWhenClose { get => endServerWhenClose; set { this.RaiseAndSetIfChanged(ref endServerWhenClose, value); ConfigManager.ModifyConfig("end_server_when_close", value); } }
        public bool Daemon { get => daemon; set { this.RaiseAndSetIfChanged(ref daemon, value); ConfigManager.ModifyConfig("daemon", value); } }
        public int JavaSelection { get => javaSelection; set { this.RaiseAndSetIfChanged(ref javaSelection, value); ConfigManager.ModifyConfig("java_selection", value); } }
        public bool AutoFindJava { get => autoFindJava; set { this.RaiseAndSetIfChanged(ref autoFindJava, value); ConfigManager.ModifyConfig("auto_find_java", value); } }
        public int OutputEncode { get => outputEncode; set { this.RaiseAndSetIfChanged(ref outputEncode, value); ConfigManager.ModifyConfig("output_encode", value); } }
        public int InputEncode { get => inputEncode; set { this.RaiseAndSetIfChanged(ref inputEncode, value); ConfigManager.ModifyConfig("input_encode", value); } }
        public bool ColoringTerminal { get => coloringTerminal; set { this.RaiseAndSetIfChanged(ref coloringTerminal, value); ConfigManager.ModifyConfig("coloring_terminal", value); } }
        public int DownloadSource { get => downloadSource; set { this.RaiseAndSetIfChanged(ref downloadSource, value); ConfigManager.ModifyConfig("download_source", value); } }
        public int DownloadThreads { get => downloadThreads; set { this.RaiseAndSetIfChanged(ref downloadThreads, value); ConfigManager.ModifyConfig("download_threads", value); } }
        public int DownloadLimit { get => downloadLimit; set { this.RaiseAndSetIfChanged(ref downloadLimit, value); ConfigManager.ModifyConfig("download_limit", value); } }
        public bool PanelEnable { get => panelEnable; set { this.RaiseAndSetIfChanged(ref panelEnable, value); ConfigManager.ModifyConfig("panel_enable", value); } }
        public int PanelPort { get => panelPort; set { this.RaiseAndSetIfChanged(ref panelPort, value); ConfigManager.ModifyConfig("panel_port", value); } }
        public bool PanelMonitor { get => panelMonitor; set { this.RaiseAndSetIfChanged(ref panelMonitor, value); ConfigManager.ModifyConfig("panel_monitor", value); } }
        public bool PanelTerminal { get => panelTerminal; set { this.RaiseAndSetIfChanged(ref panelTerminal, value); ConfigManager.ModifyConfig("panel_terminal", value); } }
        public bool AutoUpdate { get => autoUpdate; set { this.RaiseAndSetIfChanged(ref autoUpdate, value); ConfigManager.ModifyConfig("auto_update", value); } }
        public bool BetaUpdate { get => betaUpdate; set { this.RaiseAndSetIfChanged(ref betaUpdate, value); ConfigManager.ModifyConfig("auto_eula", value); } }
        //缓冲验证变量
        private int appPriorityCache;
        private int javaSelectionCache;
        private int outputEncodeCache;
        private int inputEncodeCache;
        private int downloadSourceCache;
        private int downloadThreadsCache;
        private int downloadLimitCache;
        private int panelPortCache;
        #endregion

        #region 读取配置文件
        public void GetConfig()
        {
            ConfigManager.LoadConfig();
            autoEula = (bool)ConfigManager.CurrentConfigs["auto_eula"];
            appPriorityCache = (int)ConfigManager.CurrentConfigs["app_priority"];
            endServerWhenClose = (bool)ConfigManager.CurrentConfigs["end_server_when_close"];
            daemon = (bool)ConfigManager.CurrentConfigs["daemon"];
            javaSelectionCache = (int)ConfigManager.CurrentConfigs["java_selection"];
            autoFindJava = (bool)ConfigManager.CurrentConfigs["auto_find_java"];
            outputEncodeCache = (int)ConfigManager.CurrentConfigs["output_encode"];
            inputEncodeCache = (int)ConfigManager.CurrentConfigs["input_encode"];
            coloringTerminal = (bool)ConfigManager.CurrentConfigs["coloring_terminal"];
            downloadSourceCache = (int)ConfigManager.CurrentConfigs["download_source"];
            downloadThreadsCache = (int)ConfigManager.CurrentConfigs["download_threads"];
            downloadLimitCache = (int)ConfigManager.CurrentConfigs["download_limit"];
            panelEnable = (bool)ConfigManager.CurrentConfigs["panel_enable"];
            panelPortCache = (int)ConfigManager.CurrentConfigs["panel_port"];
            panelMonitor = (bool)ConfigManager.CurrentConfigs["panel_monitor"];
            panelTerminal = (bool)ConfigManager.CurrentConfigs["panel_terminal"];
            autoUpdate = (bool)ConfigManager.CurrentConfigs["auto_update"];
            betaUpdate = (bool)ConfigManager.CurrentConfigs["beta_update"];
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
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LSL.Services;
using LSL.Services.Validators;
using ReactiveUI;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        // 启动器配置文件板块
        #region 核心配置
        public bool AutoEula { get => (bool)ViewConfigs["auto_eula"]; set { CacheConfig("auto_eula", value); } }
        public int AppPriority { get => (int)ViewConfigs["app_priority"]; set { CacheConfig("app_priority", value); } }
        public bool EndServerWhenClose { get => (bool)ViewConfigs["end_server_when_close"]; set { CacheConfig("end_server_when_close", value); } }
        public bool Daemon { get => (bool)ViewConfigs["daemon"]; set { CacheConfig("daemon", value); } }
        public bool AutoFindJava { get => (bool)ViewConfigs["auto_find_java"]; set { CacheConfig("auto_find_java", value); } }
        public int OutputEncode { get => (int)ViewConfigs["output_encode"]; set { CacheConfig("output_encode", value); } }
        public int InputEncode { get => (int)ViewConfigs["input_encode"]; set { CacheConfig("input_encode", value); } }
        public bool ColoringTerminal { get => (bool)ViewConfigs["coloring_terminal"]; set { CacheConfig("coloring_terminal", value); } }
        public int DownloadSource { get => (int)ViewConfigs["download_source"]; set { CacheConfig("download_source", value); } }
        public int DownloadThreads { get => (int)ViewConfigs["download_threads"]; set { CacheConfig("download_threads", value); } }
        [DownloadLimitValidator]
        public string? DownloadLimit { get => ViewConfigs["download_limit"].ToString(); set { CacheConfig("download_limit", value); } }
        public bool PanelEnable { get => (bool)ViewConfigs["panel_enable"]; set { CacheConfig("panel_enable", value); } }
        [PanelPortValidator]
        public string? PanelPort { get => ViewConfigs["panel_port"].ToString(); set { CacheConfig("panel_port", value); } }
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
            ViewConfigs = new Dictionary<string, object>(ConfigManager.CurrentConfigs);
        }

        public void CacheConfig(string key, object value)// 向缓存字典中写入新配置
        {
            if (!ViewConfigs.TryAdd(key, value))
            {
                ViewConfigs[key] = value;
            }
        }


        #endregion

        // 服务器管理板块
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
        // TODO:将初始值的设定交给切换页面的方法
        #region 服务器数据

        private string _newServerName = "NewServer";// 服务器名称
        [ServerNameValidator] public string NewServerName { get => _newServerName; set => this.RaiseAndSetIfChanged(ref _newServerName, value); }

        private string _corePath = string.Empty;// 核心文件路径
        [ServerCorePathValidator] public string CorePath { get => _corePath; set => this.RaiseAndSetIfChanged(ref _corePath, value); }

        private string _minMemory = "200";// 服务器最小内存
        [MinMemValidator] public string MinMemory { get => _minMemory.ToString(); set => this.RaiseAndSetIfChanged(ref _minMemory, value); }

        private string _maxMemory = "200";// 服务器最大内存
        [MaxMemValidator] public string MaxMemory { get => _maxMemory.ToString(); set => this.RaiseAndSetIfChanged(ref _maxMemory, value); }

        private int _javaId = 0;//Java编号
        public int JavaId { get => _javaId; set => this.RaiseAndSetIfChanged(ref _javaId, value); }

        private string _extJvm = string.Empty;// 附加JVM参数
        [ExtJvmValidator] public string ExtJvm { get => _extJvm; set => this.RaiseAndSetIfChanged(ref _extJvm, value); }
        //TODO:在修改配置时查找指定的Java，并自动填充JavaId
        public void ReadServerConfig(string serverID)
        {
            string serverName = ServerNames[ServerIDs.IndexOf(serverID)];
            var ConfigDict = ServerConfigManager.ReadServerConfig(serverName);
            if (ConfigDict != null) throw new Exception("读取服务器配置失败，指定的服务器配置文件不存在或已损坏");
        }
        #endregion

        public ICommand SearchJava { get; }// 搜索Java命令
        public ICommand ConfirmAddServer { get; }// 确认新增服务器命令
        public ICommand DeleteServer { get; }// 删除服务器命令
        public ICommand EditServer { get; }// 编辑服务器命令

        #region 全局获取服务器列表ReadServerList => ServerNames
        //持久化服务器映射列表
        private ObservableCollection<string> _serverIDs = [];// 主配置文件中的服务器ID列表
        private ObservableCollection<string> _servernames = [];// 服务器名称列表，以ServerID的顺序排列
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
                try
                {
                    string TargetedServerPath = (string)JsonHelper.ReadJson(ConfigManager.ServerConfigPath, $"$.{ServerID}");
                    string TargetedConfigPath = Path.Combine(TargetedServerPath, "lslconfig.json");
                    string KeyPath = "$.name";
                    ServerNames.Add((string)JsonHelper.ReadJson(TargetedConfigPath, KeyPath));
                }
                catch (DirectoryNotFoundException ex)
                {
                    ServerNames.Add($"NonExist server{ServerID}");
                    //throw new Exception($"服务器 {ServerID} 的路径不存在，请检查配置文件。\r错误消息：{ex.Message}");
                }
            }
            if (SelectedServerIndex > ServerNames.Count)
            {
                SelectedServerIndex = 0;
            }
        }

        #endregion

        // Java板块
        #region 全局获取Java列表ReadJavaList => JavaVersions
        //持久化Java映射列表
        private ObservableCollection<JavaInfo> _javaVersions = [];
        public ObservableCollection<JavaInfo> JavaVersions
        {
            get => _javaVersions;
            set => this.RaiseAndSetIfChanged(ref _javaVersions, value);
        }
        // Java列表读取
        public void ReadJavaList()
        {
            JavaManager.InitJavaDict();
            foreach (var item in JavaManager.JavaDict)
            {
                JavaVersions.Add(item.Value);
            }
        }
        #endregion

    }
}
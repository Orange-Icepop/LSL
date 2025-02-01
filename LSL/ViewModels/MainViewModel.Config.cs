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
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        // 启动器配置文件板块
        #region 核心配置数据
        public bool AutoEula { get => (bool)ViewConfigs["auto_eula"]; set { CacheConfig("auto_eula", value); } }
        public int AppPriority { get => (int)ViewConfigs["app_priority"]; set { CacheConfig("app_priority", value); } }
        public bool EndServerWhenClose { get => (bool)ViewConfigs["end_server_when_close"]; set { CacheConfig("end_server_when_close", value); } }
        public bool Daemon { get => (bool)ViewConfigs["daemon"]; set { CacheConfig("daemon", value); } }
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

        #region 服务器数据

        private string _newServerName;// 服务器名称
        [ServerNameValidator] public string NewServerName { get => _newServerName; set => this.RaiseAndSetIfChanged(ref _newServerName, value); }

        private string _corePath;// 核心文件路径
        [ServerCorePathValidator] public string CorePath { get => _corePath; set => this.RaiseAndSetIfChanged(ref _corePath, value); }

        private string _minMemory;// 服务器最小内存
        [MinMemValidator] public string MinMemory { get => _minMemory.ToString(); set => this.RaiseAndSetIfChanged(ref _minMemory, value); }

        private string _maxMemory;// 服务器最大内存
        [MaxMemValidator] public string MaxMemory { get => _maxMemory.ToString(); set => this.RaiseAndSetIfChanged(ref _maxMemory, value); }

        private int _javaId;//Java编号
        public int JavaId { get => _javaId; set => this.RaiseAndSetIfChanged(ref _javaId, value); }

        private string _extJvm;// 附加JVM参数
        [ExtJvmValidator] public string ExtJvm { get => _extJvm; set => this.RaiseAndSetIfChanged(ref _extJvm, value); }
        #endregion
        //TODO:在修改配置时查找指定的Java，并自动填充JavaId
        #region 新增服务器处理方法
        public async Task AddNewServer()
        {
            string JavaPath = JavaManager.JavaDict[JavaId.ToString()].Path;
            Dictionary<string, string> ServerInfo = new()
            {
                { "ServerName", NewServerName },
                { "JavaPath", JavaPath },
                { "CorePath", CorePath },
                { "MinMem", MinMemory },
                { "MaxMem", MaxMemory },
                { "ExtJvm", ExtJvm }
            };
            var checkResult = CheckService.VerifyServerConfig(ServerInfo);
            string ErrorInfo = "";
            foreach (var item in checkResult)
            {
                if (item.Passed == false)
                {
                    ErrorInfo += $"{item.Reason}\r";
                }
                else continue;
            }
            if (ErrorInfo != "")
            {
                await ShowPopup(5, "服务器配置错误", ErrorInfo);
                return;
            }
            var coreResult = CoreValidationService.Validate(CorePath, out string Problem);
            string validateResult = "";
            switch (coreResult)
            {
                case CoreValidationService.CoreType.Error:
                    {
                        await ShowPopup(5, "验证错误", "验证核心文件时发生错误。\r" + Problem);
                        return;
                    }
                case CoreValidationService.CoreType.ForgeInstaller:
                    {
                        await ShowPopup(5, "核心文件错误", "您选择的文件是一个Forge安装器，而不是一个Minecraft服务端核心文件。");
                        return;
                    }
                case CoreValidationService.CoreType.FabricInstaller:
                    {
                        await ShowPopup(5, "核心文件错误", "您选择的文件是一个Fabric安装器，而不是一个Minecraft服务端核心文件。");
                        return;
                    }
                case CoreValidationService.CoreType.Unknown:
                    {
                        validateResult = await ShowPopup(2, "未知的核心文件类型", "LSL无法确认您选择的文件是否为Minecraft服务端核心文件。\r这可能是由于LSL没有收集足够的关于服务器核心的辨识信息造成的。如果这是确实一个Minecraft服务端核心并且具有一定的知名度，请您前往LSL的仓库（https://github.com/Orange-Icepop/LSL）提交相关Issue。\r您可以直接点击确认绕过校验，但是LSL及其开发团队不为因此造成的后果负任何义务或责任。");
                        break;
                    }
                case CoreValidationService.CoreType.Client:
                    {
                        await ShowPopup(5, "核心文件错误", "您选择的文件是一个Minecraft客户端核心文件，而不是一个服务端核心文件。");
                        return;
                    }
                default:
                    validateResult = "Yes";
                    break;
            }
            if (validateResult != "Yes") return;
            string confirmResult = await ShowPopup(2, "确定添加此服务器吗？", $"服务器信息：\r名称：{NewServerName}\rJava路径：{JavaPath}\r核心文件路径：{CorePath}\r服务器类型：{coreResult}\r内存范围：{MinMemory} ~ {MaxMemory}\r附加JVM参数：{ExtJvm}");
            if (confirmResult == "Yes")
            {
                ServerConfigManager.RegisterServer(NewServerName, JavaPath, CorePath, uint.Parse(_minMemory), uint.Parse(_maxMemory), ExtJvm);
                ReadServerList();
                FullViewBackCmd.Execute(null);
            }
        }
        #endregion
        private void LoadNewServerConfig()
        {
            NewServerName = "NewServer";
            CorePath = "";
            MinMemory = "200";
            MaxMemory = "500";
            JavaId = 0;
            ExtJvm = "-Dlog4j2.formatMsgNoLookups=true";
        }

        private void LoadCurrentServerConfig()
        {
            NewServerName = new string(CurrentServerConfig.name);
            CorePath = new string(CurrentServerConfig.core_name);
            MinMemory = CurrentServerConfig.min_memory.ToString();
            MaxMemory = CurrentServerConfig.max_memory.ToString();
            JavaId = 0;
            ExtJvm = new string(CurrentServerConfig.ext_jvm);
        }

        public ICommand SearchJava { get; }// 搜索Java命令
        public ICommand ConfirmAddServer { get; }// 确认新增服务器命令
        public ICommand DeleteServer { get; }// 删除服务器命令
        public ICommand EditServer { get; }// 编辑服务器命令

        public ServerConfig CurrentServerConfig // 当前服务器的LSL配置文件
        {
            get => ServerConfigManager.ServerConfigs[SelectedServerId];
        }

        #region 全局获取服务器列表ReadServerList => ServerNames
        //持久化服务器映射列表
        private ObservableCollection<string> _serverIDs = [];// 主配置文件中的服务器ID列表
        private ObservableCollection<string> _servernames = [];// 服务器名称列表，以ServerID的顺序排列
        public ObservableCollection<string> ServerIDs => _serverIDs;
        public ObservableCollection<string> ServerNames => _servernames;
        // 服务器列表读取（从配置文件读取）
        public void ReadServerList()
        {
            ServerConfigManager.LoadServerConfigs();
            ServerIDs.Clear();
            ServerNames.Clear();
            foreach (var item in ServerConfigManager.ServerConfigs)
            {
                ServerIDs.Add(item.Value.server_id);
                ServerNames.Add(item.Value.name);
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
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
using Avalonia.Threading;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        // �����������ļ����
        #region ������������
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

        #region ��д�����ļ�
        public Dictionary<string, object> ViewConfigs;// ��ͼ�����õĻ����ֵ�

        public void GetConfig()
        {
            ConfigManager.LoadConfig();
            ViewConfigs = new Dictionary<string, object>(ConfigManager.CurrentConfigs);
        }

        public void CacheConfig(string key, object value)// �򻺴��ֵ���д��������
        {
            if (!ViewConfigs.TryAdd(key, value))
            {
                ViewConfigs[key] = value;
            }
        }


        #endregion

        // ������������
        #region �洢�ļ�·������
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

        #region ����������

        private string _newServerName;// ����������
        [ServerNameValidator] public string NewServerName { get => _newServerName; set => this.RaiseAndSetIfChanged(ref _newServerName, value); }

        private string _corePath;// �����ļ�·��
        [ServerCorePathValidator] public string CorePath { get => _corePath; set => this.RaiseAndSetIfChanged(ref _corePath, value); }

        private string _minMemory;// ��������С�ڴ�
        [MinMemValidator] public string MinMemory { get => _minMemory.ToString(); set => this.RaiseAndSetIfChanged(ref _minMemory, value); }

        private string _maxMemory;// ����������ڴ�
        [MaxMemValidator] public string MaxMemory { get => _maxMemory.ToString(); set => this.RaiseAndSetIfChanged(ref _maxMemory, value); }

        private int _javaId;//Java���
        public int JavaId { get => _javaId; set => this.RaiseAndSetIfChanged(ref _javaId, value); }

        private string _extJvm;// ����JVM����
        [ExtJvmValidator] public string ExtJvm { get => _extJvm; set => this.RaiseAndSetIfChanged(ref _extJvm, value); }
        #endregion
        //TODO:���޸�����ʱ����ָ����Java�����Զ����JavaId
        #region ����������������
        private async Task AddNewServer()
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
                await ShowPopup(5, "���������ô���", ErrorInfo);
                return;
            }
            var coreResult = CoreValidationService.Validate(CorePath, out string Problem);
            string validateResult = "";
            switch (coreResult)
            {
                case CoreValidationService.CoreType.Error:
                    {
                        await ShowPopup(5, "��֤����", "��֤�����ļ�ʱ��������\r" + Problem);
                        return;
                    }
                case CoreValidationService.CoreType.ForgeInstaller:
                    {
                        await ShowPopup(5, "�����ļ�����", "��ѡ����ļ���һ��Forge��װ����������һ��Minecraft����˺����ļ���");
                        return;
                    }
                case CoreValidationService.CoreType.FabricInstaller:
                    {
                        await ShowPopup(5, "�����ļ�����", "��ѡ����ļ���һ��Fabric��װ����������һ��Minecraft����˺����ļ���");
                        return;
                    }
                case CoreValidationService.CoreType.Unknown:
                    {
                        validateResult = await ShowPopup(2, "δ֪�ĺ����ļ�����", "LSL�޷�ȷ����ѡ����ļ��Ƿ�ΪMinecraft����˺����ļ���\r�����������LSLû���ռ��㹻�Ĺ��ڷ��������ĵı�ʶ��Ϣ��ɵġ��������ȷʵһ��Minecraft����˺��Ĳ��Ҿ���һ����֪���ȣ�����ǰ��LSL�Ĳֿ⣨https://github.com/Orange-Icepop/LSL���ύ���Issue��\r������ֱ�ӵ��ȷ���ƹ�У�飬����LSL���俪���ŶӲ�Ϊ�����ɵĺ�����κ���������Ρ�");
                        break;
                    }
                case CoreValidationService.CoreType.Client:
                    {
                        await ShowPopup(5, "�����ļ�����", "��ѡ����ļ���һ��Minecraft�ͻ��˺����ļ���������һ������˺����ļ���");
                        return;
                    }
                default:
                    validateResult = "Yes";
                    break;
            }
            if (validateResult != "Yes") return;
            string confirmResult = await ShowPopup(2, "ȷ����Ӵ˷�������", $"��������Ϣ��\r���ƣ�{NewServerName}\rJava·����{JavaPath}\r�����ļ�·����{CorePath}\r���������ͣ�{coreResult}\r�ڴ淶Χ��{MinMemory} ~ {MaxMemory}\r����JVM������{ExtJvm}");
            if (confirmResult == "Yes")
            {
                ServerConfigManager.RegisterServer(NewServerName, JavaPath, CorePath, uint.Parse(_minMemory), uint.Parse(_maxMemory), ExtJvm);
                ReadServerList();
                Notify(1, null, "���������óɹ���");
                FullViewBackCmd.Execute(null);
            }
        }
        #endregion

        #region �޸ķ���������
        private async Task EditCurrentServer()
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
                await ShowPopup(5, "���������ô���", ErrorInfo);
                return;
            }
            string confirmResult = await ShowPopup(1, "�༭������", $"��������Ϣ��\r���ƣ�{NewServerName}\rJava·����{JavaPath}\r�ڴ淶Χ��{MinMemory} ~ {MaxMemory}\r����JVM������{ExtJvm}");
            if (confirmResult == "Yes")
            {
                await Task.Run(() => ServerConfigManager.EditServer(SelectedServerId, NewServerName, JavaPath, uint.Parse(_minMemory), uint.Parse(_maxMemory), ExtJvm));
                ReadServerList();
                FullViewBackCmd.Execute(null);
            }
        }
        #endregion

        #region ������޸ķ�����ʱ����������
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
            CorePath = Path.Combine(ConfigManager.ServersPath, NewServerName, new string(CurrentServerConfig.core_name));
            MinMemory = CurrentServerConfig.min_memory.ToString();
            MaxMemory = CurrentServerConfig.max_memory.ToString();
            JavaId = 0;
            ExtJvm = new string(CurrentServerConfig.ext_jvm);
        }
        #endregion

        public ICommand SearchJava { get; }// ����Java����
        public ICommand ConfirmAddServer { get; }// ȷ����������������
        public ICommand DeleteServer { get; }// ɾ������������
        public ICommand EditServer { get; }// �༭����������

        #region ��ǰ������LSL������������ã�ID��·����Java�ȣ�

        public ServerConfig CurrentServerConfig // ��ǰ��������LSL�����ļ�
        {
            get
            {
                if (int.TryParse(SelectedServerId, out int result) && result >= 0) return ServerConfigManager.ServerConfigs[SelectedServerId];
                else return new ServerConfig("", "", "", "", "", 0, 0, "");
            }
        }
        public string CurrentServerName { get => CurrentServerConfig.name; }
        public string CurrentServerPath { get => CurrentServerConfig.server_path; }
        public string CurrentServerJava { get => CurrentServerConfig.using_java; }
        public string SelectedServerId
        {
            get
            {
                if (ServerIDs.Count == 0 || SelectedServerIndex < 0 || SelectedServerIndex >= ServerIDs.Count) return "";
                else return ServerIDs[SelectedServerIndex];
            }
        }// ��ǰѡ�еķ�����ID

        public Dictionary<string, object> CurrentServerProperty = [];// ��ǰ������server.properties�ֵ�
        public bool ReadProperties()// ��ȡ��ǰ������server.properties
        {
            try
            {
                var text = File.ReadAllLines(Path.Combine(CurrentServerPath, "server.properties"));
                CurrentServerProperty.Clear();
                foreach (var line in text)
                {
                    if (!line.StartsWith('#'))
                    {
                        var keyValue = line.Split('=');
                        if (keyValue.Length == 2)
                        {
                            CurrentServerProperty.Add(keyValue[0], keyValue[1]);
                        }
                    }
                }
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
        #endregion

        #region ȫ�ֻ�ȡ�������б�ReadServerList => ServerNames
        //�־û�������ӳ���б�
        private ObservableCollection<string> _serverIDs = [];// �������ļ��еķ�����ID�б�
        private ObservableCollection<string> _serverNames = [];// �����������б���ServerID��˳������
        public ObservableCollection<string> ServerIDs => _serverIDs;
        public ObservableCollection<string> ServerNames => _serverNames;
        // �������б��ȡ���������ļ���ȡ��
        public void ReadServerList()
        {
            ServerConfigManager.LoadServerConfigs();
            ObservableCollection<string> ids = [];
            ObservableCollection<string> names = [];
            foreach (var item in ServerConfigManager.ServerConfigs)
            {
                ids.Add(item.Value.server_id);
                names.Add(item.Value.name);
            }
            _serverIDs = ids;
            _serverNames = names;
            if (_selectedServerIndex > ServerNames.Count)
            {
                _selectedServerIndex = 0;
                this.RaisePropertyChanged(nameof(SelectedServerIndex));
            }
            this.RaisePropertyChanged(nameof(ServerIDs));
            this.RaisePropertyChanged(nameof(ServerNames));
        }

        #endregion

        // Java���
        #region ȫ�ֻ�ȡJava�б�ReadJavaList => JavaVersions
        //�־û�Javaӳ���б�
        private ObservableCollection<JavaInfo> _javaVersions = [];
        public ObservableCollection<JavaInfo> JavaVersions
        {
            get => _javaVersions;
            set => this.RaiseAndSetIfChanged(ref _javaVersions, value);
        }
        // Java�б��ȡ
        public void ReadJavaList()
        {
            JavaManager.InitJavaDict();
            JavaVersions.Clear();
            foreach (var item in JavaManager.JavaDict)
            {
                JavaVersions.Add(item.Value);
            }
        }
        #endregion

    }
}
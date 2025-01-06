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
        // �����������ļ����
        #region ��������
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
        // TODO:����ʼֵ���趨�����л�ҳ��ķ���
        #region ����������

        private string _newServerName = "NewServer";// ����������
        [ServerNameValidator] public string NewServerName { get => _newServerName; set => this.RaiseAndSetIfChanged(ref _newServerName, value); }

        private string _corePath = string.Empty;// �����ļ�·��
        [ServerCorePathValidator] public string CorePath { get => _corePath; set => this.RaiseAndSetIfChanged(ref _corePath, value); }

        private string _minMemory = "200";// ��������С�ڴ�
        [MinMemValidator] public string MinMemory { get => _minMemory.ToString(); set => this.RaiseAndSetIfChanged(ref _minMemory, value); }

        private string _maxMemory = "200";// ����������ڴ�
        [MaxMemValidator] public string MaxMemory { get => _maxMemory.ToString(); set => this.RaiseAndSetIfChanged(ref _maxMemory, value); }

        private int _javaId = 0;//Java���
        public int JavaId { get => _javaId; set => this.RaiseAndSetIfChanged(ref _javaId, value); }

        private string _extJvm = string.Empty;// ����JVM����
        [ExtJvmValidator] public string ExtJvm { get => _extJvm; set => this.RaiseAndSetIfChanged(ref _extJvm, value); }
        //TODO:���޸�����ʱ����ָ����Java�����Զ����JavaId
        public void ReadServerConfig(string serverID)
        {
            string serverName = ServerNames[ServerIDs.IndexOf(serverID)];
            var ConfigDict = ServerConfigManager.ReadServerConfig(serverName);
            if (ConfigDict != null) throw new Exception("��ȡ����������ʧ�ܣ�ָ���ķ����������ļ������ڻ�����");
        }
        #endregion

        public ICommand SearchJava { get; }// ����Java����
        public ICommand ConfirmAddServer { get; }// ȷ����������������
        public ICommand DeleteServer { get; }// ɾ������������
        public ICommand EditServer { get; }// �༭����������

        #region ȫ�ֻ�ȡ�������б�ReadServerList => ServerNames
        //�־û�������ӳ���б�
        private ObservableCollection<string> _serverIDs = [];// �������ļ��еķ�����ID�б�
        private ObservableCollection<string> _servernames = [];// �����������б���ServerID��˳������
        public ObservableCollection<string> ServerIDs => _serverIDs;
        public ObservableCollection<string> ServerNames => _servernames;
        // �������б��ȡ���������ļ���ȡ��
        public void ReadServerList()
        {
            string jsonContent = File.ReadAllText(ConfigManager.ServerConfigPath);
            JObject jsonObj = JObject.Parse(jsonContent);
            //���������ļ��е����з�����ID
            ServerIDs.Clear();
            foreach (var item in jsonObj.Properties())
            {
                ServerIDs.Add(item.Name);
            }
            //���ݷ�����ID��ȡÿ���������������ļ�
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
                    //throw new Exception($"������ {ServerID} ��·�������ڣ����������ļ���\r������Ϣ��{ex.Message}");
                }
            }
            if (SelectedServerIndex > ServerNames.Count)
            {
                SelectedServerIndex = 0;
            }
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
            foreach (var item in JavaManager.JavaDict)
            {
                JavaVersions.Add(item.Value);
            }
        }
        #endregion

    }
}
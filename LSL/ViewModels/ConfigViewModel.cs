using System;
using System.Collections.Generic;
using LSL.ViewModels;
using LSL.Services;
using ReactiveUI;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Input;

namespace LSL.ViewModels
{
    public partial class ConfigViewModel : ViewModelBase
    {
        #region ���ñ���
        //��ReactiveUI�İ취�����̫����
        //�����ֲ�����ObservableProperty����ΪҪ�Զ�����ĺ�Ĳ���
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
        //�ɹ۲�������һ��ʷ
        //communitytoolkitզ�Ͳ������޸Ĳ�����
        public bool AutoEula { get => autoEula; set { autoEula = value; OnPropertyChanged(nameof(AutoEula)); ConfigManager.ModifyConfig("auto_eula", value); } }
        public int AppPriority { get => appPriority; set { appPriority = value; OnPropertyChanged(nameof(AppPriority)); ConfigManager.ModifyConfig("app_priority", value); } }
        public bool EndServerWhenClose { get => endServerWhenClose; set { endServerWhenClose = value; OnPropertyChanged(nameof(EndServerWhenClose)); ConfigManager.ModifyConfig("end_server_when_close", value); } }
        public bool Daemon { get => daemon; set { daemon = value; OnPropertyChanged(nameof(Daemon)); ConfigManager.ModifyConfig("daemon", value); } }
        public int JavaSelection { get => javaSelection; set { javaSelection = value; OnPropertyChanged(nameof(JavaSelection)); ConfigManager.ModifyConfig("java_selection", value); } }
        public bool AutoFindJava { get => autoFindJava; set { autoFindJava = value; OnPropertyChanged(nameof(AutoFindJava)); ConfigManager.ModifyConfig("auto_find_java", value); } }
        public int OutputEncode { get => outputEncode; set { outputEncode = value; OnPropertyChanged(nameof(OutputEncode)); ConfigManager.ModifyConfig("output_encode", value); } }
        public int InputEncode { get => inputEncode; set { inputEncode = value; OnPropertyChanged(nameof(InputEncode)); ConfigManager.ModifyConfig("input_encode", value); } }
        public bool ColoringTerminal { get => coloringTerminal; set { coloringTerminal = value; OnPropertyChanged(nameof(ColoringTerminal)); ConfigManager.ModifyConfig("coloring_terminal", value); } }
        public int DownloadSource { get => downloadSource; set { downloadSource = value; OnPropertyChanged(nameof(DownloadSource)); ConfigManager.ModifyConfig("download_source", value); } }
        public int DownloadThreads { get => downloadThreads; set { downloadThreads = value; OnPropertyChanged(nameof(DownloadThreads)); ConfigManager.ModifyConfig("download_threads", value); } }
        public int DownloadLimit { get => downloadLimit; set { downloadLimit = value; OnPropertyChanged(nameof(DownloadLimit)); ConfigManager.ModifyConfig("download_limit", value); } }
        public bool PanelEnable { get => panelEnable; set { panelEnable = value; OnPropertyChanged(nameof(PanelEnable)); ConfigManager.ModifyConfig("panel_enable", value); } }
        public int PanelPort { get => panelPort; set { panelPort = value; OnPropertyChanged(nameof(PanelPort)); ConfigManager.ModifyConfig("panel_port", value); } }
        public bool PanelMonitor { get => panelMonitor; set { panelMonitor = value; OnPropertyChanged(nameof(PanelMonitor)); ConfigManager.ModifyConfig("panel_monitor", value); } }
        public bool PanelTerminal { get => panelTerminal; set { panelTerminal = value; OnPropertyChanged(nameof(PanelTerminal)); ConfigManager.ModifyConfig("panel_terminal", value); } }
        public bool AutoUpdate { get => autoUpdate; set { autoUpdate = value; OnPropertyChanged(nameof(AutoUpdate)); ConfigManager.ModifyConfig("auto_update", value); } }
        public bool BetaUpdate { get => betaUpdate; set { betaUpdate = value; OnPropertyChanged(nameof(BetaUpdate)); ConfigManager.ModifyConfig("auto_eula", value); } }
        //������֤����
        private int appPriorityCache;
        private int javaSelectionCache;
        private int outputEncodeCache;
        private int inputEncodeCache;
        private int downloadSourceCache;
        private int downloadThreadsCache;
        private int downloadLimitCache;
        private int panelPortCache;
        #endregion

        public ConfigViewModel()
        {
            #region ��ȡ�����ļ�
            try
            {
                autoEula = (bool)ConfigManager.ReadConfig("auto_eula");
                appPriorityCache = (int)ConfigManager.ReadConfig("app_priority");
                endServerWhenClose = (bool)ConfigManager.ReadConfig("end_server_when_close");
                daemon = (bool)ConfigManager.ReadConfig("daemon");
                javaSelectionCache = (int)ConfigManager.ReadConfig("java_selection");
                autoFindJava = (bool)ConfigManager.ReadConfig("auto_find_java");
                outputEncodeCache = (int)ConfigManager.ReadConfig("output_encode");
                inputEncodeCache = (int)ConfigManager.ReadConfig("input_encode");
                coloringTerminal = (bool)ConfigManager.ReadConfig("coloring_terminal");
                downloadSourceCache = (int)ConfigManager.ReadConfig("download_source");
                downloadThreadsCache = (int)ConfigManager.ReadConfig("download_threads");
                downloadLimitCache = (int)ConfigManager.ReadConfig("download_limit");
                panelEnable = (bool)ConfigManager.ReadConfig("panel_enable");
                panelPortCache = (int)ConfigManager.ReadConfig("panel_port");
                panelMonitor = (bool)ConfigManager.ReadConfig("panel_monitor");
                panelTerminal = (bool)ConfigManager.ReadConfig("panel_terminal");
                autoUpdate = (bool)ConfigManager.ReadConfig("auto_update");
                betaUpdate = (bool)ConfigManager.ReadConfig("beta_update");
            }
            catch (Exception)
            {
                throw new ArgumentException("�����ļ�������ɾ��������Ŀ¼��LSL�ļ����е�config.json�����ԡ�");
            }
            #endregion

            #region ������֤
            if (appPriorityCache >= 0 && appPriorityCache <= 2)
                appPriority = appPriorityCache;
            else ConfigManager.ModifyConfig("app_priority", 1);

            if (javaSelectionCache >= 0)
                javaSelection = javaSelectionCache;
            else ConfigManager.ModifyConfig("java_selection", 0);

            if (outputEncodeCache >= 1 && outputEncodeCache <= 2)
                outputEncode = outputEncodeCache;
            else ConfigManager.ModifyConfig("output_encode", 1);

            if (inputEncodeCache >= 0 && inputEncodeCache <= 2)
                inputEncode = inputEncodeCache;
            else ConfigManager.ModifyConfig("input_encode", 0);

            if (downloadSourceCache >= 0 && downloadSourceCache <= 1)
                downloadSource = downloadSourceCache;
            else ConfigManager.ModifyConfig("download_source", 0);

            if (downloadThreadsCache >= 1 && downloadThreadsCache <= 128)
                downloadThreads = downloadThreadsCache;
            else ConfigManager.ModifyConfig("download_threads", 16);

            if (downloadLimitCache >= 0 && downloadLimitCache <= 1000)
                downloadLimit = downloadLimitCache;
            else ConfigManager.ModifyConfig("download_limit", 0);

            if (panelPortCache >= 8080 && panelPortCache <= 65535)
                panelPort = panelPortCache;
            else ConfigManager.ModifyConfig("panel_port", 25000);
            #endregion
        }

        JavaManager javaManager = new JavaManager();
        public void GetJava()
        {
            javaManager.DetectJava();
        }

    }
}
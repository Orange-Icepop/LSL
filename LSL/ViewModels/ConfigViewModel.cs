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
        //communitytoolkit咋就不允许修改操作呢
        public bool AutoEula { get => autoEula; set { autoEula = value; OnPropertyChanged(nameof(AutoEula)); ConfigurationManager.ModifyConfig("auto_eula", value); } }
        public int AppPriority { get => appPriority; set { appPriority = value; OnPropertyChanged(nameof(AppPriority)); ConfigurationManager.ModifyConfig("app_priority", value); } }
        public bool EndServerWhenClose { get => endServerWhenClose; set { endServerWhenClose = value; OnPropertyChanged(nameof(EndServerWhenClose)); ConfigurationManager.ModifyConfig("end_server_when_close", value); } }
        public bool Daemon { get => daemon; set { daemon = value; OnPropertyChanged(nameof(Daemon)); ConfigurationManager.ModifyConfig("daemon", value); } }
        public int JavaSelection { get => javaSelection; set { javaSelection = value; OnPropertyChanged(nameof(JavaSelection)); ConfigurationManager.ModifyConfig("java_selection", value); } }
        public bool AutoFindJava { get => autoFindJava; set { autoFindJava = value; OnPropertyChanged(nameof(AutoFindJava)); ConfigurationManager.ModifyConfig("auto_find_java", value); } }
        public int OutputEncode { get => outputEncode; set { outputEncode = value; OnPropertyChanged(nameof(OutputEncode)); ConfigurationManager.ModifyConfig("output_encode", value); } }
        public int InputEncode { get => inputEncode; set { inputEncode = value; OnPropertyChanged(nameof(InputEncode)); ConfigurationManager.ModifyConfig("input_encode", value); } }
        public bool ColoringTerminal { get => coloringTerminal; set { coloringTerminal = value; OnPropertyChanged(nameof(ColoringTerminal)); ConfigurationManager.ModifyConfig("coloring_terminal", value); } }
        public int DownloadSource { get => downloadSource; set { downloadSource = value; OnPropertyChanged(nameof(DownloadSource)); ConfigurationManager.ModifyConfig("download_source", value); } }
        public int DownloadThreads { get => downloadThreads; set { downloadThreads = value; OnPropertyChanged(nameof(DownloadThreads)); ConfigurationManager.ModifyConfig("download_threads", value); } }
        public int DownloadLimit { get => downloadLimit; set { downloadLimit = value; OnPropertyChanged(nameof(DownloadLimit)); ConfigurationManager.ModifyConfig("download_limit", value); } }
        public bool PanelEnable { get => panelEnable; set { panelEnable = value; OnPropertyChanged(nameof(PanelEnable)); ConfigurationManager.ModifyConfig("panel_enable", value); } }
        public int PanelPort { get => panelPort; set { panelPort = value; OnPropertyChanged(nameof(PanelPort)); ConfigurationManager.ModifyConfig("panel_port", value); } }
        public bool PanelMonitor { get => panelMonitor; set { panelMonitor = value; OnPropertyChanged(nameof(PanelMonitor)); ConfigurationManager.ModifyConfig("panel_monitor", value); } }
        public bool PanelTerminal { get => panelTerminal; set { panelTerminal = value; OnPropertyChanged(nameof(PanelTerminal)); ConfigurationManager.ModifyConfig("panel_terminal", value); } }
        public bool AutoUpdate { get => autoUpdate; set { autoUpdate = value; OnPropertyChanged(nameof(AutoUpdate)); ConfigurationManager.ModifyConfig("auto_update", value); } }
        public bool BetaUpdate { get => betaUpdate; set { betaUpdate = value; OnPropertyChanged(nameof(BetaUpdate)); ConfigurationManager.ModifyConfig("auto_eula", value); } }
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

        public ConfigViewModel()
        {
            #region 读取配置文件
            try
            {
                autoEula = (bool)ConfigurationManager.ReadConfig("auto_eula");
                appPriorityCache = (int)ConfigurationManager.ReadConfig("app_priority");
                endServerWhenClose = (bool)ConfigurationManager.ReadConfig("end_server_when_close");
                daemon = (bool)ConfigurationManager.ReadConfig("daemon");
                javaSelectionCache = (int)ConfigurationManager.ReadConfig("java_selection");
                autoFindJava = (bool)ConfigurationManager.ReadConfig("auto_find_java");
                outputEncodeCache = (int)ConfigurationManager.ReadConfig("output_encode");
                inputEncodeCache = (int)ConfigurationManager.ReadConfig("input_encode");
                coloringTerminal = (bool)ConfigurationManager.ReadConfig("coloring_terminal");
                downloadSourceCache = (int)ConfigurationManager.ReadConfig("download_source");
                downloadThreadsCache = (int)ConfigurationManager.ReadConfig("download_threads");
                downloadLimitCache = (int)ConfigurationManager.ReadConfig("download_limit");
                panelEnable = (bool)ConfigurationManager.ReadConfig("panel_enable");
                panelPortCache = (int)ConfigurationManager.ReadConfig("panel_port");
                panelMonitor = (bool)ConfigurationManager.ReadConfig("panel_monitor");
                panelTerminal = (bool)ConfigurationManager.ReadConfig("panel_terminal");
                autoUpdate = (bool)ConfigurationManager.ReadConfig("auto_update");
                betaUpdate = (bool)ConfigurationManager.ReadConfig("beta_update");
            }
            catch (Exception)
            {
                PopupPublisher.Instance.PopupMessage("deadlyError", "配置文件出错，请删除主程序目录下LSL文件夹中的config.json后重试。");
            }
            #endregion

            #region 缓存验证
            if (appPriorityCache >= 0 && appPriorityCache <= 2)
                appPriority = appPriorityCache;
            else ConfigurationManager.ModifyConfig("app_priority", 1);

            if (javaSelectionCache >= 0)
                javaSelection = javaSelectionCache;
            else ConfigurationManager.ModifyConfig("java_selection", 0);

            if (outputEncodeCache >= 1 && outputEncodeCache <= 2)
                outputEncode = outputEncodeCache;
            else ConfigurationManager.ModifyConfig("output_encode", 1);

            if (inputEncodeCache >= 0 && inputEncodeCache <= 2)
                inputEncode = inputEncodeCache;
            else ConfigurationManager.ModifyConfig("input_encode", 0);

            if (downloadSourceCache >= 0 && downloadSourceCache <= 1)
                downloadSource = downloadSourceCache;
            else ConfigurationManager.ModifyConfig("download_source", 0);

            if (downloadThreadsCache >= 1 && downloadThreadsCache <= 128)
                downloadThreads = downloadThreadsCache;
            else ConfigurationManager.ModifyConfig("download_threads", 16);

            if (downloadLimitCache >= 0 && downloadLimitCache <= 1000)
                downloadLimit = downloadLimitCache;
            else ConfigurationManager.ModifyConfig("download_limit", 0);

            if (panelPortCache >= 8080 && panelPortCache <= 65535)
                panelPort = panelPortCache;
            else ConfigurationManager.ModifyConfig("panel_port", 25000);
            #endregion
        }

        JavaManager javaManager = new JavaManager();
        public void GetJava()
        {
            javaManager.DetectJava();
        }

    }
}
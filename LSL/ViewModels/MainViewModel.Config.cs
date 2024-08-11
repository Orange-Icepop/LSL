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
        public int AppPriority { get => appPriority; set { this.RaiseAndSetIfChanged(ref appPriority, value); ; ConfigManager.ModifyConfig("app_priority", value); } }
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
                throw new ArgumentException("配置文件出错，请删除主程序目录下LSL文件夹中的config.json后重试。");
            }
        }
        #endregion


    }
}
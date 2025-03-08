using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSL.Services;

namespace LSL.ViewModels
{
    // 用于连接视图模型与服务层
    // 主要成员为void，用于调用服务层方法
    public class ServiceConnector
    {
        public AppStateLayer AppState { get; set; }
        public ServiceConnector(AppStateLayer appState)
        {
            AppState = appState;
        }

        #region 配置部分
        public void GetConfig(bool readFile = false)
        {
            if (readFile) ConfigManager.LoadConfig();
            AppState.CurrentConfigs = ConfigManager.CurrentConfigs;
        }

        public void ReadJavaConfig(bool readFile = false)
        {
            if (readFile) JavaManager.ReadJavaConfig();
            AppState.CurrentJavaDict = JavaManager.JavaDict;
        }

        public void ReadServerConfig(bool readFile = false)
        {
            if (readFile) ServerConfigManager.LoadServerConfigs();
            AppState.CurrentServerConfigs = ServerConfigManager.ServerConfigs;
        }

        public void SaveConfig()
        {
            ConfigManager.ConfirmConfig(AppState.CurrentConfigs);
        }

        #endregion
    }
}
